using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.BusV2.Impl
{
    public class CommandBusOptions
    {
        /// <summary>
        /// 远程指令处理器地址映射
        /// </summary>
        /// <remarks>
        /// key:指令（指令类路径）的匹配前缀
        /// value:指令处理器地址
        /// </remarks>
        public Dictionary<string, List<string>> RemoteCommandHandlerAddresses { get; set; }
    }
}
