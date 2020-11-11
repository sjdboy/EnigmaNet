using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Exceptions
{
    /// <summary>
    /// 业务异常
    /// </summary>
    public class BizException : Exception
    {
        public string ErrorCode { get; set; }

        public BizException()
        {
        }

        public BizException(string message,string errorCode=null)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public BizException(string message, Exception innerException, string errorCode = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
