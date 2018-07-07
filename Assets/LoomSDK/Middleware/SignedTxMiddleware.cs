using Google.Protobuf;
using System.Threading.Tasks;
using Loom.Unity3d.Internal.Protobuf;

namespace Loom.Unity3d
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

        public Task<byte[]> Handle(byte[] txData)
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
    }
}
