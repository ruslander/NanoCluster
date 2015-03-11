using NanoCluster.Config;
using NetMQ;

namespace NanoCluster
{
    public class ElectionReplayEndpoint
    {
        public void Run(ClusterConfig cfg2)
        {
            using (NetMQContext context = NetMQContext.Create())
            using (NetMQSocket responder = context.CreateResponseSocket())
            {
                responder.Options.ReceiveTimeout = cfg2.ElectionMessageReceiveTimeoutSeconds;
                cfg2.BindByConfigType(responder);

                for (;;)
                {
                    var message = string.Empty;

                    try
                    {
                        message = responder.ReceiveString();
                    }
                    catch (AgainException e)
                    {
                    }

                    if (message == "ELECTION")
                    {
                        responder.Send("OK");
                    }
                }
            }
        }
    }
}