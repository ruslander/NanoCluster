using System;
using System.Diagnostics;
using System.Text;
using Nancy;
using Nancy.Hosting.Self;
using NanoCluster;

namespace Node3.Web
{
    class Program
    {
        internal static NanoClusterEngine Cluster;
 
        static void Main(string[] args)
        {
            Cluster = new NanoClusterEngine(
                "tcp://localhost:5557",
                "tcp://localhost:5555,tcp://localhost:5556,tcp://localhost:5557"
                );

            const string appHost = "http://localhost:8888/";

            using (var nancyHost = new NancyHost(new Uri(appHost)))
            {
                nancyHost.Start();

                Console.WriteLine("Nancy now listening - navigating to http://localhost:8888/. Press enter to stop");
                try
                {
                    Process.Start(appHost);
                }
                catch (Exception)
                {
                }
                Console.ReadKey();
            }

            Console.WriteLine("Stopped. Good bye!");
        }
    }

    public class MainModule : NancyModule
    {
        public MainModule()
        {
            Get["/"] = parameters =>
            {
                var jsonBytes = Encoding.UTF8.GetBytes((Program.Cluster.IsCoordinatorProcess ? "leader" : "follower"));
                return new Response
                {
                    ContentType = "application/json",
                    Contents = s => s.Write(jsonBytes, 0, jsonBytes.Length)
                };
            };
        }
    }
}
