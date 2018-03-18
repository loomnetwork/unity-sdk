using System.Threading.Tasks;

namespace Loom.Unity3d
{
    public interface IKeyStore
    {
        Task SetAsync(string key, byte[] privateKey);
        Task<byte[]> GetPrivateKeyAsync(string key);
        Task<string[]> GetKeysAsync();
    }
}
