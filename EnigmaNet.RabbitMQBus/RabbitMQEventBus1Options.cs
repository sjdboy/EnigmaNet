using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNetCore.RabbitMQBus
{
    public class RabbitMQEventBus1Options
    {
        public class ProductQueueSetting
        {
            public string QueueName { get; set; }
            public bool ForAllEvent { get; set; }
            public List<string> EventTypeStrings { get; set; }
        }

        public class ConsumerQueueSetting
        {
            public string QueueName { get; set; }
            public int HandlerAmount { get; set; }
        }

        public bool MQEnabled { get; set; }

        public string Host { get; set; }
        public int Port { get; set; }
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public int EmptyWaitMilliSeconds { get; set; }
        public int ErrorWaitMilliSeconds { get; set; }

        public List<ProductQueueSetting> ProductQueueSettings { get; set; }
        public List<ConsumerQueueSetting> ConsumerQueueSettings { get; set; }
    }
}
