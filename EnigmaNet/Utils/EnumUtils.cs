using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace EnigmaNet.Utils
{
    public static class EnumUtils
    {
        internal static string GetEnumFieldDisplayName(FieldInfo field)
        {
            var objs = field.GetCustomAttributes(typeof(DisplayAttribute), false);
            if (objs?.Length > 0)
            {
                return ((DisplayAttribute)objs[0]).Name;
            }

            var objs2 = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (objs2?.Length > 0)
            {
                return ((DescriptionAttribute)objs2[0]).Description;
            }

            var objs3 = field.GetCustomAttributes(typeof(DisplayNameAttribute), false);
            if (objs3?.Length > 0)
            {
                return ((DisplayNameAttribute)objs3[0]).DisplayName;
            }

            return field.Name;
        }

        /// <summary>
        /// 字符串转换成枚举
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="name"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static TEnum Parse<TEnum>(string name, bool ignoreCase = true) where TEnum : struct
        {
            return (TEnum)Enum.Parse(typeof(TEnum), name, ignoreCase);
        }

        /// <summary>
        /// 整数转换成枚举
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TEnum Parse<TEnum>(int value) where TEnum : struct
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value.ToString());
        }

        /// <summary>
        /// 尝试将字符串转换成枚举
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool TryParse<TEnum>(string name, out TEnum obj, bool ignoreCase = true) where TEnum : struct
        {
            return Enum.TryParse<TEnum>(name, ignoreCase, out obj);
        }

        /// <summary>
        /// 尝试将整数转换成枚举
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool TryParse<TEnum>(int value, out TEnum obj) where TEnum : struct
        {
            return Enum.TryParse<TEnum>(value.ToString(), out obj);
        }

        /// <summary>
        /// 返回枚举名称和显示名称列表
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static Dictionary<string, string> GetNameDisplayList<TEnum>() where TEnum : struct
        {
            var enumType = typeof(TEnum);

            var dic = new Dictionary<string, string>();
            foreach (string name in Enum.GetNames(enumType))
            {
                var em = (TEnum)Enum.Parse(enumType, name);

                dic.Add(Convert.ToString(em), GetEnumFieldDisplayName(enumType.GetField(name)));
            }
            return dic;
        }

        /// <summary>
        /// 反回枚举列表
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static IEnumerable<TEnum> GetEnumList<TEnum>() where TEnum : struct
        {
            var enumType = typeof(TEnum);

            var enumList = new List<TEnum>();

            foreach (string name in Enum.GetNames(enumType))
            {
                var em = (TEnum)Enum.Parse(enumType, name);

                enumList.Add(em);
            }
            return enumList;
        }

        /// <summary>
        /// 返回枚举值和显示名称列表
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static Dictionary<int, string> GetValueDisplayList<TEnum>() where TEnum : struct
        {
            var enumType = typeof(TEnum);

            var dic = new Dictionary<int, string>();
            foreach (string name in Enum.GetNames(enumType))
            {
                var em = (TEnum)Enum.Parse(enumType, name);

                dic.Add(Convert.ToInt32(em), GetEnumFieldDisplayName(enumType.GetField(name)));
            }
            return dic;
        }

        /// <summary>
        /// 返回枚举显示名称
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string GetDisplay<TEnum>(TEnum e) where TEnum : struct
        {
            var enumType = typeof(TEnum);

            if (!Enum.IsDefined(enumType, e))
            {
                return null;
            }

            var field = enumType.GetField(Convert.ToString(e));
            if (field == null)
            {
                return null;
            }
            return GetEnumFieldDisplayName(field);
        }

        /// <summary>
        /// 返回枚举值显示名称
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string GetDisplay<TEnum>(int enumValue) where TEnum : struct
        {
            var e = Parse<TEnum>(enumValue.ToString());

            return GetDisplay(e);
        }

        /// <summary>
        /// 返回枚举名显示名称
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumName"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static string GetDisplay<TEnum>(string enumName, bool ignoreCase = true) where TEnum : struct
        {
            var e = Parse<TEnum>(enumName, ignoreCase);

            return GetDisplay(e);
        }

        /// <summary>
        /// 枚举中是否存在该定义
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="obj">枚举或值或名称</param>
        /// <returns></returns>
        public static bool IsDefine<TEnum>(object obj) where TEnum : struct
        {
            return Enum.IsDefined(typeof(TEnum), obj);
        }
    }
}
