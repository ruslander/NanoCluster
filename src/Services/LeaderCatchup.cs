using System;
using System.Collections.Generic;
using System.Threading;
using NanoCluster.Config;
using NanoCluster.Pipeline;
using NetMQ;

namespace NanoCluster.Services
{
    public class LeaderCatchup
    {
        private readonly ElectionAgent _elector;
        private readonly ClusterConfig _config;
        private readonly CancellationTokenSource _terminator;

        public LeaderCatchup(ElectionAgent elector, ClusterConfig config, CancellationTokenSource terminator)
        {
            _elector = elector;
            _config = config;
            _terminator = terminator;
        }

        public void Run(DistributedProcess localProcess)
        {
            var workerThread = new Thread(() => MainLoop(localProcess));
            workerThread.Start();
            Console.WriteLine("LeaderCatchup agent started for '{0}' host.", _config.Host);

            while (!_terminator.IsCancellationRequested) ;

            Console.WriteLine("Disposing LeaderCatchup");

            workerThread.Abort();
        }

        private void MainLoop(DistributedProcess localProcess)
        {
            while (!_terminator.IsCancellationRequested)
            {
                if (_elector.IsLeadingProcess)
                {
                    Thread.Sleep(_config.ElectionIntervalSeconds);
                    continue;
                }

                var deltas = CatchupFromVersion(_elector.LeaderHost, localProcess.Version);

                foreach (var evt in deltas)
                {
                    localProcess.Apply(evt);
                }

                Thread.Sleep(TimeSpan.FromSeconds(.5));
            }
        }

        private List<object> CatchupFromVersion(string leaderHost, int version)
        {
            using (var context = NetMQContext.Create())
            using (var client = context.CreateRequestSocket())
            {
                client.Connect(leaderHost);
                client.Options.ReceiveTimeout = _config.MessageReceiveTimeoutSeconds;

                client.SendMore("catchup").Send(version.ToString());

                var reply = new List<object>();

                try
                {
                    var payload = client.ReceiveString();
                    var deltas = (List<object>)BinarySerializer.Deserialize(payload);

                    reply.AddRange(deltas);
                }
                catch (AgainException e)
                {
                }

                client.Options.Linger = TimeSpan.FromSeconds(1); //required
                client.Close(); //required

                return reply;
            }
        }
    }
}