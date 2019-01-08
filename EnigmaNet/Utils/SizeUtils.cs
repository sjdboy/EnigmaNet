using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Utils
{
    public static class SizeUtils
    {
        /// <summary>
        /// 获取尺寸描述
        /// </summary>
        /// <param name="bitLength">大小（bit）</param>
        /// <param name="decimals">小数位数</param>
        /// <returns></returns>
        public static string GetSizeDescript(long bitLength, int decimals = 0)
        {
            string sizeName = "0 K";
            if (0 < bitLength && bitLength < 1024.00)
            {
                sizeName = "1K";
            }
            else if (bitLength >= 1024.00 && bitLength < 1048576)
            {
                sizeName = (bitLength / 1024.00).ToString("F" + decimals) + " K";
            }
            else if (bitLength >= 1048576 && bitLength < 1073741824)
            {
                sizeName = (bitLength / 1024.00 / 1024.00).ToString("F" + decimals) + " M";
            }
            else if (bitLength >= 1073741824)
            {
                sizeName = (bitLength / 1024.00 / 1024.00 / 1024.00).ToString("F" + decimals) + " G";
            }
            return sizeName;
        }
    }
}
