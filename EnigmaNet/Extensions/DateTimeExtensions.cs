using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }

        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }

        public static long? ToUnixTimeSeconds(this DateTime? dateTime)
        {
            return dateTime?.ToUnixTimeSeconds();
        }

        public static long? ToUnixTimeMilliseconds(this DateTime? dateTime)
        {
            return dateTime?.ToUnixTimeMilliseconds();
        }

        public static DateTime FromUnixTimeSeconds(this long value)
        {
            return DateTimeOffset.FromUnixTimeSeconds(value).LocalDateTime;
        }

        public static DateTime FromUnixTimeMilliseconds(this long value)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(value).LocalDateTime;
        }

        public static DateTime? FromUnixTimeSeconds(this long? value)
        {
            return value?.FromUnixTimeSeconds();
        }

        public static DateTime? FromUnixTimeMilliseconds(this long? value)
        {
            return value?.FromUnixTimeMilliseconds();
        }
    }
}
