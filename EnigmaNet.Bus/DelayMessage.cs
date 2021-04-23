using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 延迟消息
    /// </summary>
    public abstract class DelayMessage
    {
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// 消息标识
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// 延迟秒数
        /// </summary>
        public int DelaySeconds { get; set; }
    }
}
