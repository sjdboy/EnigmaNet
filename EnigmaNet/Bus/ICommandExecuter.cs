using System.Threading.Tasks;

namespace EnigmaNet.Bus
{
    public interface ICommandExecuter
    {
        Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command);
    }
}
