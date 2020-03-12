using System.Threading.Tasks;

namespace EnigmaNet.BusV2
{
    public interface ICommandExecuter
    {
        Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command);

        Task<object> ExecuteAsync(object command);
    }
}
