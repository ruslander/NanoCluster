using System;
using System.Threading;
using NanoCluster;

namespace Node4.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var cluster = new NanoClusterEngine("A");

            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Cli \\> " + cluster.WhoAmI()); 
            }
        }
    }
}
