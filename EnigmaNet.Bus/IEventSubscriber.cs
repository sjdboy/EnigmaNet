using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 事件订阅器
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        Task SubscribeAsync<T>(IEventHandler<T> handler) where T : Event;
    }
}
