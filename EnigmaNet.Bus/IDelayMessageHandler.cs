using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 延迟消息处理器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDelayMessageHandler<T> where T : DelayMessage
    {
        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <returns></returns>
        Task HandleAsync(T message);
    }
}
