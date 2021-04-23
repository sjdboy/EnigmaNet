using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 事件发布器
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        Task PublishAsync<T>(T @event) where T : Event;
    }
}
