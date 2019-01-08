using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNetCore.Bus.Impl
{
    public class EventBus2Options
    {
        public List<Type> UseMessageQueueEventTypes { get; set; }

        public List<string> SendMessageQueuePaths { get; set; }

        public List<string> ReceiveMessageQueuePaths { get; set; }
    }
}
