using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using EnigmaNet.Bus;

namespace EnigmaNet.RabbitMQBus
{
    public static class BusExtensions
    {
        const string RabbitMQEventBusOptionsKey = "RabbitMQEventBusOptions";
        const string RabbitMQDelayMessageBusOptionsKey = "RabbitMQDelayMessageBusOptions";

        public static IServiceCollection AddRabbitMQEventBus(this IServiceCollection service, IConfiguration configuration)
        {
            service.Configure<RabbitMQEventBusOptions>(configuration.GetSection(RabbitMQEventBusOptionsKey));

            service.AddSingleton<RabbitMQEventBus>();
            service.AddSingleton<IEventPublisher>(provider => provider.GetService<RabbitMQEventBus>());
            service.AddSingleton<IEventSubscriber>(provider => provider.GetService<RabbitMQEventBus>());

            return service;
        }

        public static IServiceCollection AddRabbitMQEventBusWithBufferPublisher(this IServiceCollection service, IConfiguration configuration)
        {
            service.Configure<RabbitMQEventBusOptions>(configuration.GetSection(RabbitMQEventBusOptionsKey));

            service.AddSingleton<RabbitMQEventBus>();
            service.AddSingleton<EventBufferPublisher>();
            service.AddSingleton<IEventPublisher>(provider => provider.GetService<EventBufferPublisher>());
            service.AddSingleton<IEventSubscriber>(provider => provider.GetService<RabbitMQEventBus>());

            return service;
        }

        public static IApplicationBuilder InitRabbitMQEventBus(this IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

            lifetime.ApplicationStarted.Register(() =>
            {
                var rabbitMQEventBus = app.ApplicationServices.GetRequiredService<RabbitMQEventBus>();
                rabbitMQEventBus.Init();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                var rabbitMQEventBus = app.ApplicationServices.GetRequiredService<RabbitMQEventBus>();
                rabbitMQEventBus.StopEventHandlers();

                var eventBufferPublisher = app.ApplicationServices.GetService<EventBufferPublisher>();
                if (eventBufferPublisher != null)
                {
                    eventBufferPublisher.SendAllEvents();
                }
            });

            return app;
        }

        public static IServiceCollection AddRabbitMQDelayMessageBus(this IServiceCollection service, IConfiguration configuration)
        {
            service.Configure<RabbitMQDelayMessageBusOptions>(configuration.GetSection(RabbitMQDelayMessageBusOptionsKey));

            service.AddSingleton<RabbitMQDelayMessageBus>();
            service.AddSingleton<IDelayMessagePublisher>(provider => provider.GetService<RabbitMQDelayMessageBus>());
            service.AddSingleton<IDelayMessageSubscriber>(provider => provider.GetService<RabbitMQDelayMessageBus>());

            return service;
        }

        public static IApplicationBuilder InitRabbitMQDelayMessageBus(this IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

            lifetime.ApplicationStarted.Register(() =>
            {
                var rabbitMQDelayMessageBus = app.ApplicationServices.GetRequiredService<RabbitMQDelayMessageBus>();
                rabbitMQDelayMessageBus.Init();
            });

            return app;
        }
    }
}
