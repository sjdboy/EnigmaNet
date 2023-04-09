using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnigmaNet.BigCache.Cos.Extensions
{
    public static class AppExtensions
    {
        const string CosBigCacheOptionsKey = "CosBigCacheOptions";

        public static IServiceCollection AddCosBigCache(this IServiceCollection service, IConfiguration configuration)
        {
            service.Configure<Options.CosBigCacheOptions>(configuration.GetSection(CosBigCacheOptionsKey));

            service.AddSingleton<EnigmaNet.BigCache.IBigCache, EnigmaNet.BigCache.Cos.CosBigCache>();

            return service;
        }

    }
}
