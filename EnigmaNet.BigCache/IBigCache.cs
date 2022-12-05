using System.Threading.Tasks;

namespace EnigmaNet.BigCache
{
    public interface IBigCache
    {
        Task<T> GetAsync<T>(string key);

        Task RemoveAsync(string key);

        Task SetAsync<T>(string key, T obj);
    }
}