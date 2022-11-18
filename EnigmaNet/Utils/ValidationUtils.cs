using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EnigmaNet.Utils
{
    public static class ValidationUtils
    {
        class RegexUtil
        {

            /// <summary>
            /// 简体汉字
            /// </summary>
            public const string PATTERN_CHINESE_SIMPLIFIED = @"^[\u4e00-\u9fa5]+$";

            /// <summary>
            /// 所有汉字
            /// </summary>
            public const string PATTERN_CHINESE_ALL = @"^[\u4e00-\u9fff]+$";

            /// <summary>
            /// 固话格式， 匹配3位或4位区号的电话号码，其中区号可以用小括号括起来，也可以不用，区号与本地号间可以用连字号或空格间隔，也可以没有间隔
            /// </summary>
            public const string PATTERN_TELE_PHONE = "^\\(0\\d{2}\\)[- ]?\\d{8}$|^0\\d{2}[- ]?\\d{8}$|^\\(0\\d{3}\\)[- ]?\\d{7}$|^0\\d{3}[- ]?\\d{7}$";

            /// <summary>
            /// 手机号格式
            /// </summary>
            //public const string PATTERN_MOBILE_PHONE = "^13\\d{9}$";
            public const string PATTERN_MOBILE_PHONE = "^(13[0-9]|14[0-9]|15[0-9]|16[0-9]|17[0-9]|18[0-9]|19[0-9])\\d{8}$";

            /// <summary>
            /// 邮箱格式
            /// </summary>
            public const string PATTERN_EMAIL = @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";

            /// <summary>
            /// MD5格式
            /// </summary>
            public const string PATTERN_MD5 = "^([a-fA-F0-9]{32})$";

            public const string PATTERN_IP = @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$";

            /// <summary>
            /// 代号(由字母数字下划线组成)
            /// </summary>
            public const string PATTERN_CODE = "^(([0-9a-zA-Z_]*[a-zA-Z]+)|([a-zA-Z]+[0-9a-zA-Z_]*)|([0-9a-zA-Z_]*[a-zA-Z]+[0-9a-zA-Z_]*))$";

            public const string PATTERN_LETTER = "^[a-zA-Z]+$";

            /// <summary>
            /// 数字
            /// </summary>
            public const string PATTERN_NUMBER = @"^\d+$";

            /// <summary>
            /// 身份证号
            /// </summary>
            public const string PARRERN_ID_CARD = @"^[1-9]\d{5}\d{2}((0[1-9])|(10|11|12))(([0-2][1-9])|10|20|30|31)\d{3}$";

            /// <summary>
            /// 银行卡号
            /// </summary>
            public const string BankNumberPattern = @"^([1-9]{1})(\d{14}|\d{18})$";

            /// <summary>
            /// 验证输入是否匹配正则模式
            /// </summary>
            /// <param name="pattern"></param>
            /// <param name="input"></param>
            /// <returns></returns>
            public static bool IsMatch(string pattern, string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return false;
                }
                var regex = new Regex(pattern);
                return regex.IsMatch(input);
            }

        }

        /// <summary>
        /// 是否是代号
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsCode(string str)
        {
            return RegexUtil.IsMatch(RegexUtil.PATTERN_CODE, str);
        }

        public static bool IsChinese(string str)
        {
            return RegexUtil.IsMatch(RegexUtil.PATTERN_CHINESE_ALL, str);
        }

        public static bool IsChineseSimplified(string str)
        {
            return RegexUtil.IsMatch(RegexUtil.PATTERN_CHINESE_SIMPLIFIED, str);
        }

        public static bool IsLetter(string str)
        {
            return RegexUtil.IsMatch(RegexUtil.PATTERN_LETTER, str);
        }

        public static bool IsEmail(string str)
        {
            return RegexUtil.IsMatch(RegexUtil.PATTERN_EMAIL, str);
        }

        public static bool IsMobile(string str)
        {
            return RegexUtil.IsMatch(RegexUtil.PATTERN_MOBILE_PHONE, str);
        }

        public static bool IsBankNumber(string str)
        {
            return RegexUtil.IsMatch(RegexUtil.BankNumberPattern, str);
        }

        /// <summary>
        /// 是否是数字
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNumber(string str)
        {
            return RegexUtil.IsMatch(RegexUtil.PATTERN_NUMBER, str);
        }

        /// <summary>
        /// 是否是身份证号
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsIdCard(string str)
        {
            return RegexUtil.IsMatch(RegexUtil.PARRERN_ID_CARD, str);
        }
    }
}
