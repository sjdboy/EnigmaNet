using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Exceptions
{
    /// <summary>
    /// 参数为空
    /// </summary>
    public class ArgumentNullOrEmptyException : ArgumentException
    {
        public ArgumentNullOrEmptyException()
            : base()
        { }

        public ArgumentNullOrEmptyException(string paramName)
            : base(string.Format("'{0}' must be not empty or null", paramName), paramName)
        { }

        public ArgumentNullOrEmptyException(string paramName, string message)
            : base(message, paramName)
        { }

        public ArgumentNullOrEmptyException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ArgumentNullOrEmptyException(string message, string paramName, Exception innerException)
            : base(message, paramName, innerException)
        { }
    }
}
