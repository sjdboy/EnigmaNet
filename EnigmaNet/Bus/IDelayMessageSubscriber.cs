using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 延迟消息订阅器
    /// </summary>
    public interface IDelayMessageSubscriber
    {
        /// <summary>
        /// 订阅消息处理器
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="handler">处理程序</param>
        Task SubscribeAsync<T>(IDelayMessageHandler<T> handler) where T : DelayMessage;
    }
}
