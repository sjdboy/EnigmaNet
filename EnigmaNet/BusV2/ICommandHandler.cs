using System.Threading.Tasks;

namespace EnigmaNet.BusV2
{
    public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<TResult> HandleAsync(TCommand command);
    }

    //public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Empty> where TCommand : ICommand<Empty>
    //{
    //}

    public interface ICommandTaskHandler<TCommand> : ICommandHandler<TCommand, Empty> where TCommand : ICommand<Empty>
    {
        Task<Empty> ICommandHandler<TCommand, Empty>.HandleAsync(TCommand command)
        {
            return HandleTaskAsync(command).ContinueWith(m =>
            {
                if (m.IsFaulted)
                {
                    throw m.Exception;
                }
                return Empty.Value;
            });
        }

        Task HandleTaskAsync(TCommand command);
    }
}
