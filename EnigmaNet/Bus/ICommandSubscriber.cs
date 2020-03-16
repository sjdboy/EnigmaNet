using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    public interface ICommandSubscriber
    {
        Task SubscribeAsync<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler) where TCommand : ICommand<TResult>;
    }
}
