using EnigmaNet.Bus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Extensions
{
    public static class BusExtensions
    {
        public static async Task<T> SendAndReturnAsync<T, TCommand>(this ICommandSender source, TCommand command, Func<TCommand, T> getResultFunc)
              where TCommand : Command
        {
            await source.SendAsync(command);
            return getResultFunc(command);
        }
    }
}
