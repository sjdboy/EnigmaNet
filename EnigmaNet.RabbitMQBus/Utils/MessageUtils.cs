using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using RabbitMQ.Client;

namespace EnigmaNet.RabbitMQBus.Utils
{
    class MessageUtils
    {
        class XDeath
        {
            public long Count { get; set; }
            public string Exchange { get; set; }
            public string Queue { get; set; }
            public string Reason { get; set; }
            public List<string> RoutingKeys { get; set; }
            public AmqpTimestamp Time { get; set; }

            public static List<XDeath> GetDeathData(IDictionary<string, object> headers)
            {
                if (headers == null)
                {
                    return null;
                }
                if (!headers.ContainsKey("x-death"))
                {
                    return null;
                }

                var datas = headers["x-death"] as System.Collections.IEnumerable;

                var list = new List<XDeath>();

                if (datas != null)
                {
                    foreach (IDictionary<string, object> data in datas)
                    {
                        var item = new XDeath
                        {
                            Count = (long)data["count"],
                            Exchange = Encoding.UTF8.GetString((byte[])data["exchange"]),
                            Queue = Encoding.UTF8.GetString((byte[])data["queue"]),
                            Reason = Encoding.UTF8.GetString((byte[])data["reason"]),
                            RoutingKeys = new List<string>(),
                            Time = (AmqpTimestamp)data["time"]
                        };

                        var routingKeys = data["routing-keys"] as System.Collections.IEnumerable;

                        foreach (byte[] key in routingKeys)
                        {
                            string keyValue;
                            if (key?.Length > 0)
                            {
                                keyValue = Encoding.UTF8.GetString(key);
                            }
                            else
                            {
                                keyValue = string.Empty;
                            }

                            item.RoutingKeys.Add(keyValue);
                        }

                        list.Add(item);
                    }
                }

                return list;
            }
        }

        public static long? GetDeathCount(BasicGetResult message, string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            var messageDeathDatas = XDeath.GetDeathData(message.BasicProperties?.Headers);

            var item = messageDeathDatas.Where(m => m.Queue == queueName).FirstOrDefault();
            if (item != null)
            {
                return item.Count;
            }
            else
            {
                return null;
            }
        }
    }
}
