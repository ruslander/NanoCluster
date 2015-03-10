using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

        public virtual void HoldElection(string host, IList<string> authoritiesForMe)
        {
            if (!authoritiesForMe.Any())
            {
                // i have the highest priority step up
                Console.WriteLine("I have the highest priority, taking the cluster leadership !!!");
                IsCoordinatorProcess = true;
                return;
            }

            foreach (var candidate in authoritiesForMe)
            {
                if (TriggerElection(candidate) == CandidateStatus.Alive)
                {
                    IsCoordinatorProcess = false;

                    // if there is alive a candidate with higher id then, back off
                    Console.WriteLine("Node '{0}' with higher priority is alive, backing off", candidate);
                    return;
                }
                else
                {
                    // this node is dead keep going
                    Console.WriteLine("Node '{0}' is unavailable, moving on", candidate);
                }
            }

            // if nobody responds then step up
            Console.WriteLine("None from my authoritative list of nodes are available, taking over the cluster !!!");
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