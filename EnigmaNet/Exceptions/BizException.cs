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
        public BizException()
        {
        }

        public BizException(string message)
            : base(message)
        {
        }

        public BizException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
