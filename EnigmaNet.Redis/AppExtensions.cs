using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnigmaNet.Redis
{
    public static class AppExtensions
    {
        const string RedisOptionsKey = "RedisOptions";

        public static IServiceCollection AddRedis(this IServiceCollection service)
        {
            var configuration = service.BuildServiceProvider().GetRequiredService<IConfiguration>();

            service.Configure<Options.RedisOptions>(configuration.GetSection(RedisOptionsKey));

            service.AddSingleton<IRedisFactory, Impl.RedisFactory>();

            return service;
        }
    }
}
