using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
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

        static void Subscriber(IEventSubscriber eventSubscriber, object eventHandler)
        {
            var handlerTypes = eventHandler.GetType().GetInterfaces()
                       .Where(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(IEventHandler<>));

            if (handlerTypes != null && handlerTypes.Count() > 0)
            {
                var subscribeMethod = typeof(IEventSubscriber).GetMethod("SubscribeAsync");
                foreach (var handlerType in handlerTypes)
                {
                    var eventType = handlerType.GetGenericArguments();
                    //subscribeMethod.MakeGenericMethod(eventType).Invoke(eventSubscriber, new object[] { eventHandler });
                    var task = (Task)subscribeMethod.MakeGenericMethod(eventType).Invoke(eventSubscriber, new object[] { eventHandler });
                    task.Wait();
                }
            }
        }

        public static void Subscriber(IEventSubscriber eventSubscriber, params object[] eventHandlers)
        {
            foreach (var eventHanlder in eventHandlers)
            {
                Subscriber(eventSubscriber, eventHanlder);
            }
        }

        static void Subscriber(IDelayMessageSubscriber delayMessageSubscriber, object delayMessageHandler)
        {
            var handlerTypes = delayMessageHandler.GetType().GetInterfaces()
                       .Where(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(IDelayMessageHandler<>));

            if (handlerTypes != null && handlerTypes.Count() > 0)
            {
                var subscribeMethod = typeof(IDelayMessageSubscriber).GetMethod("SubscribeAsync");
                foreach (var handlerType in handlerTypes)
                {
                    var delayMessageType = handlerType.GetGenericArguments();
                    var task = (Task)subscribeMethod.MakeGenericMethod(delayMessageType).Invoke(delayMessageSubscriber, new object[] { delayMessageHandler });
                    task.Wait();
                }
            }
        }

        public static void Subscriber(IDelayMessageSubscriber delayMessageSubscriber, params object[] delayMessageHandlers)
        {
            foreach (var delayMessageHanlder in delayMessageHandlers)
            {
                Subscriber(delayMessageSubscriber, delayMessageHanlder);
            }
        }

        public static void Subscriber(ICommandSubscriber commandSubscriber, IEventSubscriber eventSubscriber, params object[] handlers)
        {
            foreach (var eventHanlder in handlers)
            {
                Subscriber(eventSubscriber, eventHanlder);
            }

            foreach (var commandHandler in handlers)
            {
                Subscriber(commandSubscriber, commandHandler);
            }
        }

        public static void Subscriber(ICommandSubscriber commandSubscriber, IEventSubscriber eventSubscriber, IDelayMessageSubscriber delayMessageSubscriber, params object[] handlers)
        {
            foreach (var eventHanlder in handlers)
            {
                Subscriber(eventSubscriber, eventHanlder);
            }

            foreach (var commandHandler in handlers)
            {
                Subscriber(commandSubscriber, commandHandler);
            }

            foreach (var handler in handlers)
            {
                Subscriber(delayMessageSubscriber, handler);
            }
        }

    }
}
