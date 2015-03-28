using System.Threading;
using NUnit.Framework;

namespace NanoCluster.Tests
{
    [TestFixture]
    public class ConfigurationDslTests
    {
        [Test]
        public void One_node_cluster_with_autodiscovery_configuraton()
        {
            using (var n = new NanoClusterEngine())
            {
                Assert.IsTrue(n.IsLeadingProcess);
            }
        }

        [Test]
        public void One_node_cluster_with_static_configuraton()
        {
            using (var n = new NanoClusterEngine(
                "tcp://localhost:5555",
                "tcp://localhost:5555,tcp://localhost:5556"
            ))
            {
                Assert.IsTrue(n.IsLeadingProcess);
            }
        }

        [Test]
        public void Two_node_cluster_with_static_configuraton_leads_are_orderd_from_least_to_most_important()
        {
            var n1 = new NanoClusterEngine(
                "tcp://localhost:5555",
                "tcp://localhost:5555,tcp://localhost:5556"
            );

            var n2 = new NanoClusterEngine(cfg =>
            {
                cfg.UseStaticTopology(
                    "tcp://localhost:5556",
                    "tcp://localhost:5555,tcp://localhost:5556");
            });

            Thread.Sleep(5000);

            Assert.IsFalse(n1.IsLeadingProcess);
            Assert.IsTrue(n2.IsLeadingProcess);

            n2.Dispose();

            Thread.Sleep(5000);

            Assert.IsTrue(n1.IsLeadingProcess);
            
            n1.Dispose();
        }

        [Test]
        public void Two_node_cluster_with_dynamic_configuraton_peers_join_by_key()
        {
            using (var node1 = new NanoClusterEngine(cfg => { cfg.DiscoverByClusterKey("A");}))
            using (var node2 = new NanoClusterEngine(cfg => { cfg.DiscoverByClusterKey("A");}))
            {
                Assert.IsTrue(node1.IsLeadingProcess);
                Assert.False(node2.IsLeadingProcess);
            }
        }

        [Test]
        public void Two_clusters_with_dynamic_configuraton_peers_join_by_key()
        {
            using (var node1 = new NanoClusterEngine(cfg => { cfg.DiscoverByClusterKey("Cl1",7000); }))
            using (var node2 = new NanoClusterEngine(cfg => { cfg.DiscoverByClusterKey("Cl2",9999); }))
            using (var node3 = new NanoClusterEngine(cfg => { cfg.DiscoverByClusterKey("Cl2",9999); }))
            {
                Thread.Sleep(5000);

                Assert.IsTrue(node1.IsLeadingProcess);
                Assert.IsTrue(node2.IsLeadingProcess);
                Assert.IsFalse(node3.IsLeadingProcess);
            }
        }
    }
}
