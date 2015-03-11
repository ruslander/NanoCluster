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
        private NetMQContext Context = NetMQContext.Create();
        private readonly TimeSpan DeadNodeTimeout = TimeSpan.FromSeconds(10);
        private readonly Dictionary<ActiveNode, DateTime> ActiveNodes = new Dictionary<ActiveNode, DateTime>();

        private string _randomPort;

        public override void BindByConfigType(NetMQSocket responder)
        {
            _randomPort = responder.BindRandomPort("tcp://*").ToString();

            var advertizer = new NetMQBeacon(Context);
            advertizer.Configure(9999);
            advertizer.Publish(_randomPort + " " + DateTime.Now.Ticks, TimeSpan.FromSeconds(2));
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
                    receptor.Subscribe("");

                    while (true)
                    {
                        string peerName;
                        var portAndTicks = receptor.ReceiveString(out peerName);
                        var nodeName = peerName.Replace(":9999", "");

                        var activeNode = new ActiveNode(nodeName, portAndTicks);

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

        public class ActiveNode
        {
            public ActiveNode(string name, string portAndTicks)
            {
                Name = name;

                var parts = portAndTicks.Split(' ');

                Port = parts[0];
                Uptime = long.Parse(parts[1]);

                Uri = string.Format("tcp://{0}:{1}", name, Port);
            }

            public string Uri { get; private set; }
            public string Name { get; private set; }
            public string Port { get; private set; }
            public long Uptime { get; private set; }

            public override string ToString()
            {
                return Name + ":" + Port;
            }

            protected bool Equals(ActiveNode other)
            {
                return string.Equals(Name, other.Name) && Port == other.Port;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ActiveNode)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Port.GetHashCode();
                }
            }
        }
    }
}