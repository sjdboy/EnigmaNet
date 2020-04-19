using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.BytedanceMicroApp
{
    public class BytedanceMicroAppException : Exception
    {
        public BytedanceMicroAppException(int errorCode, string message) : base(message)
        {
            this.ErrorCode = errorCode;
        }

        public int ErrorCode { get; set; }
    }
}
