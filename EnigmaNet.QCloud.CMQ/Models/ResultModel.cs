using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.QCloud.CMQ.Models
{
    public abstract class ResultModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string RequestId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 示例如：(10210)queue is already existed
        /// </remarks>
        public int? GetModuleCode()
        {
            if (string.IsNullOrEmpty(Message))
            {
                return null;
            }

            var rightTagIndex = Message.IndexOf(")");
            if (rightTagIndex > 0)
            {
                var moduleNumberText = Message.Substring(1, rightTagIndex - 1);

                return Convert.ToInt32(moduleNumberText);
            }
            else
            {
                return null;
            }
        }
    }
}
