using System.Threading.Tasks;
using NanoCluster.Config;

namespace NanoCluster
{
    public class NanoClusterEngine
    {
        ContinuousElectionRunner _elector;

        public bool IsCoordinatorProcess {
            get { return _elector.IsCoordinatorProcess; }
        }

        public NanoClusterEngine()
        {
            var config = new ClusterAutoConfig();
            Bootstrap(config);
        }

        public NanoClusterEngine(string host, string membersByPriority)
        {
            var config = new ClusterStaticConfig(host, membersByPriority);
            Bootstrap(config);
        }

        private void Bootstrap(ClusterConfig config)
        {
            _elector = new ContinuousElectionRunner(config);

            var electionReplayer = new ElectionReplayEndpoint();

            Task.Factory.StartNew(() => electionReplayer.Run(config));
            Task.Factory.StartNew(() => { _elector.Run(); });
        }
    }
}