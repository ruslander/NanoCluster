using System;
using NanoCluster.Config;
using NanoCluster.Pipeline;
using NetMQ;

namespace NanoCluster
{
    public class ClusteredNode
    {
        private readonly ClusterConfig _config;
        public DistributedProcess Process { get; set; }

        public ClusteredNode(ClusterConfig config)
        {
            _config = config;
        }

        public void Run()
        {
            using (var context = NetMQContext.Create())
            using (var mainLoop = context.CreateResponseSocket())
            {
                mainLoop.Options.ReceiveTimeout = _config.ElectionMessageReceiveTimeoutSeconds;
                mainLoop.Bind(_config.Host);

                while (true)
                {
                    var message = string.Empty;

                    try
                    {
                        message = mainLoop.ReceiveString();
                    }
                    catch (AgainException e)
                    {
                    }

                    if (message == "election")
                    {
                        mainLoop.Send("ok");
                    }

                    if (message == "send")
                    {
                        var payload = mainLoop.ReceiveString();

                        var typedMsg = BinarySerializer.Deserialize(payload);
                        Process.Dispatch(typedMsg);

                        mainLoop.Send("pong");
                    }

                    if (message == "catchup")
                    {
                        var ver = mainLoop.ReceiveString();
                        var delta = Process.Delta(int.Parse(ver));
                        
                        var payload = BinarySerializer.Serialize(delta);
                        mainLoop.Send(payload);
                    }
                }
            }
        }

        public object Send(string host, object message)
        {
            using (var context = NetMQContext.Create())
            using (var client = context.CreateRequestSocket())
            {
                client.Connect(host);
                client.Options.ReceiveTimeout = _config.ElectionMessageReceiveTimeoutSeconds;

                var payload = BinarySerializer.Serialize(message);
                client.SendMore("send").Send(payload);

                var reply = string.Empty;

                try
                {
                    reply = client.ReceiveString();
                }
                catch (AgainException e)
                {
                }

                client.Options.Linger = TimeSpan.FromSeconds(1); //required
                client.Close(); //required

                return reply;
            }
        }
    }
}