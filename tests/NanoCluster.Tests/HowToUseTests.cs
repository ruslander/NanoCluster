using System;
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
            var node1 = new NanoClusterEngine("B");
            var node2 = new NanoClusterEngine("B");

            try
            {
                Assert.IsTrue(node1.IsLeadingProcess);
                Assert.False(node2.IsLeadingProcess);

                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                node1.Dispose();
                node2.Dispose();
            }
        }
    }
}
