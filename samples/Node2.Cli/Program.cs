using System;
using System.Threading;
using NanoCluster;

namespace Node2.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var cluster = new NanoClusterEngine(
                "tcp://localhost:5555",
                "tcp://localhost:5555,tcp://localhost:5556,tcp://localhost:5557"
                );

            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Cli \\> " + (cluster.IsCoordinatorProcess ? "leader" : "follower"));
            }
        }
    }
}
