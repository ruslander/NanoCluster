using System.Threading;
using NanoCluster.Pipeline;

namespace NanoCluster.Config
{
    public class ConfigurationModel
    {
        private readonly CancellationTokenSource _terminator;
        
        public ClusterConfig Cfg;
        public DistributedProcess Process;

        public ConfigurationModel(CancellationTokenSource terminator)
        {
            _terminator = terminator;
        }

        public void UseStaticTopology(string host, string members)
        {
            Cfg =  new ClusterStaticConfig(host, members);
        }

        public void DiscoverByClusterKey(string name)
        {
            Cfg = new ClusterAutoConfig(_terminator, name);
        }

        public void DiscoverByClusterKey(string name, int gossipOnPort)
        {
            Cfg = new ClusterAutoConfig(_terminator, name, gossipOnPort);
        }
    }
}