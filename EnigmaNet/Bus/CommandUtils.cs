using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    public class CommandUtils
    {
        class CommandExecuterWrapper
        {
            public Task<object> ExecuteAsync<TResult>(ICommandExecuter commandExecuter, ICommand<TResult> command)
            {
                return commandExecuter.ExecuteAsync(command).ContinueWith(m =>
                {
                    if (m.IsFaulted)
                    {
                        throw m.Exception;
                    }

                    return (object)m.Result;
                });
            }
        }

        static CommandExecuterWrapper _commandExecuterWrapper = new CommandExecuterWrapper();

        public static Task<object> ExecuteAsync(ICommandExecuter commandExecuter, object command)
        {
            var commandType = command.GetType();

            var iCommandType = commandType.GetInterfaces()
                .FirstOrDefault(m => m.IsGenericType && m.GetGenericTypeDefinition() == typeof(ICommand<>));

            if (iCommandType == null)
            {
                throw new ArgumentException($"command'{commandType}' not impl ICommand<>");
            }

            var resultType = iCommandType.GetGenericArguments()[0];

            var method = typeof(CommandExecuterWrapper).GetMethod(nameof(CommandExecuterWrapper.ExecuteAsync))
                .MakeGenericMethod(resultType);

            var task = (Task<object>)method.Invoke(_commandExecuterWrapper, new[] { commandExecuter, command });

            return task;
        }
    }
}
