using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EnigmaNet.QCloud.Cls
{
    public static class ClsLoggerExtensions
    {
        const string QcloudClsOptionsKey = "QcloudClsOptions";
        const string LoggingOptionsKey = "Logging";
        static bool _hadReg = false;

        public static IServiceCollection AddClsLogging(this IServiceCollection service, IConfiguration configuration)
        {
            service.Configure<ClsOptions>(configuration.GetSection(QcloudClsOptionsKey));
            service.Configure<LoggingOptions>(configuration.GetSection(LoggingOptionsKey));

            service.AddSingleton<ClsWriter>();
            service.AddSingleton<ClsLoggerProvider>();

            return service;
        }

        public static IApplicationBuilder UseClsLogging(this IApplicationBuilder app)
        {
            if (_hadReg)
            {
                throw new Exception("Already AddClsLog");
            }

            _hadReg = true;

            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var clsWriter = app.ApplicationServices.GetRequiredService<ClsWriter>();
            var clsLoggerProvider = app.ApplicationServices.GetRequiredService<ClsLoggerProvider>();

            loggerFactory.AddProvider(clsLoggerProvider);

            lifetime.ApplicationStarted.Register(() =>
            {
                clsWriter.StartSyncTask();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                clsWriter.StopSyncTaskAndSendAllLog();
            });

            return app;
        }
    }
}
