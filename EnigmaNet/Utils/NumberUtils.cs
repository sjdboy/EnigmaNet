using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Utils
{
    public static class NumberUtils
    {
        /// <summary>
        /// 获取小数位数
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static int GetDecimalLength(decimal number)
        {
            return BitConverter.GetBytes(decimal.GetBits(number)[3])[2];
        }
    }
}
