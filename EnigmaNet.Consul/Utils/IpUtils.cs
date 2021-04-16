using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Consul.Utils
{
    class IpUtils
    {
        public static string GetIp(string ipSegmentRequire=null)
        {
            var ips = NetworkInterface.GetAllNetworkInterfaces()
                .OrderByDescending(m => m.Speed)
                .Where(m => m.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    m.OperationalStatus == OperationalStatus.Up)
                .ToList();

            if (ips?.Count > 0)
            {
                foreach (var ip in ips)
                {
                    var property = ip.GetIPProperties();

                    var ipv4 = property.UnicastAddresses
                        .Where(m => m.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(m => m.Address)
                        .FirstOrDefault()?.ToString();

                    if (string.IsNullOrEmpty(ipv4))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(ipSegmentRequire))
                    {
                        if (ipv4.StartsWith(ipSegmentRequire))
                        {
                            return ipv4;
                        }
                    }
                    else
                    {
                        return ipv4;
                    }
                }
            }

            return null;
        }
    }
}
