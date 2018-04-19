using Google.Protobuf;
using System.Threading.Tasks;

namespace Loom.Unity3d
{
    /// <summary>
    /// Adds a nonce to txs.
    /// </summary>
    public class NonceTxMiddleware : ITxMiddlewareHandler
    {
        /// <summary>
        /// Public key for which the nonce should be set. 
        /// </summary>
        public byte[] PublicKey { get; set; }

        /// <summary>
        /// Client that should be used to retrieve the nonce.
        /// </summary>
        public DAppChainClient Client { get; set; }

        public async Task<byte[]> Handle(byte[] txData)
        {
            var key = CryptoUtils.BytesToHexString(this.PublicKey);
            var nonce = await this.Client.GetNonceAsync(key);
            var tx = new NonceTx
            {
                Inner = ByteString.CopyFrom(txData),
                Sequence = nonce + 1
            };
            return tx.ToByteArray();
        }
    }
}
