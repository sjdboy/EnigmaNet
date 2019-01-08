using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Utils
{
    public static class DateTimeUtils
    {
        static readonly DateTime START_UT_DT = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0));

        /// <summary>
        /// Net时间转换为UnixTime时间戳
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static double ToUnixTime(DateTime dt)
        {
            return Math.Floor((dt - START_UT_DT).TotalSeconds);
        }

        /// <summary>
        /// Net时间转换为UnixTime时间戳
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static long ToUnixTime2(DateTime dt)
        {
            return Convert.ToInt64(Math.Floor((dt - START_UT_DT).TotalSeconds));
        }

        /// <summary>
        /// UnixTime时间戳转换Net时间
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(double v)
        {
            return START_UT_DT.AddSeconds(v);
        }
    }
}
