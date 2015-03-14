namespace NanoCluster.Config
{
    public class ClusterStaticConfig : ClusterConfig
    {
        public ClusterStaticConfig(string host, string membersByPriority)
        {
            Host = host;
            ApplyChangedPriorityList(membersByPriority.Split(','));
        }
    }
}