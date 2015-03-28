using System;
using System.Runtime.Serialization;
using System.Threading;
using NanoCluster.Config;
using NanoCluster.Pipeline;
using NanoCluster.Services;
using NLog;

namespace NanoCluster
{
    public class NanoClusterEngine : IDisposable
    {
        public DistributedTransactionLog TransactionLog = null;
        public ConfigurationModel Configurer = null;

        private readonly CancellationTokenSource _terminator = new CancellationTokenSource();

        private static readonly Logger Logger = LogManager.GetLogger("NanoClusterEngine");
        
        ElectionAgent _elector;
        ClusteringAgent _clusteringAgent;
        LeaderCatchup _catchup;
        ClusterConfig _config;

        public bool IsLeadingProcess {
            get { return _elector.IsLeadingProcess; }
        }

        public NanoClusterEngine()
        {
            _config = new ClusterAutoConfig(_terminator,"");
            Bootstrap(_config, TransactionLog);
        }

        public NanoClusterEngine(string host, string membersByPriority)
        {
            _config = new ClusterStaticConfig(host, membersByPriority);
            Bootstrap(_config, TransactionLog);
        }

        public NanoClusterEngine(Action<ConfigurationModel> configure)
        {
            Configurer = new ConfigurationModel(_terminator);
            configure(Configurer);

            _config = Configurer.Cfg;

            if (Configurer.DistributedTransactions != null)
                TransactionLog = Configurer.DistributedTransactions;

            Bootstrap(_config, TransactionLog);
        }

        public string WhoAmI()
        {
            return (IsLeadingProcess ? "L" : "F") + " " + _config.NodeId();
        }

        private void Bootstrap(ClusterConfig config, DistributedTransactionLog transactions)
        {
            _elector = new ElectionAgent(config, _terminator);
            _clusteringAgent = new ClusteringAgent(config, _terminator) { Transactions = transactions };

            AgentHost.Run("clustering",_clusteringAgent.Run, _config, _terminator);
            AgentHost.Run("elector",_elector.Run, _config, _terminator);

            while (string.IsNullOrEmpty(_elector.LeaderHost))
                Thread.Sleep(1000);
            
            if(transactions == null)
                return;

            _catchup = new LeaderCatchup(_elector, config, _terminator) { Transactions = transactions };
            AgentHost.Run("catchup", _catchup.Run, _config, _terminator);
        }

        public void Send(object message)
        {
            if (IsLeadingProcess)
            {
                Logger.Info("Dispatch to processing pipeline '{0}'", message);
                TransactionLog.Dispatch(message);
                return ;
            }

            Logger.Info("Forwarded to leader '{0}'", message);

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