using NetMQ;

namespace NanoCluster.Config
{
    public class ClusterStaticConfig : ClusterConfig
    {
        public ClusterStaticConfig(string host, string membersByPriority)
        {
            Host = host;
            ApplyChangedPriorityList(membersByPriority.Split(','));
        }

        public override void BindByConfigType(NetMQSocket responder)
        {
            responder.Bind(Host);
        }
    }
}