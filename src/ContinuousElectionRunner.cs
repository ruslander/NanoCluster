using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using NanoCluster.Config;
using NetMQ;

namespace NanoCluster
{
    public enum CandidateStatus
    {
        Alive,
        Dead,
    }

    public class ContinuousElectionRunner
    {
        public bool IsCoordinatorProcess;

        private readonly ClusterConfig _cfg;
        private readonly object _lockObject = new object();

        public ContinuousElectionRunner(ClusterConfig cfg)
        {
            _cfg = cfg;
        }

        public void Run()
        {
            for(;;)
            {
                lock (_lockObject)
                {
                    try
                    {
                        //Console.WriteLine("Election instigated by '{0}'.", _local.Uri);

                        HoldElection(_cfg.Host, _cfg.AuthoritiesToMe);

                        //Console.WriteLine("Election instigated by '{0}' COMPLETED.", _local.Uri);
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine("Exception" + ex.ToString());
                    }
                }

                //Thread.Sleep(TimeSpan.FromSeconds(ElectionIntervalSeconds));
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
                WriteOnChange("I have the highest priority set up, taking the cluster leadership", "me");
                IsCoordinatorProcess = true;
                return;
            }

            foreach (var candidate in authoritiesForMe)
            {
                if (TriggerElection(candidate) == CandidateStatus.Alive)
                {
                    WriteOnChange("Node " + candidate + " has higher priority and is alive, cancel this election",candidate);
                    IsCoordinatorProcess = false;
                    return;
                }
                else
                {
                    Console.WriteLine("Node " + candidate + " is unreachable");
                }
            }

            WriteOnChange("Nobody from my authoritative list of nodes are available, taking over the cluster", "me");
            IsCoordinatorProcess = true;
        }

        public virtual CandidateStatus TriggerElection(string uri)
        {
            //Console.WriteLine("Trigger election to '{0}'.", nodeAddress.Uri);

            using (NetMQContext context = NetMQContext.Create())
            using (NetMQSocket client = context.CreateRequestSocket())
            {
                client.Connect(uri);
                client.Options.ReceiveTimeout = _cfg.ElectionMessageReceiveTimeoutSeconds;
                client.Send("ELECTION");
                
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

                return reply == "OK" ? CandidateStatus.Alive : CandidateStatus.Dead;
            }
        }
    }
}