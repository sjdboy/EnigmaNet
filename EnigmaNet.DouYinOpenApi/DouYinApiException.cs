using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi
{
    public class DouYinApiException : Exception
    {
        public DouYinApiException(int errorCode, string message) : base(message)
        {
            this.ErrorCode = errorCode;
        }

        public int ErrorCode { get; set; }
    }
}
