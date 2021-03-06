﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NLog;
using Timer = System.Timers.Timer;

namespace NanoCluster.Config
{
    public class ClusterAutoConfig : ClusterConfig
    {
        private readonly NetMQContext _context = NetMQContext.Create();
        private readonly CancellationTokenSource _terminator;
        private readonly TimeSpan DeadNodeTimeout = TimeSpan.FromSeconds(10);
        private readonly Dictionary<ActiveNode, DateTime> ActiveNodes = new Dictionary<ActiveNode, DateTime>();
        private readonly Random Rnd = new Random();
        private Thread workerThread;
        private Timer timer;

        private static readonly Logger Logger = LogManager.GetLogger("ClusterAutoConfig");

        readonly ManualResetEventSlim _initializationCoordinator = new ManualResetEventSlim(false);

        public string ClusterName;
        private readonly int _gossipOnPort;

        public ClusterAutoConfig(CancellationTokenSource terminator, string clusterName) : this(terminator, clusterName,9999){}

        public ClusterAutoConfig(CancellationTokenSource terminator, string clusterName, int gossipOnPort)
        {
            _terminator = terminator;
            ClusterName = clusterName;
            _gossipOnPort = gossipOnPort;

            workerThread = new Thread(MainLoop);
            workerThread.Start();


            _initializationCoordinator.Wait();
        }

        private void MainLoop()
        {
            timer = new Timer(10 * 1000);
            timer.Elapsed += (sender, eventArgs) =>
            {
                var deadNodes = ActiveNodes.
                    Where(n => DateTime.Now > n.Value + DeadNodeTimeout).
                    Select(n => n.Key).ToArray();

                foreach (var node in deadNodes)
                {
                    Logger.Warn("Detected a dead node " + node);
                    ActiveNodes.Remove(node);
                    ApplyChangedPriorityList(GetMembersByPriority());
                }
            };
            timer.Start();


            var info = new ActiveNode()
            {
                ClusterName = ClusterName,
                Name = "Node" + Rnd.Next(1, DateTime.Now.Millisecond),
                Port = GetNextFeePort().ToString()
            };

            var nodeInfoAdvertiser = new NetMQBeacon(_context);
            nodeInfoAdvertiser.Configure(_gossipOnPort);
            nodeInfoAdvertiser.Publish(info.ToString(), TimeSpan.FromSeconds(2));


            Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var discoverClusterMembers = new NetMQBeacon(_context))
                    {
                        discoverClusterMembers.Configure(_gossipOnPort);
                        discoverClusterMembers.Subscribe(ClusterName);

                        while (!_terminator.IsCancellationRequested)
                        {
                            string peerName;
                            var memberInfoAsString = discoverClusterMembers.ReceiveString(out peerName);
                            var host = peerName.Replace(":" + _gossipOnPort, "");

                            var activeNode = ActiveNode.Parse(host, memberInfoAsString);

                            if (activeNode.Port == info.Port && activeNode.Name == info.Name && activeNode.Uptime == info.Uptime)
                            {
                                Host = activeNode.Uri;
                                _initializationCoordinator.Set();
                            }

                            if (!ActiveNodes.ContainsKey(activeNode))
                            {
                                ActiveNodes.Add(activeNode, DateTime.Now);
                                Logger.Info("New node joining " + activeNode);

                                ApplyChangedPriorityList(GetMembersByPriority());
                            }
                            else
                                ActiveNodes[activeNode] = DateTime.Now;
                        }
                    }
                }
                catch (TerminatingException e)
                {
                    return;
                }
            }, _terminator.Token);
        }

        static int GetNextFeePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private string[] GetMembersByPriority()
        {
            return ActiveNodes.Keys.OrderByDescending(x => x.Uptime).Select(x => x.Uri).ToArray();
        }

        public class ActiveNode : IEquatable<ActiveNode>
        {
            public string Host;
            public string Uri;
            public string ClusterName;
            public string Name;
            public string Uptime;
            public string Port;

            public ActiveNode()
            {
                Uptime = DateTime.Now.Ticks.ToString();
            }

            public static ActiveNode Parse(string host, string payload)
            {
                var parts = payload.Split('|');

                return new ActiveNode()
                {
                    ClusterName = parts[0],
                    Name = parts[1],
                    Port = parts[2],
                    Uptime = parts[3],
                    Host = host,
                    Uri = string.Format("tcp://{0}:{1}", host, parts[2])
                };
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as ActiveNode);
            }

            public bool Equals(ActiveNode other)
            {
                return other != null && other.Uri == this.Uri;
            }

            public override int GetHashCode()
            {
                return (Host.GetHashCode() * 397) ^ Port.GetHashCode();
            }

            public override string ToString()
            {
                return ClusterName + "|" + Name + "|" + Port + "|" + Uptime;
            }
        }

        public override void Dispose()
        {
            Logger.Debug("Disposing");
            workerThread.Abort();
            timer.Stop();
        }
    }
}