using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 延迟消息发布器
    /// </summary>
    public interface IDelayMessagePublisher
    {
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        Task PublishAsync<T>(T message) where T : DelayMessage;
    }
}
