using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus.Impl
{
    public class EventBusOptions
    {
        /// <summary>
        /// 队列处理线程数
        /// </summary>
        public int ThreadNumber { get; set; }

        /// <summary>
        /// 队列空闲等待时间（秒）
        /// </summary>
        public int EmptyWaitSecond { get; set; }
    }
}
