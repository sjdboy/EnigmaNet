using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 命令发布器
    /// </summary>
    [Obsolete("请使用BusV2")]
    public interface ICommandSender
    {
        /// <summary>
        /// 发送命令
        /// </summary>
        /// <typeparam name="T">命令类型</typeparam>
        /// <param name="command">命令</param>
        Task SendAsync<T>(T command) where T : Command;

        ///// <summary>
        ///// 发送命令
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="command"></param>
        //void Send<T>(T command) where T : Command;
    }
}
