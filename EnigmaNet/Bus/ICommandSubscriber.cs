using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 命令订阅器
    /// </summary>
    [Obsolete("请使用BusV2")]
    public interface ICommandSubscriber
    {
        /// <summary>
        /// 订阅命令
        /// </summary>
        /// <typeparam name="T">命令类型</typeparam>
        /// <param name="handler">处理程序</param>
        void Subscribe<T>(ICommandHandler<T> handler) where T : Command;
    }
}
