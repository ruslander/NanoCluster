using System;
using System.Linq;

namespace NanoCluster
{
    public class ClusterConfig
    {
        public string Host { get; private set; }
        public string[] PriorityList { get; private set; }
        public string[] AuthoritiesToMe { get; private set; }

        public TimeSpan ElectionMessageReceiveTimeoutSeconds
        {
            get { return TimeSpan.FromSeconds(.5); }
        }

        public TimeSpan ElectionIntervalSeconds
        {
            get { return TimeSpan.FromSeconds(2.5); }
        }

        public ClusterConfig(string host, string membersByPriority)
        {
            Host = host;
            PriorityList = membersByPriority.Split(',');

            var indexOf = Array.IndexOf(PriorityList, host) + 1;

            AuthoritiesToMe = PriorityList.ToList()
                .GetRange(indexOf, PriorityList.Length - indexOf)
                .ToArray()
                .Reverse()
                .ToArray();
        }

        public override string ToString()
        {
            return Array.IndexOf(PriorityList, Host) + ")    <<" + Host + ">>        [" + string.Join(",", AuthoritiesToMe.Select(x => x.ToString()).ToArray()) + "]";
        }
    }
}