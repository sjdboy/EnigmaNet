using System.Threading.Tasks;

namespace EnigmaNet.BusV2
{
    public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<TResult> HandleAsync(TCommand command);
    }

    public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Empty> where TCommand : ICommand<Empty>
    {
    }
}
