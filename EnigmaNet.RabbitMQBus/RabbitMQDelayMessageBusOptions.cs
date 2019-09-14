using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.RabbitMQBus
{
    public class RabbitMQDelayMessageBusOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FailMessageStoreFolder { get; set; }
        public string CoreExchangeName { get; set; }
        public string InstanceId { get; set; }
    }
}
