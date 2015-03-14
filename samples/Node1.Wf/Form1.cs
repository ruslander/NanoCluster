using System;
using System.Windows.Forms;
using NanoCluster;

namespace Node1.Wf
{
    public partial class Form1 : Form
    {
        private readonly NanoClusterEngine _cluster;

        public Form1()
        {
            InitializeComponent();

            _cluster = new NanoClusterEngine(
               "tcp://localhost:5556",
               "tcp://localhost:5555,tcp://localhost:5556,tcp://localhost:5557"
               );
        }

        private void tmrRole_Tick(object sender, EventArgs e)
        {
            lblCoordinationStatus.Text = (_cluster.IsLeadingProcess ? "leader" : "follower");
        }
    }
}
