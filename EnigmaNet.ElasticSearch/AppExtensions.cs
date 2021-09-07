using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnigmaNet.ElasticSearch
{
    public static class AppExtensions
    {
        const string EsOptionsKey = "EsOptions";

        public static IServiceCollection AddElasticSearch(this IServiceCollection service, IConfiguration configuration)
        {
            service.Configure<Options.EsOptions>(configuration.GetSection(EsOptionsKey));

            service.AddSingleton<IEsClientFactory, Impl.EsClientFactory>();

            return service;
        }
    }
}
