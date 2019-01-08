using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 字串为空则替换成指定字符串
        /// </summary>
        /// <param name="source"></param>
        /// <param name="becomeValue"></param>
        /// <returns></returns>
        public static string IfEmptyBecome(this string source, string becomeValue)
        {
            if (string.IsNullOrEmpty(source))
            {
                return becomeValue;
            }
            else
            {
                return source;
            }
        }

    }
}
