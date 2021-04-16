using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnigmaNet.Bus
{
    public static class BusExtensions
    {
        public static IServiceCollection AddMemoryCommandBus(this IServiceCollection service)
        {
            service.AddSingleton<Impl.CommandBus>();
            service.AddSingleton<ICommandExecuter>(provider => provider.GetService<Impl.CommandBus>());
            service.AddSingleton<ICommandSubscriber>(provider => provider.GetService<Impl.CommandBus>());

            return service;
        }

        public static IServiceCollection AddMemoryEventBus(this IServiceCollection service)
        {
            service.AddSingleton<Impl.EventBus>();
            service.AddSingleton<IEventPublisher>(provider => provider.GetService<Impl.EventBus>());
            service.AddSingleton<IEventSubscriber>(provider => provider.GetService<Impl.EventBus>());

            return service;
        }

        public static IApplicationBuilder SubscriberCommandHandlers(this IApplicationBuilder app)
        {
            var logger = app.ApplicationServices.GetService<ILoggerFactory>().CreateLogger(typeof(BusExtensions).FullName);

            var commandSubscriber = app.ApplicationServices.GetRequiredService<ICommandSubscriber>();

            var subscribeMethod = typeof(ICommandSubscriber).GetMethod(nameof(ICommandSubscriber.SubscribeAsync));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetInterfaces()?.Any(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)) == true)
                    {
                        logger.LogInformation($"find command handler,type:{type}");

                        var handler = app.ApplicationServices.GetService(type);
                        if (handler == null)
                        {
                            continue;
                        }

                        logger.LogInformation($"handler is reg service,handler:{handler.GetType()}");

                        var handlerTypes = handler.GetType().GetInterfaces()
                            .Where(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
                        foreach (var handlerType in handlerTypes)
                        {
                            var commandTypes = handlerType.GetGenericArguments();
                            subscribeMethod.MakeGenericMethod(commandTypes).Invoke(commandSubscriber, new object[] { handler });

                            logger.LogInformation($"subscribe,commandHandler:{handler.GetType()} commandHandlerType:{handlerType}");
                        }
                    }
                }
            }

            return app;
        }

        public static IApplicationBuilder SubscriberEventHandlers(this IApplicationBuilder app)
        {
            var logger = app.ApplicationServices.GetService<ILoggerFactory>().CreateLogger(typeof(BusExtensions).FullName);

            var eventSubscriber = app.ApplicationServices.GetRequiredService<IEventSubscriber>();

            var subscribeMethod = typeof(IEventSubscriber).GetMethod(nameof(IEventSubscriber.SubscribeAsync));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetInterfaces()?.Any(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(IEventHandler<>)) == true)
                    {
                        logger.LogInformation($"find event handler,type:{type}");

                        var handler = app.ApplicationServices.GetService(type);
                        if (handler == null)
                        {
                            continue;
                        }

                        logger.LogInformation($"handler is reg service,handler:{handler.GetType()}");

                        var handlerTypes = handler.GetType().GetInterfaces()
                            .Where(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(IEventHandler<>));
                        foreach (var handlerType in handlerTypes)
                        {
                            var eventTypes = handlerType.GetGenericArguments();
                            subscribeMethod.MakeGenericMethod(eventTypes).Invoke(eventSubscriber, new object[] { handler });

                            //var task = (Task)subscribeMethod.MakeGenericMethod(eventTypes).Invoke(eventSubscriber, new object[] { handler });
                            //task.Wait();

                            logger.LogInformation($"subscribe,eventHandler:{handler.GetType()} eventHandlerType:{handlerType}");
                        }
                    }
                }
            }

            return app;
        }

        public static IApplicationBuilder SubscriberDelayMessageHandlers(this IApplicationBuilder app)
        {
            var logger = app.ApplicationServices.GetService<ILoggerFactory>().CreateLogger(typeof(BusExtensions).FullName);

            var delayMessageSubscriber = app.ApplicationServices.GetRequiredService<IDelayMessageSubscriber>();

            var subscribeMethod = typeof(IDelayMessageSubscriber).GetMethod(nameof(IDelayMessageSubscriber.SubscribeAsync));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetInterfaces()?.Any(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(IDelayMessageHandler<>)) == true)
                    {
                        logger.LogInformation($"find delay message handler,type:{type}");

                        var handler = app.ApplicationServices.GetService(type);
                        if (handler == null)
                        {
                            continue;
                        }

                        logger.LogInformation($"handler is reg service,handler:{handler.GetType()}");

                        var handlerTypes = handler.GetType().GetInterfaces()
                            .Where(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(IDelayMessageHandler<>));
                        foreach (var handlerType in handlerTypes)
                        {
                            var delayMessageTypes = handlerType.GetGenericArguments();
                            subscribeMethod.MakeGenericMethod(delayMessageTypes).Invoke(delayMessageSubscriber, new object[] { handler });

                            logger.LogInformation($"subscribe,delayMessageHandler:{handler.GetType()} delayMessageHandlerType:{handlerType}");
                        }
                    }
                }
            }

            return app;
        }
    }
}
