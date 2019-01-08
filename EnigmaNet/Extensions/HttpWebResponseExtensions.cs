using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Extensions
{
    public static class HttpWebResponseExtensions
    {
        public static async Task<string> ReadAsStringAsync(this HttpWebResponse source)
        {
            return await ReadAsStringAsync(source, Encoding.UTF8);
        }
        public static async Task<string> ReadAsStringAsync(this HttpWebResponse source, Encoding encoding)
        {
            using (var stream = source.GetResponseStream())
            {
                using (var reader = new StreamReader(stream, encoding))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}
