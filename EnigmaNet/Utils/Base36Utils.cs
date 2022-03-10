using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Utils
{
    /// <summary>
    /// Base36
    /// </summary>
    /// <remarks>
    /// 数字 + 小写字母
    /// 参考在线工具：https://tool.lu/hexconvert/
    /// </remarks>
    public static class Base36Utils
    {
        const int Radix = 36;
        //const string Charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string Charset = "0123456789abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Encode an `ulong` to a Base36 string
        /// </summary>
        /// <param name="input">The number to encode</param>
        /// <returns>The encoded string</returns>
        public static string Encode(ulong input)
        {
            var charset = Charset.AsSpan();
            var sb = new StringBuilder();

            while (input > 0)
            {
                sb.Insert(0, charset[(int)(input % Radix)]);
                input /= Radix;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Encode an `ulong` to a Base36 string
        /// </summary>
        /// <param name="input">The number to encode</param>
        /// <returns>The encoded string</returns>
        public static string Encode(long input)
        {
            return Encode((ulong)input);
        }

        /// <summary>
        /// Decode a Base36 string to an `ulong`
        /// </summary>
        /// <param name="input">The string to decode</param>
        /// <returns>The decoded number</returns>
        public static ulong Decode(string input)
        {
            var charset = Charset.AsSpan();
            var chars = input.ToLower().AsSpan();
            var len = chars.Length;

            ulong result = 0;
            var iter = 0;

            for (var i = len - 1; i >= 0; i--)
            {
                result += (ulong)charset.IndexOf(chars[i]) * (ulong)Math.Pow(Radix, iter++);
            }

            return result;
        }

    }
}
