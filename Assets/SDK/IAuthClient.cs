using System.Threading.Tasks;

namespace Loom.Unity3d
{
    public class Identity
    {
        public string Username { get; internal set; }
        /// <summary>
        /// 32-byte private key.
        /// </summary>
        public byte[] PrivateKey { get; internal set; }
    }

    public interface IAuthClient
    {
        Task<string> GetAccessTokenAsync();
        Task<Identity> GetIdentityAsync(string accessToken, IKeyStore keyStore);
    }
}
