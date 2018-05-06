using Chaos.NaCl;
using System.Threading.Tasks;
using UnityEngine;

namespace Loom.Unity3d
{
    public class Identity
    {
        public string Username { get; internal set; }

        /// <summary>
        /// 64-byte private key.
        /// </summary>
        public byte[] PrivateKey {
            get
            {
                return this.privateKey;
            }

            internal set
            {
                this.privateKey = value;
                this.PublicKey = CryptoUtils.PublicKeyFromPrivateKey(this.privateKey);
            }
        }

        /// <summary>
        /// 32-byte public key.
        /// Note that public key is generated from the private key, so the PrivateKey property must
        /// be set before this property will contain a valid public key.
        /// </summary>
        public byte[] PublicKey { get; private set; }

        private byte[] privateKey;
    }

    public interface IAuthClient
    {
        ILogger Logger { get; set; }

        Task<string> GetAccessTokenAsync();
        Task<Identity> GetIdentityAsync(string accessToken, IKeyStore keyStore);
        Task<Identity> CreateIdentityAsync(string accessToken, IKeyStore keyStore);
        Task ClearIdentityAsync();
    }
}
