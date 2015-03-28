using System;
using System.Collections.Generic;
using System.Threading;
using NanoCluster.Config;
using NanoCluster.Pipeline;
using NetMQ;
using NLog;

namespace NanoCluster.Services
{
    public class LeaderCatchup
    {
        private readonly ElectionAgent _elector;
        private readonly NetMQContext _context = NetMQContext.Create();
        private readonly ClusterConfig _config;
        private readonly CancellationTokenSource _terminator;

        private static readonly Logger Logger = LogManager.GetLogger("LeaderCatchup");

        public DistributedTransactionLog Transactions { get; set; }

        public LeaderCatchup(ElectionAgent elector, ClusterConfig config, CancellationTokenSource terminator)
        {
            _elector = elector;
            _config = config;
            _terminator = terminator;
        }

        public void Run()
        {
            while (!_terminator.IsCancellationRequested)
            {
                if (_elector.IsLeadingProcess)
                {
                    Thread.Sleep(_config.ElectionIntervalSeconds);
                    continue;
                }

                Logger.Debug("Catching up with {0} from {1}", _elector.LeaderHost, Transactions.Version);
                var deltas = CatchupFromVersion(_elector.LeaderHost, Transactions.Version);
                Logger.Debug("Leader handed {0} events", deltas.Count);


                foreach (var evt in deltas)
                {
                    Transactions.Apply(evt);
                }

                Thread.Sleep(TimeSpan.FromSeconds(.5));
            }
        }

        private List<object> CatchupFromVersion(string leaderHost, int version)
        {
            using (var client = _context.CreateRequestSocket())
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