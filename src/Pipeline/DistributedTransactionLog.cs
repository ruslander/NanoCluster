using System.Collections.Generic;
using System.Linq;

namespace NanoCluster.Pipeline
{
    public class DistributedTransactionLog
    {
        public int Version = 0;
        public List<object> Changes = new List<object>();

        public void Apply(object evt)
        {
            Version++;
            Changes.Add(evt);

            ((dynamic)this).When((dynamic)evt);
        }

        public void Dispatch(object cmd)
        {
            ((dynamic)this).Handle((dynamic)cmd);
        }

        public List<object> Delta(int fromV)
        {
            return Changes.Skip(fromV).ToList();
        }
    }
}