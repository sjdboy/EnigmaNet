using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    /// <summary>
    /// 命令处理器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("请使用BusV2")]
    public interface ICommandHandler<T> where T : Command
    {
        /// <summary>
        /// 处理命令 
        /// </summary>
        /// <param name="command">命令</param>
        Task HandleAsync(T command);
    }
}
