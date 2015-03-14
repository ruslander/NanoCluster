using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NanoCluster
{
    public class BinarySerializer
    {
        public static string Serialize(object obj)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Flush();
                stream.Position = 0;
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static object Deserialize(string payload)
        {
            var b = Convert.FromBase64String(payload);
            using (var stream = new MemoryStream(b))
            {
                var formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(stream);
            }
        }
    }
}