using System;
using System.Runtime.Serialization;
using System.Threading;
using NanoCluster.Config;
using NanoCluster.Pipeline;
using NanoCluster.Services;

namespace NanoCluster
{
    public class NanoClusterEngine : IDisposable
    {
        public DistributedProcess Process = new DistributedProcess();

        private readonly CancellationTokenSource _terminator = new CancellationTokenSource();
        
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

            _config = new ClusterAutoConfig(_terminator);
            Bootstrap(_config, Process);
        }

        public NanoClusterEngine(string name)
        {
            _config = new ClusterAutoConfig(_terminator) { ClusterName = name };
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
            _elector = new ElectionAgent(config, _terminator);
            _clusteringAgent = new ClusteringAgent(config, _terminator) { Process = process };
            _catchup = new LeaderCatchup(_elector, config, _terminator) { Process = process };

            AgentHost.Run("clustering",_clusteringAgent.Run, _config, _terminator);
            AgentHost.Run("elector",_elector.Run, _config, _terminator);

            while (string.IsNullOrEmpty(_elector.LeaderHost))
                Thread.Sleep(1000);

            AgentHost.Run("catchup", _catchup.Run, _config, _terminator);
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
            _terminator.Cancel();
            _config.Dispose();

            Thread.Sleep(100);
        }
    }
}