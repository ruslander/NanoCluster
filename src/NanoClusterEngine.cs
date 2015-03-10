using System.Threading.Tasks;

namespace NanoCluster
{
    public class NanoClusterEngine
    {
        readonly ContinuousElectionRunner _elector;

        public bool IsCoordinatorProcess {
            get { return _elector.IsCoordinatorProcess; }
        }

        public NanoClusterEngine(string host, string membersByPriority)
        {
            var config = new ClusterConfig(host, membersByPriority);

            _elector = new ContinuousElectionRunner(config);

            var electionReplayer = new ElectionReplayEndpoint();

            Task.Factory.StartNew(() => electionReplayer.Run(config));
            Task.Factory.StartNew(() =>{ _elector.Run(); });
        }
    }
}