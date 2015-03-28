﻿using System;
using System.Collections.Generic;
using System.Threading;
using H.Core;
using NanoCluster;
using NanoCluster.Pipeline;

namespace H1
{
    class Program
    {
        static void Main(string[] args)
        {
            var leaderLog = new InProcessDistributedCache();
            var followerLog = new InProcessDistributedCache();

            using (var leader = new NanoClusterEngine(cfg =>
            {
                cfg.DiscoverByClusterKey("Cache");
                cfg.DistributedTransactions = leaderLog;
            }))
            using (var follower = new NanoClusterEngine(cfg =>
            {
                cfg.DiscoverByClusterKey("Cache");
                cfg.DistributedTransactions = followerLog;
            }))
            {
                Thread.Sleep(3000);

                Console.WriteLine(leader.IsLeadingProcess);

                Console.ReadKey();

                Thread.Sleep(3000);

                follower.Send(new StoreCommand() { Key = "user", Value = "rsln" });

                Thread.Sleep(15000);

                Console.WriteLine(leaderLog.Get("user"));
                Console.WriteLine(followerLog.Get("user"));

                Console.ReadKey();
            }
        }
    }


    [Serializable]
    public class StoreCommand
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    [Serializable]
    public class EvictCommand
    {
        public string Key { get; set; }
    }

    [Serializable]
    public class ItemDeleted
    {
        public string Key { get; set; }
    }

    [Serializable]
    public class ItemAdded
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class InProcessDistributedCache : DistributedTransactionLog
    {
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

        public void Handle(StoreCommand cmd)
        {
            if (_cache.ContainsKey(cmd.Key))
                return;

            Apply(new ItemAdded() { Key = cmd.Key, Value = cmd.Value });
        }

        public void Handle(EvictCommand cmd)
        {
            if (_cache.ContainsKey(cmd.Key))
                return;

            Apply(new ItemDeleted() { Key = cmd.Key });
        }

        public void When(ItemAdded add)
        {
            _cache.Add(add.Key, add.Value);
        }

        public void When(ItemDeleted delete)
        {
            _cache.Remove(delete.Key);
        }

        public string Get(string key)
        {
            return _cache[key];
        }
    }
}
