using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using EnigmaNet.Bus;

namespace EnigmaNet.MicroserviceBus
{
    public static class BusExtensions
    {
        const string RemoteCommandBusOptionsKey = "RemoteCommandBusOptions";

        public static IServiceCollection AddRemoteCommandBus(this IServiceCollection service, IConfiguration configuration)
        {
            service.Configure<RemoteCommandBusOptions>(configuration.GetSection(RemoteCommandBusOptionsKey));

            service.AddSingleton<RemoteCommandBus>();
            service.AddSingleton<ICommandExecuter>(provider => provider.GetService<RemoteCommandBus>());
            service.AddSingleton<ICommandSubscriber>(provider => provider.GetService<RemoteCommandBus>());

            return service;
        }

        public static IApplicationBuilder InitRemoteCommandBus(this IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

            lifetime.ApplicationStarted.Register(() =>
            {
                var remoteCommandBus = app.ApplicationServices.GetRequiredService<RemoteCommandBus>();
                remoteCommandBus.StartTokenCheckIfNot();
            });

            return app;
        }
    }
}
