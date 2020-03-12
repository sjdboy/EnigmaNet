using System.Threading.Tasks;

namespace EnigmaNet.BusV2
{
    public interface ICommandSubscriber
    {
        Task SubscribeAsync<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler) where TCommand : ICommand<TResult>;
    }
}
