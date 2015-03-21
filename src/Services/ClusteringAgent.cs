using System;
using System.Threading;
using NanoCluster.Config;
using NanoCluster.Pipeline;
using NetMQ;

namespace NanoCluster.Services
{
    public class ClusteringAgent
    {
        private readonly ClusterConfig _config;
        private readonly CancellationTokenSource _terminator;
        public DistributedProcess Process { get; set; }

        public ClusteringAgent(ClusterConfig config, CancellationTokenSource terminator)
        {
            _config = config;
            _terminator = terminator;
        }

        public void Run()
        {
            var workerThread = new Thread(MainLoop);
            workerThread.Start();
            Console.WriteLine("Clustering agent started for '{0}' host.", _config.Host);
            
            while (!_terminator.IsCancellationRequested);

            Console.WriteLine("Disposing Clustering");

            workerThread.Abort();
        }

        private void MainLoop(object obj)
        {
            using (var context = NetMQContext.Create())
            using (var mainLoop = context.CreateResponseSocket())
            {
                mainLoop.Options.ReceiveTimeout = _config.ElectionMessageReceiveTimeoutSeconds;
                mainLoop.Bind(_config.Host);

                while (!_terminator.IsCancellationRequested)
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