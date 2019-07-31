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
            var content = await database.StringGetAsync(key, flags);
            if (content.IsNullOrEmpty || string.IsNullOrEmpty(content))
            {
                return default(T);
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
        }

        public static async Task<IDictionary<string, T>> ModelGetAsync<T>(this IDatabase database, ISet<string> keys, CommandFlags flags = CommandFlags.None)
        {
            if (keys == null || keys.Count == 0)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var keyList = keys.Select(m => (RedisKey)m).ToArray();

            var contents = await database.StringGetAsync(keyList, flags);

            var list = new Dictionary<string, T>();

            for (var i = 0; i < keyList.Length; i++)
            {
                var key = keyList[i];
                var content = contents[i];

                T t;
                if (content.IsNullOrEmpty || string.IsNullOrEmpty(content))
                {
                    t = default(T);
                }
                else
                {
                    t = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
                }

                list.Add(key, t);
            }

            return list;
        }

        public static async Task ModelSetAsync<T>(this IDatabase database, string key, T model, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            RedisValue content;
            if (model != null)
            {
                content = Newtonsoft.Json.JsonConvert.SerializeObject(model);
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
                    content = Newtonsoft.Json.JsonConvert.SerializeObject(item.Value);
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
