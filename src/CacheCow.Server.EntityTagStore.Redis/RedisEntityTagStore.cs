using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookSleeve;
using CacheCow.Common;
using CacheCow.Server.EntityTagStore.Redis.Extensions;

namespace CacheCow.Server.EntityTagStore.Redis {

    public class RedisEntityTagStore : IEntityTagStore {

        //TODO: All the code inside this class has been written in a blocking fashion
        //      because the methods are not asynchronus. Making the extensibility 
        //      points async should be the solution.
        //      The methods which has void return expression has been implemented as fire-and-forget.

        private const int _db = 6;
        private static Lazy<RedisConnection> _redisConn;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="password"></param>
        public RedisEntityTagStore(string host, int port = 6379, string password = null) {
            
            if (string.IsNullOrEmpty(host)) {
                throw new ArgumentNullException("server");
            }

            _redisConn = new Lazy<RedisConnection>(() => {

                var conn = (password != null) ? 
                           new RedisConnection(host, port, password: password) : 
                           new RedisConnection(host, port);

                //NOTE: Blocking till the connection is verified seems like the best option here.
                conn.Open().Wait();
                return conn;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eTag"></param>
        /// <returns></returns>
        public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag) {

            eTag = null;

            var cEntityBytes = _redisConn.Value.Strings.Get(_db, key.ToString()).Result;
            if (cEntityBytes == null) {
                return false;
            }

            eTag = cEntityBytes.ToCacheEntity().ToTimedEntityTagHeaderValue();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eTag"></param>
        public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag) {

            //TODO: Add the CacheKey to the RoutePattern set based on the RoutePattern as key
            var cEntityBytes = new CacheEntity {    
                CacheKeyHash = key.Hash,
                RoutePattern = key.RoutePattern,
                ETag = eTag.Tag,
                LastModified = eTag.LastModified
            }.ToByteArray();

            //fire-and-forget
            _redisConn.Value.Strings.Set(_db, key.ToString(), cEntityBytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryRemove(CacheKey key) {

            if (key == null) {
                throw new ArgumentNullException("key");
            }

            return _redisConn.Value.Keys.Remove(_db, key.ToString()).Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="routePattern"></param>
        /// <returns></returns>
        public int RemoveAllByRoutePattern(string routePattern) {

            if (string.IsNullOrEmpty(routePattern)) {
                throw new ArgumentNullException("routePattern");
            }

            //If there is no item inside the set based on this routePattern, 
            //the returned value will be empty array
            var cacheKeys = _redisConn.Value.Sets.GetAllString(_db, routePattern).Result;

            foreach (var cacheKey in cacheKeys) {
                _redisConn.Value.Keys.Remove(_db, cacheKey).Wait();
            }

            return cacheKeys.Length;
        }

        /// <summary>
        /// Clears all the values and keys from the database
        /// </summary>
        public void Clear() {

            var findKeysTask = _redisConn.Value.Keys.Find(_db, "*");

            if (findKeysTask.IsCompleted) {
                ClearAll(findKeysTask.Result);
            }
            else if (findKeysTask.IsCanceled) { 

                //Swallow
            }
            else if (findKeysTask.IsFaulted) {

                //Swallow
            }

            findKeysTask.ContinueWith(task => {

                if (task.Status == TaskStatus.RanToCompletion) {
                    ClearAll(findKeysTask.Result);
                }
                else if (task.Status == TaskStatus.Canceled) {

                    //Swallow
                }
                else if (task.Status == TaskStatus.Faulted) {

                    //Swallow
                }
            });
        }

        private void ClearAll(string[] keys) {

            for (int i = 0; i < keys.Length; i++) {

                //Fire and forget
                _redisConn.Value.Keys.Remove(_db, keys[i]);
            }
        }
    }
}