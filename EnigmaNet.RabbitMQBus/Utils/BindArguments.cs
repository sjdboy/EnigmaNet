using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.RabbitMQBus.Utils
{
    class BindArguments
    {
        public const string XMatch = "x-match";
        public const string XMathAny = "any";
        public const string XMathAll = "all";
        //public const string MessageValue = "1";
        public const string MessageType = "message-type";
    }
}
