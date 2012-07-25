using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using CacheCow.Common;

namespace CacheCow.Server.EntityTagStore.Redis.Extensions {

    internal static class CacheEntityExtensions {

        internal static byte[] ToByteArray(this CacheEntity cacheEntity) {

            using (MemoryStream stream = new MemoryStream()) {

                new BinaryFormatter().Serialize(stream, cacheEntity);
                return stream.ToArray();
            }
        }

        internal static TimedEntityTagHeaderValue ToTimedEntityTagHeaderValue(this CacheEntity cacheEntity) {

            return new TimedEntityTagHeaderValue(cacheEntity.ETag) { 
                LastModified = cacheEntity.LastModified 
            };
        }
    }
}
