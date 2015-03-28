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
        private readonly NetMQContext _context = NetMQContext.Create();

        public DistributedTransactionLog Transactions { get; set; }

        public ClusteringAgent(ClusterConfig cfg, CancellationTokenSource terminator)
        {
            _cfg = cfg;
            _terminator = terminator;
        }

        public void Run()
        {
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
                        Transactions.Dispatch(typedMsg);

                        mainLoop.Send("dispatched");
                        Logger.Debug("Reply : dispatched");
                    }

                    if (message == "catchup")
                    {
                        var ver = mainLoop.ReceiveString();
                        var delta = Transactions.Delta(int.Parse(ver));

                        var payload = BinarySerializer.Serialize(delta);
                        mainLoop.Send(payload);
                        Logger.Debug("Reply : payload");

                    }
                }
            }
        }

        public object Send(string host, object message)
        {
            using (var client = _context.CreateRequestSocket())
            {
                client.Connect(host);
                client.Options.ReceiveTimeout = _cfg.ElectionMessageReceiveTimeoutSeconds;

                var payload = BinarySerializer.Serialize(message);
                client.SendMore("send").Send(payload);

                var reply = string.Empty;

                try
                {
                    Logger.Info("Forward to leader :" + host);
                    reply = client.ReceiveString();
                    Logger.Info("Leader replay result : " + reply);
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