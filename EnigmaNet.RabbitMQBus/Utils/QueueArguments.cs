using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.RabbitMQBus.Utils
{
    class QueueArguments
    {
        public const string MessageTTL = "x-message-ttl";
        public const string DeadLetterExchange = "x-dead-letter-exchange";
        public const string DeadLetterRoutingKey = "x-dead-letter-routing-key";
    }
}
