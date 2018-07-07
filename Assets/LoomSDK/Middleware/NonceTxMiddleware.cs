using Google.Protobuf;
using System.Threading.Tasks;
using Loom.Unity3d.Internal.Protobuf;

namespace Loom.Unity3d
{
    /// <summary>
    /// Adds a nonce to transactions.
    /// </summary>
    public class NonceTxMiddleware : ITxMiddlewareHandler
    {
        private readonly string publicKeyHex;

        /// <summary>
        /// Public key for which the nonce should be set.
        /// </summary>
        public byte[] PublicKey { get; }

        /// <summary>
        /// Client that should be used to retrieve the nonce.
        /// </summary>
        public DAppChainClient Client { get; }

        /// <summary>
        /// Creates middleware that adds a nonce to transactions for given public key.
        /// </summary>
        /// <param name="publicKey">Public key for which the nonce should be set.</param>
        /// <param name="client">Client that should be used to retrieve the nonce.</param>
        public NonceTxMiddleware(byte[] publicKey, DAppChainClient client) {
            PublicKey = publicKey;
            Client = client;

            this.publicKeyHex = CryptoUtils.BytesToHexString(this.PublicKey);
        }

        public async Task<byte[]> Handle(byte[] txData)
        {
            var nonce = await this.Client.GetNonceAsync(this.publicKeyHex);
            var tx = new NonceTx
            {
                Inner = ByteString.CopyFrom(txData),
                Sequence = nonce + 1
            };
            return tx.ToByteArray();
        }
    }
}
