using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EnigmaNet.Utils
{
    public static class StringUtils
    {
        /// <summary>
        /// 重复字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static string RepeatString(string str, int n)
        {
            char[] arr = str.ToCharArray();

            char[] arrDest = new char[arr.Length * n];

            for (int i = 0; i < n; i++)
            {
                Buffer.BlockCopy(arr, 0, arrDest, i * arr.Length * 2, arr.Length * 2);
            }

            return new string(arrDest);
        }

        public static string Left(string text, int length)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (text.Length > length)
                {
                    return text.Substring(0, length);
                }
            }
            return text;
        }

        public static string Left(string text, int length, string overFlowStr)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (text.Length > length)
                {
                    return text.Substring(0, length) + overFlowStr;
                }
            }
            return text;
        }

        public static string Right(string text, int length)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (text.Length > length)
                {
                    return text.Substring(text.Length - length);
                }
            }
            return text;
        }

        public static string TrimEnd(string text, string trimStr)
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(trimStr) && text.Length >= trimStr.Length)
            {
                if (Right(text, trimStr.Length) == trimStr)
                {
                    return Left(text, text.Length - trimStr.Length);
                }
            }
            return text;
        }

        public static string TrimStart(string text, string trimStr)
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(trimStr) && text.Length >= trimStr.Length)
            {
                if (Left(text, trimStr.Length) == trimStr)
                {
                    return Right(text, text.Length - trimStr.Length);
                }
            }
            return text;
        }

        /// <summary>
        /// 根据字节数长度来取字符
        /// </summary>
        /// <param name="text"></param>
        /// <param name="length"></param>
        /// <param name="includeOverFlowChar">是否返回超出长度的那个字符</param>
        /// <param name="overFlowSuffix">超出长度要加的后缀</param>
        /// <returns></returns>
        public static string GetLeftByCharLenth(string text, int length, bool includeOverFlowChar = true, string overFlowSuffix = null)
        {
            if (string.IsNullOrEmpty(text) ||
                System.Text.Encoding.Unicode.GetByteCount(text) <= length
                )
            {
                return text;
            }

            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(text);


            if (bytes.Length <= length)
            {
                return text;
            }

            var total = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                total += System.Text.Encoding.Unicode.GetByteCount(new char[] { c });

                if (total > length)
                {
                    if (includeOverFlowChar)
                    {
                        return text.Substring(0, i + 1) + overFlowSuffix;
                    }
                    else
                    {
                        return text.Substring(0, i) + overFlowSuffix;
                    }
                }

            }

            return text;
        }

        public static string GetChineseNumber(int number)
        {
            string[] chineseNumber = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };

            string[] unit = { "", "十", "百", "千", "万", "十万", "百万", "千万", "亿", "十亿", "百亿", "千亿", "兆", "十兆", "百兆", "千兆" };
            StringBuilder ret = new StringBuilder();

            string inputNumber = number.ToString();

            int idx = inputNumber.Length;
            bool needAppendZero = false;
            foreach (char c in inputNumber)
            {
                idx--;
                if (c > '0')
                {
                    if (needAppendZero)
                    {
                        ret.Append(chineseNumber[0]);
                        needAppendZero = false;
                    }
                    ret.Append(chineseNumber[(int)(c - '0')] + unit[idx]);
                }
                else
                    needAppendZero = true;
            }
            return ret.Length == 0 ? chineseNumber[0] : ret.ToString();
        }

        /// <summary>
        /// 获取类的显示名称
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetClassDisplayName<T>()
        {
            //System.Attribute[] attrs = System.Attribute.GetCustomAttributes(typeof(T),typeof(DisplayNameAttribute));

            //if (attrs != null && attrs.Length > 0)
            //{
            //    return ((DisplayNameAttribute)attrs[0]).DisplayName;
            //}
            //else {
            //    return typeof(T).Name;
            //}

            return GetClassDisplayName(typeof(T));
        }

        /// <summary>
        /// 获取类的显示名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetClassDisplayName(Type classType)
        {
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(classType, typeof(DisplayNameAttribute));

            if (attrs != null && attrs.Length > 0)
            {
                return ((DisplayNameAttribute)attrs[0]).DisplayName;
            }
            else
            {
                return classType.Name;
            }
        }

        /// <summary>
        /// 获取遮盖的字符串
        /// </summary>
        /// <param name="text">内容</param>
        /// <param name="startShowLength">开始部分显示几个字</param>
        /// <param name="endShowLength">结束部分显示几个字</param>
        /// <returns></returns>
        public static string GetHiddenString(string text, int startShowLength, int endShowLength = 0)
        {
            if (startShowLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startShowLength));
            }
            if (endShowLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(endShowLength));
            }

            if (string.IsNullOrEmpty(text) || text.Length <= (startShowLength + endShowLength))
            {
                return text;
            }

            return text.Substring(0, startShowLength)
                + RepeatString("*", text.Length - startShowLength - endShowLength)
                + text.Substring(text.Length - endShowLength)
                ;
        }

        public static string IfEmptyBecome(string text, string emptyBecomeText)
        {
            if (string.IsNullOrEmpty(text))
            {
                return emptyBecomeText;
            }
            else
            {
                return text;
            }
        }

        public static string GetCamelCaseString(string name)
        {
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
