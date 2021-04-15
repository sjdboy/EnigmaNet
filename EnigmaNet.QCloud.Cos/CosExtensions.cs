using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using EnigmaNet.QCloud.Cos.Options;

namespace EnigmaNet.QCloud.Cos
{
    public static class CosExtensions
    {
        public const string QCloudCosOptionsKey = "QCloudCosOptions";

        public static IServiceCollection AddQCloudCos(this IServiceCollection service)
        {
            var configuration = service.BuildServiceProvider().GetRequiredService<IConfiguration>();

            service.Configure<QCloudCosOptions>(configuration.GetSection(QCloudCosOptionsKey));

            service.AddSingleton<ICosClient, Impl.CosClient>();

            return service;
        }
    }
}
