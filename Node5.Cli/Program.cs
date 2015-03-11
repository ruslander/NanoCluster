﻿using System;
using System.Threading;
using NanoCluster;

namespace Node5.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var cluster = new NanoClusterEngine();

            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Cli \\> " + (cluster.IsCoordinatorProcess ? "leader" : "follower"));
            }
        }
    }
}
