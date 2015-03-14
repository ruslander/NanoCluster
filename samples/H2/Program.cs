using System;
using H.Core;
using NanoCluster;

namespace H2
{
    class Program
    {
        static void Main(string[] args)
        {
            var cluster = new NanoClusterEngine(new ClusteredChatProcess());

            while (true)
            {
                Console.Write(cluster.WhoAmI() + "\\>");
                var input = Console.ReadLine();

                var text = string.Format("{1}> {0} | {2}", cluster.WhoAmI(), DateTime.Now.ToShortTimeString(), input);

                cluster.Send(new NewMessage() { Text = text });
            }
        }
    }
}
