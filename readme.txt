NanoCluster

Yes, a small footprint clustering infrastructure over NetMq.
Leader election is done accoring to Bully algorhitm http://en.wikipedia.org/wiki/Bully_algorithm


How to use it 

1) define the list of host:port for each member you want to participate in the cluster
3) use host:port to create an instance of NanoClusterEngine in each app domain
2) at run-time you can check the IsCoordinatorProcess on NanoClusterEngine 
 

Usage

var cluster = new NanoClusterEngine(
    // engine host	
    "tcp://localhost:5555",

    // all members, ascending prioritized, from least to most important
    "tcp://localhost:5555,tcp://localhost:5556,tcp://localhost:5557"
    );

while (true)
{
    Thread.Sleep(1000);
    var whoami = (cluster.IsCoordinatorProcess ? "leader" : "follower");
    Console.WriteLine("Cli \\> " + whoami);
}

