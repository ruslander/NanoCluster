using NUnit.Framework;

namespace NanoCluster.Tests
{
    [TestFixture]
    public class HowToUseTests
    {
        [Test]
        public void One_node_will_lead()
        {
            var cl = new NanoClusterEngine("A");

            Assert.IsTrue(cl.IsLeadingProcess);

            cl.Dispose();
        }

        [Test]
        public void In_two_nodes_cluster_one_will_lead()
        {
            using (var node1 = new NanoClusterEngine("A"))
            using (var node2 = new NanoClusterEngine("A"))
            {
                Assert.IsTrue(node1.IsLeadingProcess);
                Assert.False(node2.IsLeadingProcess);
            }
        }

        [Test]
        public void Two_nodes_will_not_join_if_have_different_keys()
        {
            using (var node1 = new NanoClusterEngine("A"))
            using (var node2 = new NanoClusterEngine("B"))
            {
                Assert.IsTrue(node1.IsLeadingProcess);
                Assert.IsTrue(node2.IsLeadingProcess);
            }
        }
    }
}
