using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using TencentCloud.Common;
using TencentCloud.Clb.V20180317;
using TencentCloud.Clb.V20180317.Models;

namespace EnigmaNet.QCloud.Clb
{
    public static class ClbExtensions
    {
        class TargetInfo
        {
            public string EniIp { get; set; }
            public int Port { get; set; }
        }

        const string QcloudClbOptionsKey = "QcloudClbOptions";

        public static IApplicationBuilder RegisterQcloudClb(this IApplicationBuilder app, ClbOptions options = null)
        {
            if (options == null)
            {
                var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
                options = configuration.GetSection(QcloudClbOptionsKey).Get<ClbOptions>();
            }

            if (!(options?.Locations?.Count > 0))
            {
                return app;
            }

            var logger = app.ApplicationServices.GetService<ILoggerFactory>().CreateLogger(typeof(ClbExtensions).FullName);

            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

            var cred = new Credential
            {
                SecretId = options.SecretId,
                SecretKey = options.SecretKey,
            };

            TargetInfo targetInfo = null;

            lifetime.ApplicationStarted.Register(() =>
            {
                try
                {
                    {
                        string ip;
                        int port;
                        {
                            var addressFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
                            var address = new Uri(addressFeature.Addresses.FirstOrDefault());
                            port = address.Port;
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

                        logger.LogInformation($"server info,ip:{ip} port:{port}");

                        targetInfo = new TargetInfo
                        {
                            EniIp = ip,
                            Port = port,
                        };
                    }

                    {
                        var client = new ClbClient(cred, options.Region);

                        foreach (var group in options.Locations.GroupBy(m => m.LoadBalancerId))
                        {
                            var loadBalancerId = group.Key;

                            var tryIndex = 0;
                            while (true)
                            {
                                tryIndex++;

                                logger.LogInformation($"RegisterTargets,tryIndex:{tryIndex},{Newtonsoft.Json.JsonConvert.SerializeObject(group)}");

                                try
                                {
                                    client.BatchRegisterTargetsSync(new BatchRegisterTargetsRequest
                                    {
                                        LoadBalancerId = loadBalancerId,
                                        Targets = group.Select(m => new BatchTarget
                                        {
                                            ListenerId = m.ListenerId,
                                            LocationId = m.LocationId,
                                            Port = targetInfo.Port,
                                            EniIp = targetInfo.EniIp,
                                            Weight = 10,
                                        }).ToArray(),
                                    });

                                    logger.LogInformation($"RegisterTargets finish,tryIndex:{tryIndex},{Newtonsoft.Json.JsonConvert.SerializeObject(group)}");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, $"RegisterTargets error,tryIndex:{tryIndex}, maxTryCount:{options.MaxTryCount}, {Newtonsoft.Json.JsonConvert.SerializeObject(group)}");

                                    if (tryIndex >= options.MaxTryCount)
                                    {
                                        throw;
                                    }
                                    else
                                    {
                                        Thread.Sleep(500);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, $"reg error and stop app");

                    //注册出错=>退出应用
                    lifetime.StopApplication();
                }
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                if (targetInfo == null)
                {
                    return;
                }

                var client = new ClbClient(cred, options.Region);

                foreach (var group in options.Locations.GroupBy(m => m.LoadBalancerId))
                {
                    var loadBalancerId = group.Key;

                    var tryIndex = 0;
                    while (true)
                    {
                        tryIndex++;

                        try
                        {
                            logger.LogInformation($"BatchDeregisterTargets,tryIndex:{tryIndex},{Newtonsoft.Json.JsonConvert.SerializeObject(group)}");

                            client.BatchDeregisterTargetsSync(new BatchDeregisterTargetsRequest
                            {
                                LoadBalancerId = loadBalancerId,
                                Targets = group.Select(m => new BatchTarget
                                {
                                    ListenerId = m.ListenerId,
                                    LocationId = m.LocationId,
                                    Port = targetInfo.Port,
                                    EniIp = targetInfo.EniIp,
                                    Weight = 10,
                                }).ToArray(),
                            });

                            logger.LogInformation($"BatchDeregisterTargets finish,tryIndex:{tryIndex},{Newtonsoft.Json.JsonConvert.SerializeObject(group)}");

                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"BatchDeregisterTargets error,tryIndex:{tryIndex}, maxTryCount:{options.MaxTryCount}, {Newtonsoft.Json.JsonConvert.SerializeObject(group)}");

                            if (tryIndex >= options.MaxTryCount)
                            {
                                throw;
                            }
                            else
                            {
                                Thread.Sleep(500);
                            }
                        }
                    }
                }
            });

            return app;
        }
    }
}
