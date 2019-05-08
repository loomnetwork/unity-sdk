using Loom.Google.Protobuf;
using System.Threading.Tasks;
using Loom.Client.Protobuf;

namespace Loom.Client
{
    /// <summary>
    /// Signs transactions.
    /// </summary>
    public class SignedTxMiddleware : ITxMiddlewareHandler
    {
        /// <summary>
        /// The private key that should be used to sign transactions.
        /// </summary>
        public byte[] PrivateKey { get; }

        /// <summary>
        /// Creates middleware that signs transactions with the given key.
        /// </summary>
        /// <param name="privateKey">The private key that should be used to sign transactions.</param>
        public SignedTxMiddleware(byte[] privateKey)
        {
            this.PrivateKey = privateKey;
        }

        public virtual Task<byte[]> Handle(byte[] txData)
        {
            var sig = CryptoUtils.Sign(txData, this.PrivateKey);

            var signedTx = new SignedTx
            {
                Inner = ByteString.CopyFrom(txData),
                Signature = ByteString.CopyFrom(sig.Signature),
                PublicKey = ByteString.CopyFrom(sig.PublicKey)
            };

            return Task.FromResult(signedTx.ToByteArray());
        }

        public void HandleTxResult(BroadcastTxResult result)
        {
        }

        public void HandleTxException(LoomException e)
        {
        }
    }
}
