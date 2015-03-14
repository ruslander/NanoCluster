using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NanoCluster.Config;
using NanoCluster.Pipeline;

namespace NanoCluster
{
    public class NanoClusterEngine
    {
        public DistributedProcess Process = new DistributedProcess();
        
        ElectionAgent _elector;
        ClusteredNode _clusteredNode;
        LeaderCatchup _catchup;
        ClusterConfig _config;

        public bool IsLeadingProcess {
            get { return _elector.IsLeadingProcess; }
        }

        public NanoClusterEngine(DistributedProcess process)
        {
            Process = process;

            _config = new ClusterAutoConfig();
            Bootstrap(_config, Process);
        }

        public NanoClusterEngine(string name)
        {
            _config = new ClusterAutoConfig {ClusterName = name};
            Bootstrap(_config, Process);
        }

        public NanoClusterEngine(string host, string membersByPriority)
        {
            _config = new ClusterStaticConfig(host, membersByPriority);
            Bootstrap(_config, Process);
        }

        public string WhoAmI()
        {
            return (IsLeadingProcess ? "L" : "F") + " " + _config.NodeId();
        }

        private void Bootstrap(ClusterConfig config, DistributedProcess process)
        {
            _elector = new ElectionAgent(config);
            _clusteredNode = new ClusteredNode(config){Process = process};
            _catchup = new LeaderCatchup(_elector, config);

            Task.Factory.StartNew(() => { _elector.Run(); });
            Task.Factory.StartNew(() => { _clusteredNode.Run(); });

            while (string.IsNullOrEmpty(_elector.LeaderHost))
            {
                Thread.Sleep(1000);
            }

            Task.Factory.StartNew(() => { _catchup.Run(process); });
        }

        public void Send(object message)
        {
            if (IsLeadingProcess)
            {
                Console.WriteLine("Dispatch to processing pipeline '{0}'", message);
                Process.Dispatch(message);
                return ;
            }

            Console.WriteLine("Forwarded to leader '{0}'", message);

            try
            {
                _clusteredNode.Send(_elector.LeaderHost, message);
            }
            catch (SerializationException e)
            {
                throw new InvalidOperationException(message.GetType().Name + " Must be decorated with [Serializable] attribute");
            }
        }
    }
}