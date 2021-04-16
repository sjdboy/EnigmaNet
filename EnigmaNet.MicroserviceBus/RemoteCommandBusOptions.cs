using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.MicroserviceBus
{
    public class RemoteCommandBusOptions
    {
        public List<string> GamewayUrls { get; set; }
        public string IdentityServerUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string HttpClientName { get; set; }
        /// <summary>
        /// 刷新距token过期剩余时间
        /// </summary>
        public int RefreshTokenRemainingSeconds { get; set; } = 60;

        /// <summary>
        /// 允许执行远程指令集
        /// </summary>
        /// <remarks>
        /// 取值例如：
        /// * 允许执行所有远程指令
        /// order.* 允许执行order.开头的远程指令
        /// user.reg 允许执行user.reg远程指令
        /// </remarks>
        public List<string> AllowRemoteCommands { get; set; }
        /// <summary>
        /// 禁止执行远程命令集（优先级高）
        /// </summary>
        /// <remarks>
        /// 取值例如：
        /// * 禁止执行所有远程指令
        /// order.* 禁止执行order.开头的远程指令
        /// user.reg 禁止执行user.reg远程指令
        /// </remarks>
        public List<string> ForbidRemoteCommands { get; set; }
    }
}
