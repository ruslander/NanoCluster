using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NanoCluster.Config;
using NetMQ;

namespace NanoCluster.Services
{
    public enum CandidateStatus
    {
        Alive,
        Dead,
    }

    public class ElectionAgent
    {
        public bool IsLeadingProcess;
        public string LeaderHost;

        private readonly ClusterConfig _cfg;
        private readonly CancellationTokenSource _terminator;
        private readonly object _lockObject = new object();

        public ElectionAgent(ClusterConfig cfg, CancellationTokenSource terminator)
        {
            _cfg = cfg;
            _terminator = terminator;
        }

        public void Run()
        {
            var workerThread = new Thread(MainLoop);
            workerThread.Start();
            Console.WriteLine("Election agent started for '{0}' host.", _cfg.Host);

            while (!_terminator.IsCancellationRequested) ;

            Console.WriteLine("Disposing Election");

            workerThread.Abort();
        }

        private void MainLoop()
        {
            while (!_terminator.IsCancellationRequested)
            {
                lock (_lockObject)
                {
                    try
                    {
                        HoldElection(_cfg.Host, _cfg.AuthoritiesToMe);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception" + ex.ToString());
                    }
                }

                Thread.Sleep(_cfg.ElectionIntervalSeconds);
            }
        }

        public string PreviuosLeader = string.Empty;

        public void WriteOnChange(string text, string leader)
        {
            if (PreviuosLeader == leader) 
                return;

            Console.WriteLine(text);
            PreviuosLeader = leader;
        }

        public virtual void HoldElection(string host, IList<string> authoritiesForMe)
        {
            if (!authoritiesForMe.Any())
            {
                WriteOnChange("I have the highest priority set up, taking the cluster leadership", host);
                IsLeadingProcess = true;
                LeaderHost = host;
                return;
            }

            foreach (var candidate in authoritiesForMe)
            {
                if (TriggerElection(candidate) == CandidateStatus.Alive)
                {
                    WriteOnChange("Node " + candidate + " has higher priority and is alive, cancel this election",candidate);
                    IsLeadingProcess = false;
                    LeaderHost = candidate;
                    return;
                }
                else
                {
                    Console.WriteLine("Node " + candidate + " is unreachable");
                }
            }

            WriteOnChange("Nobody from my authoritative list of nodes are available, taking over the cluster", host);
            IsLeadingProcess = true;
            LeaderHost = host;
        }

        public virtual CandidateStatus TriggerElection(string uri)
        {
            using (NetMQContext context = NetMQContext.Create())
            using (NetMQSocket client = context.CreateRequestSocket())
            {
                client.Connect(uri);
                client.Options.ReceiveTimeout = _cfg.ElectionMessageReceiveTimeoutSeconds;
                client.Send("election");
                
                var reply = string.Empty;
                
                try
                {
                    reply = client.ReceiveString();
                }
                catch (AgainException e)
                {
                }

                client.Options.Linger = TimeSpan.FromSeconds(1); //required
                client.Close(); //required

                return reply == "ok" ? CandidateStatus.Alive : CandidateStatus.Dead;
            }
        }
    }
}