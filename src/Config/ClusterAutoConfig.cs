using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using NetMQ;

namespace NanoCluster.Config
{
    public class ClusterAutoConfig : ClusterConfig
    {
        private readonly NetMQContext Context = NetMQContext.Create();
        private readonly TimeSpan DeadNodeTimeout = TimeSpan.FromSeconds(10);
        private readonly Dictionary<ActiveNode, DateTime> ActiveNodes = new Dictionary<ActiveNode, DateTime>();
        private readonly Random Rnd = new Random();

        private string _randomPort;
        public string ClusterName = string.Empty;

        public override void BindByConfigType(NetMQSocket responder)
        {
            _randomPort = responder.BindRandomPort("tcp://*").ToString();

            var advertizer = new NetMQBeacon(Context);
            advertizer.Configure(9999);

            var info = new ActiveNode()
            {
                ClusterName = ClusterName,
                Name = "Node" + Rnd.Next(100, DateTime.Now.Millisecond),
                Port = _randomPort
            };

            advertizer.Publish(info.ToString(), TimeSpan.FromSeconds(2));
        }

        public ClusterAutoConfig()
        {
            var timer = new Timer(10 * 1000);

            timer.Elapsed += (sender, eventArgs) =>
            {
                var deadNodes = ActiveNodes.
                    Where(n => DateTime.Now > n.Value + DeadNodeTimeout).
                    Select(n => n.Key).ToArray();

                foreach (var node in deadNodes)
                {
                    Console.WriteLine("Detected a dead node " + node);
                    ActiveNodes.Remove(node);
                    ApplyChangedPriorityList(GetMembersByPriority());
                }
            };

            timer.Start();

            Task.Factory.StartNew(() =>
            {
                using (var receptor = new NetMQBeacon(Context))
                {
                    receptor.Configure(9999);
                    receptor.Subscribe(ClusterName);

                    while (true)
                    {
                        string peerName;
                        var memberInfoAsString = receptor.ReceiveString(out peerName);
                        var host = peerName.Replace(":9999", "");

                        var activeNode = ActiveNode.Parse(host, memberInfoAsString);

                        if (activeNode.Port == _randomPort)
                            Host = activeNode.Uri;

                        if (!ActiveNodes.ContainsKey(activeNode))
                        {
                            ActiveNodes.Add(activeNode, DateTime.Now);
                            Console.WriteLine("New node joining " + activeNode);

                            ApplyChangedPriorityList(GetMembersByPriority());
                        }
                        else
                            ActiveNodes[activeNode] = DateTime.Now;
                    }
                }
            });
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
    }
}