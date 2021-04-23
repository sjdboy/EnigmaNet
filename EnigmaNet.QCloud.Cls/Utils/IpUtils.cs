using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.QCloud.Cls.Utils
{
    class IpUtils
    {
        public static List<string> GetIpv4s(string ipSegmentRequire = null)
        {
            var ipv4s = new List<string>();

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
                            ipv4s.Add(ipv4);
                        }
                    }
                    else
                    {
                        ipv4s.Add(ipv4);
                    }
                }
            }

            return ipv4s;
        }
    }
}
