using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<TResult> HandleAsync(TCommand command);
    }

    public interface ICommandTaskHandler<TCommand> : ICommandHandler<TCommand, Empty> where TCommand : ICommand<Empty>
    {
        // Task<Empty> ICommandHandler<TCommand, Empty>.HandleAsync(TCommand command)
        // {
        //     return HandleTaskAsync(command).ContinueWith(m =>
        //     {
        //         if (m.IsFaulted)
        //         {
        //             throw m.Exception;
        //         }
        //         return Empty.Value;
        //     });
        // }

        async Task<Empty> ICommandHandler<TCommand, Empty>.HandleAsync(TCommand command)
        {
            await HandleTaskAsync(command);
            return Empty.Value;
        }

        Task HandleTaskAsync(TCommand command);
    }
}
