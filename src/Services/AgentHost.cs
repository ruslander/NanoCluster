using System;
using System.Threading;
using System.Threading.Tasks;
using NanoCluster.Config;
using NLog;

namespace NanoCluster.Services
{
    public class AgentHost
    {
        private static readonly Logger Logger = LogManager.GetLogger("AgentHost");

        public static void Run(string name, Action mainLoop, ClusterConfig cfg, CancellationTokenSource terminator)
        {
            Task.Factory.StartNew(() =>
            {
                var workerThread = new Thread(new ThreadStart(mainLoop)) {Name = name};
                workerThread.Start();

                Logger.Info(name.ToUpper() + " agent started [{0}]", cfg.Host);

                while (!terminator.IsCancellationRequested) ;

                Logger.Info("Disposing " + name.ToUpper());

                workerThread.Abort();

            }, terminator.Token);
        }
    }
}