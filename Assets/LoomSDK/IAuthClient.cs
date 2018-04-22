using Chaos.NaCl;
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
        public byte[] PrivateKey {
            get
            {
                return this.privateKey32;    
            }

            internal set
            {
                this.privateKey32 = value;
                byte[] publicKey32;
                byte[] privateKey64;
                Ed25519.KeyPairFromSeed(out publicKey32, out privateKey64, this.privateKey32);
                this.PublicKey = publicKey32;
            }
        }

        /// <summary>
        /// 32-byte public key.
        /// Note that public key is generated from the private key, so the PrivateKey property must
        /// be set before this property will contain a valid public key.
        /// </summary>
        public byte[] PublicKey { get; private set; }

        private byte[] privateKey32;
    }

    public interface IAuthClient
    {
        ILogger Logger { get; set; }

        Task<string> GetAccessTokenAsync();
        Task<Identity> GetIdentityAsync(string accessToken, IKeyStore keyStore);
    }
}
