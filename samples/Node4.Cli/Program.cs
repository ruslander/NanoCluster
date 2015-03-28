﻿using System;
using System.Threading;
using NanoCluster;

namespace Node4.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var cluster = new NanoClusterEngine();

            while (true)
            {
                Thread.Sleep(5000);
                Console.WriteLine("Cli \\> " + cluster.WhoAmI()); 
            }
        }
    }
}
