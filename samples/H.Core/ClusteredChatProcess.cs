using System;
using NanoCluster.Pipeline;

namespace H.Core
{
    public class ClusteredChatProcess : DistributedProcess
    {
        public void Handle(NewMessage message)
        {
            Apply(new MessageAccepted(){Text = message.Text});
        }

        public void When(MessageAccepted evt)
        {
            Console.WriteLine(evt.Text);
        }
    }

    [Serializable]
    public class MessageAccepted
    {
        public string Text { get; set; }
    }

    [Serializable]
    public class NewMessage
    {
        public string Text { get; set; }
    }
}
