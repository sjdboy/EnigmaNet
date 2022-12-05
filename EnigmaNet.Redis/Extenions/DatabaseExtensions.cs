using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace EnigmaNet.Redis.Extenions
{
    public static class DatabaseExtensions
    {
        const int PerBatchSize = 50;

        public static async Task<T> ModelGetAsync<T>(this IDatabase database, string key, CommandFlags flags = CommandFlags.None)
        {
            var redisValue = await database.StringGetAsync(key, flags);
            if (redisValue.IsNullOrEmpty)
            {
                return default;
            }

            var tType = typeof(T);
            if (tType.IsValueType || tType == typeof(string) || tType == typeof(Nullable<>))
            {
                return (T)Convert.ChangeType(redisValue, tType);
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(redisValue);
            }
        }

        public static async Task<IDictionary<string, T>> ModelGetAsync<T>(this IDatabase database, ISet<string> keys, CommandFlags flags = CommandFlags.None)
        {
            if (keys == null || keys.Count == 0)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var keyList = keys.Select(m => (RedisKey)m).ToArray();

            var redisValues = await database.StringGetAsync(keyList, flags);

            var list = new Dictionary<string, T>();

            var tType = typeof(T);

            for (var i = 0; i < keyList.Length; i++)
            {
                var key = keyList[i];
                var redisValue = redisValues[i];
                
                T t;
                if (redisValue.IsNullOrEmpty)
                {
                    t = default;
                }
                else
                {
                    if (tType.IsValueType || tType == typeof(string) || tType == typeof(Nullable<>))
                    {
                        t = (T)Convert.ChangeType(redisValue, tType);
                    }
                    else
                    {
                        t = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(redisValue);
                    }
                }

                list.Add(key, t);
            }

            return list;
        }

        public static async Task ModelSetAsync<T>(this IDatabase database, string key, T model, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            var tType = typeof(T);

            RedisValue content;
            if (model != null)
            {
                if (tType.IsValueType || tType == typeof(string))
                {
                    content = model.ToString();
                }
                else
                {
                    content = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                }
            }
            else
            {
                content = RedisValue.Null;
            }

            await database.StringSetAsync(key, content, expiry, when, flags);
        }

        public static Task ModelBatchSetAsync<T>(this IDatabase database, IDictionary<string, T> data, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? batchSize = null)
        {
            if (!(data?.Count > 0))
            {
                throw new ArgumentNullException(nameof(data));
            }

            var tType = typeof(T);

            int perBatchSize;
            if (batchSize == null || batchSize <= 0)
            {
                perBatchSize = PerBatchSize;
            }
            else
            {
                perBatchSize = batchSize.Value;
            }

            var values = new List<KeyValuePair<RedisKey, RedisValue>>();
            foreach (var item in data)
            {
                RedisValue content;
                if (item.Value != null)
                {
                    if (tType.IsValueType || tType == typeof(string))
                    {
                        content = item.Value.ToString();
                    }
                    else
                    {
                        content = Newtonsoft.Json.JsonConvert.SerializeObject(item.Value);
                    }
                }
                else
                {
                    content = RedisValue.Null;
                }

                values.Add(new KeyValuePair<RedisKey, RedisValue>(item.Key, content));
            }

            var skipedCount = 0;
            while (true)
            {
                var list = values.Skip(skipedCount).Take(perBatchSize);
                if (list.Any())
                {
                    var tasks = new List<Task<bool>>();

                    var batch = database.CreateBatch();

                    foreach (var item in list)
                    {
                        var task = batch.StringSetAsync(item.Key, item.Value, expiry, when, flags);
                        tasks.Add(task);
                    }

                    batch.Execute();

                    Task.WaitAll(tasks.ToArray());

                    skipedCount += perBatchSize;
                }
                else
                {
                    break;
                }
            }

            return Task.CompletedTask;
        }
    }
}
