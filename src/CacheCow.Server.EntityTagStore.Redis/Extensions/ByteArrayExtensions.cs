using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using CacheCow.Common;

namespace CacheCow.Server.EntityTagStore.Redis.Extensions {

    internal static class ByteArrayExtensions {

        internal static CacheEntity ToCacheEntity(this byte[] bytes) {

            using (MemoryStream stream = new MemoryStream(bytes)) {

                return (CacheEntity)(new BinaryFormatter().Deserialize(stream));
            }
        }
    }
}
