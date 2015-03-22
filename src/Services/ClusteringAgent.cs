using System;
using System.Threading;
using NanoCluster.Config;
using NanoCluster.Pipeline;
using NetMQ;
using NLog;

namespace NanoCluster.Services
{
    public class ClusteringAgent
    {
        private readonly ClusterConfig _cfg;
        private readonly CancellationTokenSource _terminator;

        private static readonly Logger Logger = LogManager.GetLogger("ClusteringAgent");

        public DistributedProcess Process { get; set; }

        public ClusteringAgent(ClusterConfig cfg, CancellationTokenSource terminator)
        {
            _cfg = cfg;
            _terminator = terminator;
        }

        public void Run()
        {
            using (NetMQContext _context = NetMQContext.Create())
            using (var mainLoop = _context.CreateResponseSocket())
            {
                mainLoop.Options.ReceiveTimeout = _cfg.ElectionMessageReceiveTimeoutSeconds;
                mainLoop.Bind(_cfg.Host);

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

                    Logger.Debug("Request " + message);

                    if (message == "election")
                    {
                        mainLoop.Send("ok");
                        Logger.Debug("Reply : ok");
                    }

                    if (message == "send")
                    {
                        var payload = mainLoop.ReceiveString();

                        var typedMsg = BinarySerializer.Deserialize(payload);
                        Process.Dispatch(typedMsg);

                        mainLoop.Send("pong");
                        Logger.Debug("Reply : pong");
                    }

                    if (message == "catchup")
                    {
                        var ver = mainLoop.ReceiveString();
                        var delta = Process.Delta(int.Parse(ver));

                        var payload = BinarySerializer.Serialize(delta);
                        mainLoop.Send(payload);
                        Logger.Debug("Reply : payload");

                    }
                }
            }
        }

        public object Send(string host, object message)
        {
            using (NetMQContext _context = NetMQContext.Create())
            using (var client = _context.CreateRequestSocket())
            {
                client.Connect(host);
                client.Options.ReceiveTimeout = _cfg.ElectionMessageReceiveTimeoutSeconds;

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