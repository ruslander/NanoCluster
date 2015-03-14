using NanoCluster.Config;
using NetMQ;

namespace NanoCluster
{
    public class ElectionAgent
    {
        public void Run(ClusterConfig cfg)
        {
            using (var context = NetMQContext.Create())
            using (var electionReplay = context.CreateResponseSocket())
            {
                electionReplay.Options.ReceiveTimeout = cfg.ElectionMessageReceiveTimeoutSeconds;
                electionReplay.Bind(cfg.Host);

                for (;;)
                {
                    var message = string.Empty;

                    try
                    {
                        message = electionReplay.ReceiveString();
                    }
                    catch (AgainException e)
                    {
                    }

                    if (message == "ELECTION")
                    {
                        electionReplay.Send("OK");
                    }
                }
            }
        }
    }
}