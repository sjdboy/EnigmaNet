using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

using Consul;
using Winton.Extensions.Configuration.Consul;

namespace EnigmaNet.Consul
{
    public static class ConsulExtensions
    {
        const string ConsulOptionsKey = "ConsulOptions";

        public static IApplicationBuilder RegisterConsul(this IApplicationBuilder app, ConsulOptions options = null)
        {
            if (options == null)
            {
                var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
                options = configuration.GetSection(ConsulOptionsKey).Get<ConsulOptions>();
            }

            if (string.IsNullOrEmpty(options.Url))
            {
                throw new ArgumentNullException(nameof(options.Url));
            }

            var logger = app.ApplicationServices.GetService<ILoggerFactory>().CreateLogger(typeof(ConsulExtensions).FullName);

            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            var currentAssembly = Assembly.GetEntryAssembly();
            var serviceName = currentAssembly.GetName().Name;
            var serviceVersion = currentAssembly.GetName().Version?.ToString();
            var consulUrl = options.Url;

            logger.LogInformation($"consulUrl:{consulUrl}");

            var registration = new AgentServiceRegistration()
            {
                ID = Guid.NewGuid().ToString() + "_" + serviceName + ":" + serviceVersion,
                Name = serviceName,
            };

            lifetime.ApplicationStarted.Register(() =>
            {
                try
                {
                    var addressFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
                    logger.LogInformation($"addressFeature:{string.Join(",", addressFeature.Addresses)}");

                    string ip;
                    string scheme;
                    int port;
                    {
                        var address = new Uri(addressFeature.Addresses.FirstOrDefault());

                        port = address.Port;
                        scheme = address.Scheme;

                        ip = address.Host;
                        if (ip == "[::]")
                        {
                            ip = Utils.IpUtils.GetIp(options.IpSegment);
                        }
                    }

                    if (string.IsNullOrEmpty(ip))
                    {
                        throw new Exception("ip is null");
                    }

                    logger.LogInformation($"scheme:{scheme} ip:{ip} port:{port}");

                    var httpCheck = new AgentServiceCheck()
                    {
                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                        Interval = TimeSpan.FromSeconds(10),
                        HTTP = $"{scheme}://{ip}:{port}{options.CheckPath}",
                        Timeout = TimeSpan.FromSeconds(5),
                        TLSSkipVerify = false,
                    };

                    registration.Checks = new[] { httpCheck };
                    registration.Address = ip;
                    registration.Port = port;
                    registration.Meta = options.Meta ?? new Dictionary<string, string>();
                    registration.Tags = options.Tags?.ToArray();

                    registration.Meta.Add("service-ver", serviceVersion);

                    logger.LogInformation($"to register httpCheck url:{httpCheck.HTTP} address:{registration.Address} port:{registration.Port} id:{registration.ID} serverName:{registration.Name}");

                    var client = new ConsulClient(m =>
                    {
                        m.Address = new Uri(consulUrl);
                        m.Token = options.Token;
                    });

                    client.Agent.ServiceRegister(registration).Wait();
                }
                catch
                {
                    //注册出错=>退出软件
                    lifetime.StopApplication();
                }
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                var client = new ConsulClient(m =>
                {
                    m.Address = new Uri(consulUrl);
                    m.Token = options.Token;
                });

                logger.LogInformation($"to deregister service,id:{registration.ID} serverName:{registration.Name}");

                client.Agent.ServiceDeregister(registration.ID).Wait();
            });

            app.Map(options.CheckPath, app2 =>
            {
                app2.Run(context =>
                {
                    return context.Response.WriteAsync("ok");
                });
            });

            return app;
        }

        public static IHostBuilder ConfigureConsulKeyValue(this IHostBuilder source, string commonFolder = "Common", string appFolder = null)
        {
            return source.ConfigureAppConfiguration((host, config) =>
            {
                if (string.IsNullOrEmpty(appFolder))
                {
                    appFolder = host.HostingEnvironment.ApplicationName;
                }

                config
                .AddJsonFile("local-appsettings.json", false)
                .AddEnvironmentVariables()
                .AddConsul($"{commonFolder}/service-appsettings.json", x =>
                {
                    var configuration = config.Build();
                    var consulOptions = new ConsulOptions();
                    configuration.Bind(ConsulOptionsKey, consulOptions);

                    Console.WriteLine("consulOptions.Url:" + consulOptions.Url);

                    if (string.IsNullOrEmpty(consulOptions.Url))
                    {
                        throw new ArgumentNullException(nameof(consulOptions.Url));
                    }

                    x.ConsulConfigurationOptions = o =>
                    {
                        o.Address = new Uri(consulOptions.Url);
                        o.Token = consulOptions.Token;
                    };
                    x.Optional = false;
                    x.ReloadOnChange = true;
                    x.PollWaitTime = TimeSpan.FromSeconds(5);
                })
                .AddConsul($"{appFolder}/remote-appsettings.json", x =>
                {
                    var configuration = config.Build();
                    var consulOptions = new ConsulOptions();
                    configuration.Bind(ConsulOptionsKey, consulOptions);

                    Console.WriteLine("consulOptions.Url:" + consulOptions.Url);

                    if (string.IsNullOrEmpty(consulOptions.Url))
                    {
                        throw new ArgumentNullException(nameof(consulOptions.Url));
                    }

                    x.ConsulConfigurationOptions = o =>
                    {
                        o.Address = new Uri(consulOptions.Url);
                        o.Token = consulOptions.Token;
                    };
                    x.Optional = false;
                    x.ReloadOnChange = true;
                    x.PollWaitTime = TimeSpan.FromSeconds(5);
                });
            });
        }

        public static IHostApplicationBuilder ConfigureConsulKeyValue(this IHostApplicationBuilder source, string commonFolder = "Common", string appFolder = null)
        {
            if (string.IsNullOrEmpty(appFolder))
            {
                appFolder=source.Environment.ApplicationName;
            }

            source.Configuration.AddJsonFile("local-appsettings.json", false)
                .AddEnvironmentVariables()
                .AddConsul($"{commonFolder}/service-appsettings.json", x =>
                {
                    var configuration = source.Configuration.Build();
                    var consulOptions = new ConsulOptions();
                    configuration.Bind(ConsulOptionsKey, consulOptions);

                    Console.WriteLine("consulOptions.Url:" + consulOptions.Url);

                    if (string.IsNullOrEmpty(consulOptions.Url))
                    {
                        throw new ArgumentNullException(nameof(consulOptions.Url));
                    }

                    x.ConsulConfigurationOptions = o =>
                    {
                        o.Address = new Uri(consulOptions.Url);
                        o.Token = consulOptions.Token;
                    };
                    x.Optional = false;
                    x.ReloadOnChange = true;
                    x.PollWaitTime = TimeSpan.FromSeconds(5);
                })
                .AddConsul($"{appFolder}/remote-appsettings.json", x =>
                {
                    var configuration = source.Configuration.Build();
                    var consulOptions = new ConsulOptions();
                    configuration.Bind(ConsulOptionsKey, consulOptions);

                    Console.WriteLine("consulOptions.Url:" + consulOptions.Url);

                    if (string.IsNullOrEmpty(consulOptions.Url))
                    {
                        throw new ArgumentNullException(nameof(consulOptions.Url));
                    }

                    x.ConsulConfigurationOptions = o =>
                    {
                        o.Address = new Uri(consulOptions.Url);
                        o.Token = consulOptions.Token;
                    };
                    x.Optional = false;
                    x.ReloadOnChange = true;
                    x.PollWaitTime = TimeSpan.FromSeconds(5);
                });

            return source;
        }
    }
}
