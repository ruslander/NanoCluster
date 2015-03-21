using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NanoCluster.Config;
using NanoCluster.Pipeline;
using NanoCluster.Services;

namespace NanoCluster
{
    public class NanoClusterEngine
    {
        public DistributedProcess Process = new DistributedProcess();
        private CancellationTokenSource terminator = new CancellationTokenSource();
        
        ElectionAgent _elector;
        ClusteringAgent _clusteringAgent;
        LeaderCatchup _catchup;
        ClusterConfig _config;

        public bool IsLeadingProcess {
            get { return _elector.IsLeadingProcess; }
        }

        public NanoClusterEngine(DistributedProcess process)
        {
            Process = process;

            _config = new ClusterAutoConfig(terminator);
            Bootstrap(_config, Process);
        }

        public NanoClusterEngine(string name)
        {
            _config = new ClusterAutoConfig(terminator) { ClusterName = name };
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
            _elector = new ElectionAgent(config, terminator);
            _clusteringAgent = new ClusteringAgent(config, terminator) { Process = process };
            _catchup = new LeaderCatchup(_elector, config, terminator);

            Task.Factory.StartNew(() => { _elector.Run(); }, terminator.Token);
            Task.Factory.StartNew(() => { _clusteringAgent.Run(); }, terminator.Token);

            while (string.IsNullOrEmpty(_elector.LeaderHost))
            {
                Thread.Sleep(1000);
            }

            Task.Factory.StartNew(() => { _catchup.Run(process); }, terminator.Token);
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
                _clusteringAgent.Send(_elector.LeaderHost, message);
            }
            catch (SerializationException e)
            {
                throw new InvalidOperationException(message.GetType().Name + " Must be decorated with [Serializable] attribute");
            }
        }

        public void Dispose()
        {
            _config.Dispose();
            terminator.Cancel();
            Thread.Sleep(100);
        }
    }
}