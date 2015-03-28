using System;
using H.Core;
using NanoCluster;

namespace H2
{
    class Program
    {
        static void Main(string[] args)
        {
            var cluster = new NanoClusterEngine(cfg =>
            {
                cfg.DiscoverByClusterKey("Chat");
                cfg.DistributedTransactions = new ClusteredChatProcess();
            });

            while (true)
            {
                Console.Write(cluster.WhoAmI() + "\\>");
                var input = Console.ReadLine();

                var text = string.Format("{0} | {1}", cluster.WhoAmI(), input);

                cluster.Send(new NewMessage() { Text = text });
            }
        }
    }
}
