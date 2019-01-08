using EnigmaNet.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举显示名称
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string GetDisplayName(this Enum e)
        {
            var enumType = e.GetType();

            if (!Enum.IsDefined(enumType, e))
            {
                return null;
            }

            var field = enumType.GetField(Convert.ToString(e));
            if (field == null)
            {
                return null;
            }
            return EnumUtils.GetEnumFieldDisplayName(field);
        }
    }
}
