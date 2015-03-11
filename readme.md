## NanoCluster  

**NanoCluster** is a non intrusive leader-election implementation for .NET. 

Use it when :

- a set of endpoints are mutating the same state and you want to allow just one
- you want to enforce authority or to coordinate a set of services
- Zookeeper is an overkill

All the boilerplate code is encapsulated in the implementation details which we took care of.  
No leaking abstractions, code against small foot print Api.


## Distribution ##
The minimal configuration you need to get up and running in 3 steps:

**1** Clone this, build and after copy from bin into your project's lib folder 2 DLL's ```NanoCluster.dl``` and ```NetMQ.dll``` 

**2** Reference both DLL's in any endpoint which needs coordination

**3** Agree with you it's not fun, sorry no nuget yet. Yes, is that simple.

## Zero configuration ##
Automatic Peer Discovery, that's right. If you are running in the cloud make sure UDP broadcast is available. Run the parameter-less constructor overload at your endpoint start up

```csharp
var cluster = new NanoClusterEngine();
```
At run-time you can check the ```IsCoordinatorProcess``` on ```NanoClusterEnginev```

## Static configuration ##
This is the way to run it it on premise when the node ip addresses are well known and rarely change.

1. define the list of host:port for each member you want to participate in the cluster   
2. use host:port to create an instance of ```NanoClusterEngine``` in each app domain
3. at run-time you can check the ```IsCoordinatorProcess``` on ```NanoClusterEngine```


```csharp
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
```

## Samples ##
The project comes along with samples. For now we have examples of 2 clusters, static and automatic discovery configuration. 



## Feel free to contribute ##
or ping me at [@ruslanrusu](https://twitter.com/ruslanrusu) if you have any questions