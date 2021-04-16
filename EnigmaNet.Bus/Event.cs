using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 事件基类
    /// </summary>
    [Serializable]
    public abstract class Event
    {
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime DateTime { get; set; } 

        /// <summary>
        /// 事件标识
        /// </summary>
        public string EventId { get; set; }
    }
}
