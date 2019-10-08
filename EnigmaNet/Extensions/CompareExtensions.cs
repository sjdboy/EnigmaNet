using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Extensions
{
    /// <summary>
    /// 比较器扩展
    /// </summary>
    public static class CompareExtensions
    {
        /// <summary>
        /// 是否介于两值之间
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="t"></param>
        /// <param name="lowerBound">下限值</param>
        /// <param name="upperBound">上限值</param>
        /// <param name="includeLowerBound">是否包含上限值</param>
        /// <param name="includeUpperBound">是否包含下限值</param>
        /// <returns></returns>
        public static bool IsBetween<T>(this IComparable<T> t, T lowerBound, T upperBound, bool includeLowerBound = true, bool includeUpperBound = true)
        {
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }

            var lowerCompareResult = t.CompareTo(lowerBound);
            var upperCompareResult = t.CompareTo(upperBound);

            return (includeLowerBound && lowerCompareResult == 0)
                || (includeUpperBound && upperCompareResult == 0)
                || (lowerCompareResult > 0 && upperCompareResult < 0);
        }

        /// <summary>
        /// 是否小于
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue">比较值</param>
        /// <returns></returns>
        public static bool IsLessThan<T>(this IComparable<T> t, T compareValue)
        {
            var compareResult = t.CompareTo(compareValue);
            return compareResult < 0;
        }

        /// <summary>
        /// 是否小于等于
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue">比较值</param>
        /// <returns></returns>
        public static bool IsLessOrEqual<T>(this IComparable<T> t, T compareValue)
        {
            var compareResult = t.CompareTo(compareValue);
            return compareResult <= 0;
        }

        /// <summary>
        /// 是否大于
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue">比较值</param>
        /// <returns></returns>
        public static bool IsGreaterThan<T>(this IComparable<T> t, T compareValue)
        {
            var compareResult = t.CompareTo(compareValue);
            return compareResult > 0;
        }

        /// <summary>
        /// 是否大于等于
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue">比较值</param>
        /// <returns></returns>
        public static bool IsGreaterOrEqual<T>(this IComparable<T> t, T compareValue)
        {
            var compareResult = t.CompareTo(compareValue);
            return compareResult >= 0;
        }

        /// <summary>
        /// 是否相等
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue"></param>
        /// <returns></returns>
        public static bool IsEqual<T>(this IComparable<T> t, T compareValue)
        {
            var compareResult = t.CompareTo(compareValue);
            return compareResult == 0;
        }

        /// <summary>
        /// 如果小于比较值则返回默认值否则返回原值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue">比较值</param>
        /// <param name="becomeValue">默认值</param>
        /// <returns></returns>
        public static T IfLessThanBecome<T>(this IComparable<T> t, T compareValue, T becomeValue)
        {
            if (t.IsLessThan(compareValue))
            {
                return becomeValue;
            }
            else
            {
                return (T)t;
            }
        }

        /// <summary>
        /// 如果小于等于比较值则返回默认值否则返回原值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue">比较值</param>
        /// <param name="becomeValue">默认值</param>
        /// <returns></returns>
        public static T IfLessOrEqualBecome<T>(this IComparable<T> t, T compareValue, T becomeValue)
        {
            if (t.IsLessOrEqual(compareValue))
            {
                return becomeValue;
            }
            else
            {
                return (T)t;
            }
        }

        /// <summary>
        /// 如果大于比较值则返回默认值否则返回原值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue">比较值</param>
        /// <param name="becomeValue">默认值</param>
        /// <returns></returns>
        public static T IfGreaterThanBecome<T>(this IComparable<T> t, T compareValue, T becomeValue)
        {
            if (t.IsGreaterThan(compareValue))
            {
                return becomeValue;
            }
            else
            {
                return (T)t;
            }
        }

        /// <summary>
        /// 如果大于等于比较值则返回默认值否则返回原值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue">比较值</param>
        /// <param name="becomeValue">默认值</param>
        /// <returns></returns>
        public static T IfGreaterOrEqualBecome<T>(this IComparable<T> t, T compareValue, T becomeValue)
        {
            if (t.IsGreaterOrEqual(compareValue))
            {
                return becomeValue;
            }
            else
            {
                return (T)t;
            }
        }

        /// <summary>
        /// 如果等于比较值则返回默认值否则返回原值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="compareValue">比较值</param>
        /// <param name="becomeValue">默认值</param>
        /// <returns></returns>
        public static T IfEqualBecome<T>(this IComparable<T> t, T compareValue, T becomeValue)
        {
            if (t.IsEqual(compareValue))
            {
                return becomeValue;
            }
            else
            {
                return (T)t;
            }
        }
    }
}
