using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Common {

    [Serializable]
    public class CacheEntity {

        public byte[] CacheKeyHash { get; set; }
        public string RoutePattern { get; set; }
        public string ETag { get; set; }
        public DateTimeOffset LastModified { get; set; }
    }
}