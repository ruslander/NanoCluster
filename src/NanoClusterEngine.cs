using System.Threading.Tasks;
using NanoCluster.Config;

namespace NanoCluster
{
    public class NanoClusterEngine
    {
        ClusteredNode _node;

        public bool IsCoordinatorProcess {
            get { return _node.IsCoordinatorProcess; }
        }

        public NanoClusterEngine()
        {
            var config = new ClusterAutoConfig();
            Bootstrap(config);
        }

        public NanoClusterEngine(string name)
        {
            var config = new ClusterAutoConfig {ClusterName = name};
            Bootstrap(config);
        }

        public NanoClusterEngine(string host, string membersByPriority)
        {
            var config = new ClusterStaticConfig(host, membersByPriority);
            Bootstrap(config);
        }

        private void Bootstrap(ClusterConfig config)
        {
            _node = new ClusteredNode(config);

            Task.Factory.StartNew(() => new ElectionAgent().Run(config));
            Task.Factory.StartNew(() => { _node.Run(); });
        }
    }
}