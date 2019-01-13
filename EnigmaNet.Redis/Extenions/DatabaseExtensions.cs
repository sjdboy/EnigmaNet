using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace EnigmaNet.Redis.Extenions
{
    public static class DatabaseExtensions
    {
        public static async Task<T> ModelGetAsync<T>(this IDatabase database, string key, CommandFlags flags = CommandFlags.None)
        {
            var content = await database.StringGetAsync(key, flags);
            if (string.IsNullOrEmpty(content))
            {
                return default(T);
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
        }

        public static async Task ModelSetAsync<T>(this IDatabase database, string key, T model, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            var content = Newtonsoft.Json.JsonConvert.SerializeObject(model);

            await database.StringSetAsync(key, content, expiry, when, flags);
        }

    }
}
