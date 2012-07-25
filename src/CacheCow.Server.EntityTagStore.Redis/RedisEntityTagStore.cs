using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookSleeve;
using CacheCow.Common;

namespace CacheCow.Server.EntityTagStore.Redis {

    public class RedisEntityTagStore : IEntityTagStore {

        //TODO: All the code inside this class has been written in a blocking fashion
        //      because the methods are not asynchronus. Making the extensibility 
        //      points async should be the solution.
        //      The methods which has void return expression has been implemented as fire-and-forget.

        private const char _delimiter = '$';
        private readonly int _db;
        private static Lazy<RedisConnection> _redisConn;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="password"></param>
        public RedisEntityTagStore(string host, int database, int port = 6379, string password = null) {
            
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

            _db = database;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eTag"></param>
        /// <returns></returns>
        public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag) {

            if (key == null) {
                throw new ArgumentNullException("key");
            }

            eTag = null;

            var resultValue = _redisConn.Value.Strings.GetString(_db, key.ToString()).Result;

            if (string.IsNullOrEmpty(resultValue)) {
                return false;
            }

            var values = resultValue.Split(_delimiter);
            eTag = new TimedEntityTagHeaderValue(values[0]) {
                LastModified = DateTimeOffset.Parse(values[1])
            };

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eTag"></param>
        public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag) {

            if (key == null) {
                throw new ArgumentNullException("key");
            }

            if (eTag == null) {
                throw new ArgumentNullException("eTag");
            }

            var finalValue = string.Format("{0}{1}{2}", eTag.Tag, _delimiter, eTag.LastModified.ToString());

            var setTask = _redisConn.Value.Strings.Set(_db, key.ToString(), finalValue);

            if (setTask.IsCompleted) {

                if (setTask.Status == TaskStatus.RanToCompletion) {

                    AddRoutePattern(key.ToString(), key.RoutePattern);
                }
            }
            else {

                setTask.ContinueWith(task => {

                    if (task.Status == TaskStatus.RanToCompletion) {

                        AddRoutePattern(key.ToString(), key.RoutePattern);
                    }
                    else if (task.Status == TaskStatus.Canceled) {

                        //Swallow
                    }
                    else if (task.Status == TaskStatus.Faulted) {

                        //Swallow
                    }
                });
            }
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

            //NOTE: This finds all the keys inside the redis databse.
            //      If the user uses this database with something else,
            //      we would be deleting all of those recods as well and
            //      this is obviously bad. The solution would be to stick a perfix
            //      for each key and then do the search against that. 
            //      E.g: CacheCow::{keyName}
            //      So that we could do a seach against that as below:
            //      _redisConn.Value.Keys.Find(_db, "CacheCow::*")
            var findKeysTask = _redisConn.Value.Keys.Find(_db, "*");

            if (findKeysTask.IsCompleted) {

                if (findKeysTask.Status == TaskStatus.RanToCompletion) {

                    ClearAll(findKeysTask.Result);
                }
            }
            else {

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
        }

        private void ClearAll(string[] keys) {

            for (int i = 0; i < keys.Length; i++) {

                //Fire and forget
                _redisConn.Value.Keys.Remove(_db, keys[i]);
            }
        }

        private Task<bool> AddRoutePattern(string cacheKey, string routePattern) {

            return _redisConn.Value.Sets.Add(_db, routePattern, cacheKey);
        }
    }
}