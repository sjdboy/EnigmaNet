using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using TencentCloud.Common;
using TencentCloud.Cvm.V20170312;
using TencentCloud.Cvm.V20170312.Models;
using TencentCloud.Clb.V20180317;
using TencentCloud.Clb.V20180317.Models;

namespace EnigmaNet.QCloud.Clb
{
    public static class ClbExtensions
    {
        class TargetInfo
        {
            public string InstanceId { get; set; }
            public int Port { get; set; }
        }

        const string QcloudClbOptionsKey = "QcloudClbOptions";

        static string GetIp()
        {
            var ip = NetworkInterface.GetAllNetworkInterfaces()
                .OrderByDescending(m => m.Speed)
                .Where(m => m.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    m.OperationalStatus == OperationalStatus.Up)
                .First();

            if (ip != null)
            {
                var property = ip.GetIPProperties();

                var ipv4 = property.UnicastAddresses
                    .Where(m => m.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(m => m.Address)
                    .FirstOrDefault();

                return ipv4.ToString();
            }
            else
            {
                return null;
            }
        }

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
                                ip = GetIp();
                            }
                        }

                        if (string.IsNullOrEmpty(ip))
                        {
                            throw new Exception("ip is null");
                        }

                        logger.LogInformation($"server info,ip:{ip} port:{port}");

                        string cvmInstanceId;
                        {
                            var requestFilsters = new List<TencentCloud.Cvm.V20170312.Models.Filter>();
                            requestFilsters.Add(new TencentCloud.Cvm.V20170312.Models.Filter
                            {
                                Name = "private-ip-address",
                                Values = new string[] { ip }
                            });

                            var cvmClient = new CvmClient(cred, options.Region);

                            var response = cvmClient.DescribeInstancesSync(
                                new DescribeInstancesRequest
                                {
                                    Filters = requestFilsters.ToArray(),
                                });

                            cvmInstanceId = response.InstanceSet?
                              .FirstOrDefault()?.InstanceId;

                            logger.LogInformation($"cvm info:{cvmInstanceId}");
                        }

                        if (string.IsNullOrEmpty(cvmInstanceId))
                        {
                            throw new Exception($"cvmInstanceId is null,stop app,ip:{ip} port:{port}");
                        }

                        targetInfo = new TargetInfo
                        {
                            InstanceId = cvmInstanceId,
                            Port = port,
                        };
                    }

                    {
                        var client = new ClbClient(cred, options.Region);

                        foreach (var group in options.Locations.GroupBy(m => m.LoadBalancerId))
                        {
                            var loadBalancerId = group.Key;

                            logger.LogInformation($"RegisterTargets,{Newtonsoft.Json.JsonConvert.SerializeObject(group)}");

                            client.BatchRegisterTargetsSync(new BatchRegisterTargetsRequest
                            {
                                LoadBalancerId = loadBalancerId,
                                Targets = group.Select(m => new BatchTarget
                                {
                                    ListenerId = m.ListenerId,
                                    LocationId = m.LocationId,
                                    Port = targetInfo.Port,
                                    InstanceId = targetInfo.InstanceId,
                                    Weight = 10,
                                }).ToArray(),
                            });

                            logger.LogInformation($"RegisterTargets finish,{Newtonsoft.Json.JsonConvert.SerializeObject(group)}");
                        }
                    }
                }
                catch(Exception ex)
                {
                    logger.LogCritical(ex,$"reg error and stop app");

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

                    logger.LogInformation($"BatchDeregisterTargets,{Newtonsoft.Json.JsonConvert.SerializeObject(group)}");

                    client.BatchDeregisterTargetsSync(new BatchDeregisterTargetsRequest
                    {
                        LoadBalancerId = loadBalancerId,
                        Targets = group.Select(m => new BatchTarget
                        {
                            ListenerId = m.ListenerId,
                            LocationId = m.LocationId,
                            Port = targetInfo.Port,
                            InstanceId = targetInfo.InstanceId,
                            Weight = 10,
                        }).ToArray(),
                    });

                    logger.LogInformation($"BatchDeregisterTargets finish,{Newtonsoft.Json.JsonConvert.SerializeObject(group)}");
                }
            });

            return app;
        }
    }
}
