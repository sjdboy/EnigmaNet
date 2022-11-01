using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi
{
    public class FeiShuApiException : Exception
    {
        public FeiShuApiException(string errorCode, string message) : base(message)
        {
            this.ErrorCode = errorCode;
        }

        public string ErrorCode { get; set; }
    }
}
