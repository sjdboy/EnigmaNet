using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNetCore.RabbitMQBus
{
    public class RabbitMQEventBus2Options
    {
        public class HostConfigModel
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public string VirtualHost { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }

        public HostConfigModel PrivateHost { get; set; }
        public HostConfigModel PublicHost { get; set; }
        public string PublicEventPrefix { get; set; }
        public string PublicBusExchangeName { get; set; }
        public string PublicBusQueueName { get; set; }
        public string PrivateBusExchangeName { get; set; }

        public int EmptyWaitMilliSeconds { get; set; }
        public int ErrorWaitMilliSeconds { get; set; }
    }
}
