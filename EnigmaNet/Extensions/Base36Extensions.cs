using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnigmaNet.Utils;

namespace EnigmaNet.Extensions
{
    public static class Base36Extensions
    {
        public static string ToBase36(this ulong source)
        {
            return Base36Utils.Encode(source);
        }

        public static string ToBase36(this long source)
        {
            return Base36Utils.Encode(source);
        }

        public static string ToBase36(this int source)
        {
            return Base36Utils.Encode(source);
        }

        public static long FromBase36(this string source)
        {
            return (long)Base36Utils.Decode(source);
        }
    }
}
