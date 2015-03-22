using System;
using System.Linq;

namespace NanoCluster.Config
{
    public abstract class ClusterConfig : IDisposable
    {
        public string Host { get; protected set; }
        public string[] PriorityList { get; protected set; }
        public string[] AuthoritiesToMe = new string[0];

        public TimeSpan ElectionMessageReceiveTimeoutSeconds
        {
            get { return TimeSpan.FromSeconds(1); }
        }
        
        public TimeSpan MessageReceiveTimeoutSeconds
        {
            get { return TimeSpan.FromSeconds(.5); }
        }

        public TimeSpan ElectionIntervalSeconds
        {
            get { return TimeSpan.FromSeconds(2.5); }
        }

        public override string ToString()
        {
            return Array.IndexOf(PriorityList, Host) + ")    <<" + Host + ">>        [" + string.Join(",", AuthoritiesToMe.Select(x => x.ToString()).ToArray()) + "]";
        }

        public abstract void Dispose();

        protected void ApplyChangedPriorityList(string[] membersByPriority)
        {
            PriorityList = membersByPriority;

            var indexOf = Array.IndexOf(PriorityList, Host) + 1;

            AuthoritiesToMe = PriorityList.ToList()
                .GetRange(indexOf, PriorityList.Length - indexOf)
                .ToArray()
                .Reverse()
                .ToArray();
        }

        public string NodeId()
        {
            return "Node" + Host.Split(':')[2];
        }
    }
}