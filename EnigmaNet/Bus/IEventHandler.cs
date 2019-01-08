using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 事件处理器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEventHandler<T> where T : Event
    {
        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="event"></param>
        Task HandleAsync(T @event);
    }
}
