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
                Logger.Info(name.ToUpper() + " agent started [{0}]", cfg.Host);

                try
                {
                    using (terminator.Token.Register(Thread.CurrentThread.Abort))
                    {
                        mainLoop();
                    }
                }
                catch (ThreadAbortException)
                {
                    Logger.Info("Disposing " + name.ToUpper());
                }

            }, terminator.Token);
        }
    }
}