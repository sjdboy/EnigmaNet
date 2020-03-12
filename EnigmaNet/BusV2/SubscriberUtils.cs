using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnigmaNet.BusV2
{
    public static class SubscriberUtils
    {
        static void Subscriber(ICommandSubscriber commandSubscriber, object commandHandler)
        {
            var handlerTypes = commandHandler.GetType().GetInterfaces()
                    .Where(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

            if (handlerTypes != null && handlerTypes.Count() > 0)
            {
                var subscribeMethod = typeof(ICommandSubscriber).GetMethod(nameof(ICommandSubscriber.SubscribeAsync));
                foreach (var handlerType in handlerTypes)
                {
                    var commandTypes = handlerType.GetGenericArguments();
                    subscribeMethod.MakeGenericMethod(commandTypes).Invoke(commandSubscriber, new object[] { commandHandler });
                }
            }
        }

        public static void Subscriber(ICommandSubscriber commandSubscriber, params object[] commandHandlers)
        {
            foreach (var commandHandler in commandHandlers)
            {
                Subscriber(commandSubscriber, commandHandler);
            }
        }
    }
}
