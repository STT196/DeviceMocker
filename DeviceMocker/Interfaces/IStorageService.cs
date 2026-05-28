using System.Threading.Tasks;

namespace DeviceMocker.Interfaces
{
    public interface IStorageService
    {
        Task SaveAsync<T>(string path, T data);
        Task<T?> LoadAsync<T>(string path);
    }
}
