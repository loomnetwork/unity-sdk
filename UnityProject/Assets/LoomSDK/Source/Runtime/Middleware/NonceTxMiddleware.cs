using Loom.Google.Protobuf;
using System.Threading.Tasks;
using Loom.Client.Protobuf;
using UnityEngine;

namespace Loom.Client
{
    /// <summary>
    /// Adds a nonce to transactions.
    /// </summary>
    /// <remarks>
    /// Obtains the initial nonce value from the chain, and then increments it locally
    /// for every tx, if a tx fails due to a nonce mismatch the chain is queried again to obtain the
    /// latest nonce.
    /// </remarks>
    public class NonceTxMiddleware : ITxMiddlewareHandler
    {
        protected readonly string publicKeyHex;
        protected readonly object nonceSetLock = new object();

        /// <summary>
        /// Public key for which the nonce should be set.
        /// </summary>
        public byte[] PublicKey { get; }

        /// <summary>
        /// Client that should be used to retrieve the nonce.
        /// </summary>
        public DAppChainClient Client { get; }

        /// <summary>
        /// Next expected nonce.
        /// </summary>
        protected ulong? NextNonce { get; set; }

        /// <summary>
        /// Creates middleware that adds a nonce to transactions for given public key.
        /// </summary>
        /// <param name="publicKey">Public key for which the nonce should be set.</param>
        /// <param name="client">Client that should be used to retrieve the nonce.</param>
        public NonceTxMiddleware(byte[] publicKey, DAppChainClient client) {
            this.PublicKey = publicKey;
            this.Client = client;

            this.publicKeyHex = CryptoUtils.BytesToHexString(this.PublicKey);
        }

        public virtual async Task<byte[]> Handle(byte[] txData)
        {
            var nextNonce = await GetNextNonceAsync();

            this.Client.Logger.Log($"[NonceLog] Using nonce {this.NextNonce} for a TX");
            var tx = new NonceTx
            {
                Inner = ByteString.CopyFrom(txData),
                Sequence = nextNonce
            };
            return tx.ToByteArray();
        }

        public void HandleTxResult(BroadcastTxResult result)
        {
            lock (this.nonceSetLock)
            {
                this.NextNonce++;
                this.Client.Logger.Log($"[NonceLog] Call succeeded, speculating next nonce {this.NextNonce}");
            }
        }

        public void HandleTxException(LoomException e)
        {
            if (e is InvalidTxNonceException)
            {
                this.NextNonce = null;
                this.Client.Logger.Log("[NonceLog] Got InvalidTxNonceException, will retrieve nonce from node next time");
            } else if (e is TxCommitException)
            {
                this.Client.Logger.Log($"[NonceLog] Got {e.GetType().Name} ({e.Message}), so next nonce is still {this.NextNonce}");
            }
        }

        protected virtual async Task<ulong> GetNextNonceAsync()
        {
            if (this.NextNonce == null)
            {
                this.Client.Logger.Log("[NonceLog] NextNonce == null, retrieving from node...");
                ulong nonce = await GetNonceFromNodeAsync();
                lock (this.nonceSetLock)
                {
                    this.NextNonce = nonce + 1;
                    this.Client.Logger.Log($"[NonceLog] Got nonce {nonce} from the node, using {this.NextNonce}");
                }
            }

            return this.NextNonce.Value;
        }

        protected virtual async Task<ulong> GetNonceFromNodeAsync()
        {
            return await this.Client.GetNonceAsyncNonBlocking(this.publicKeyHex);
        }
    }
}
