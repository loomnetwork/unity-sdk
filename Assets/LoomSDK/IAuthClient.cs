using System.Threading.Tasks;
using UnityEngine;

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
        ILogger Logger { get; set; }

        Task<string> GetAccessTokenAsync();
        Task<Identity> GetIdentityAsync(string accessToken, IKeyStore keyStore);
    }
}
