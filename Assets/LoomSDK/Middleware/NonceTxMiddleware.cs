using Google.Protobuf;
using System.Threading.Tasks;

namespace Loom.Unity3d
{
    class NonceResponse
    {
        public long Data { get; internal set; }
    }

    /// <summary>
    /// Adds a nonce to txs.
    /// </summary>
    public class NonceTxMiddleware : ITxMiddlewareHandler
    {
        /// <summary>
        /// Public key for which the nonce should be set. 
        /// </summary>
        byte[] PublicKey { get; set; }

        /// <summary>
        /// Client that should be used to retrieve the nonce.
        /// </summary>
        DAppChainClient Client { get; set; }

        public async Task<byte[]> Handle(byte[] txData)
        {
            var key = CryptoUtils.BytesToHexString(this.PublicKey);
            var resp = await this.Client.QueryAsync<NonceResponse>("query/nonce/" + key);
            var tx = new NonceTx
            {
                Inner = ByteString.CopyFrom(txData),
                Sequence = resp.Data,
                PublicKey = ByteString.CopyFrom(this.PublicKey)
            };
            return tx.ToByteArray();
        }
    }
}
