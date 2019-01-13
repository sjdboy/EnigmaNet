using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.AspNet.Extensions
{
    public static class SessionExtensions
    {
        public static void SetModel<T>(this ISession session, string key, T model)
        {
            var content = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            session.Set(key, System.Text.Encoding.UTF8.GetBytes(content));
        }

        public static T GetModel<T>(this ISession session, string key)
        {
            var data = session.TryGetValue(key, out byte[] value);

            if (value == null)
            {
                return default(T);
            }

            var content = Encoding.UTF8.GetString(value);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
        }
    }
}
