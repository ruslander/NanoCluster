using System.Text;
using NetMQ;

namespace NanoCluster
{
    public class ElectionReplayEndpoint
    {
        public void Run(ClusterConfig cfg2)
        {
            using (NetMQContext context = NetMQContext.Create())
            using (NetMQSocket server = context.CreateResponseSocket())
            {
                server.Options.ReceiveTimeout = cfg2.ElectionMessageReceiveTimeoutSeconds;
                server.Bind(cfg2.Host);

                for (;;)
                {
                    var message = string.Empty;

                    try
                    {
                        message = server.ReceiveString();
                    }
                    catch (AgainException e)
                    {
                    }

                    if (message == "ELECTION")
                    {
                        server.Send("OK");
                    }
                }
            }
        }
    }
}