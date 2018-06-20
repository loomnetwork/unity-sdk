using Google.Protobuf;
using Chaos.NaCl;
using UnityEngine;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Loom.Unity3d
{
    #region JSON RPC Interfaces

    public class BroadcastTxResult
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonProperty("log")]
        public string Log { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        /// <summary>
        /// Block height at which the Tx was committed.
        /// </summary>
        [JsonProperty("height")]
        public long Height { get; set; }

        public class TxResult
        {
            [JsonProperty("code")]
            public int Code { get; set; }
            [JsonProperty("log")]
            public string Error { get; set; }
            [JsonProperty("data")]
            public byte[] Data { get; set; }
        }

        [JsonProperty("check_tx")]
        public TxResult CheckTx { get; set; }
        [JsonProperty("deliver_tx")]
        public TxResult DeliverTx { get; set; }
    }

    #endregion

    /// <summary>
    /// Used to signal that a transaction was rejected because it had a bad nonce.
    /// </summary>
    public class InvalidTxNonceException : Exception
    {
        public InvalidTxNonceException()
        {
        }

        public InvalidTxNonceException(string msg) : base(msg)
        {
        }
    }

    /// <summary>
    /// Writes to & reads from a Loom DAppChain.
    /// </summary>
    public class DAppChainClient : IDisposable
    {

        private static readonly string LogTag = "Loom.DAppChainClient";

        private Dictionary<EventHandler<RawChainEventArgs>, EventHandler<JsonRpcEventData>> eventSubs;

        private IRPCClient writeClient;
        private IRPCClient readClient;

        /// <summary>
        /// Middleware to apply when committing transactions.
        /// </summary>
        public TxMiddleware TxMiddleware { get; set; }

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Maximum number of times a tx should be resent after being rejected because of a bad nonce.
        /// Defaults to 5.
        /// </summary>
        public int NonceRetries { get; set; }

        /// <summary>
        /// Events emitted by the DAppChain.
        /// </summary>
        public event EventHandler<RawChainEventArgs> ChainEventReceived
        {
            add
            {
                this.SubReadClient(value);
            }
            remove
            {
                this.UnsubReadClient(value);
            }
        }

        /// <summary>
        /// Constructs a client to read & write data from/to a Loom DAppChain.
        /// </summary>
        /// <param name="writeClient">RPC client to use for submitting txs.</param>
        /// <param name="readClient">RPC client to use for querying DAppChain state.</param>
        public DAppChainClient(IRPCClient writeClient, IRPCClient readClient)
        {
            this.eventSubs = new Dictionary<EventHandler<RawChainEventArgs>, EventHandler<JsonRpcEventData>>();
            this.writeClient = writeClient;
            this.readClient = readClient;
            this.Logger = NullLogger.Instance;
            this.NonceRetries = 5;
        }

        public void Dispose()
        {
            if (this.writeClient != null)
            {
                this.writeClient.Dispose();
                this.writeClient = null;
            }
            if (this.readClient != null)
            {
                this.readClient.Dispose();
                this.readClient = null;
            }
        }

        private async void SubReadClient(EventHandler<RawChainEventArgs> handler)
        {
            try
            {
                EventHandler<JsonRpcEventData> wrapper = (sender, e) =>
                {
                    handler(this, new RawChainEventArgs
                    (
                        new Address
                        {
                            ChainId = e.ContractAddress.ChainID,
                            Local = ByteString.CopyFrom(e.ContractAddress.Local)
                        },
                        new Address
                        {
                            ChainId = e.CallerAddress.ChainID,
                            Local = ByteString.CopyFrom(e.CallerAddress.Local)
                        },
                        e.BlockHeight,
                        e.Data,
                        e.Topics
                    ));
                };
                this.eventSubs.Add(handler, wrapper);
                await this.readClient.SubscribeAsync(wrapper);
            }
            catch (Exception e)
            {
                Logger.Log(LogTag, e.Message);
            }
        }

        private async void UnsubReadClient(EventHandler<RawChainEventArgs> handler)
        {
            try
            {
                EventHandler<JsonRpcEventData> wrapper = this.eventSubs[handler];
                await this.readClient.UnsubscribeAsync(wrapper);
            }
            catch (Exception e)
            {
                Logger.Log(LogTag, e.Message);
            }
        }

        /// <summary>
        /// Commits a transaction to the DAppChain.
        /// </summary>
        /// <param name="tx">Transaction to commit.</param>
        /// <returns>Commit metadata.</returns>
        /// <exception cref="InvalidTxNonceException">Thrown if the tx is rejected due to a bad nonce after <see cref="NonceRetries"/> attempts.</exception>
        public async Task<BroadcastTxResult> CommitTxAsync(IMessage tx)
        {
            BroadcastTxResult result = null;
            int badNonceCount = 0;
            do
            {
                try
                {
                    return await this.TryCommitTxAsync(tx);
                }
                catch (InvalidTxNonceException)
                {
                    ++badNonceCount;
                }
                await new WaitForSecondsRealtime(0.5f);
            } while ((this.NonceRetries != 0) && (badNonceCount <= this.NonceRetries));

            if (badNonceCount > 0)
            {
                throw new InvalidTxNonceException();
            }
            return result;
        }

        /// <summary>
        /// Tries to commit a transaction to the DAppChain.
        /// </summary>
        /// <param name="tx">Transaction to commit.</param>
        /// <returns>Commit metadata.</returns>
        /// <exception cref="InvalidTxNonceException">Thrown when the tx is rejected by the DAppChain due to a bad nonce.</exception>
        private async Task<BroadcastTxResult> TryCommitTxAsync(IMessage tx)
        {
            byte[] txBytes = tx.ToByteArray();
            if (this.TxMiddleware != null)
            {
                txBytes = await this.TxMiddleware.Handle(txBytes);
            }
            string payload = CryptoBytes.ToBase64String(txBytes);
            var result = await this.writeClient.SendAsync<BroadcastTxResult, string[]>("broadcast_tx_commit", new string[] { payload });
            if (result != null)
            {
                if (result.CheckTx.Code != 0)
                {
                    if (string.IsNullOrEmpty(result.CheckTx.Error))
                    {
                        throw new Exception(String.Format("Failed to commit Tx: {0}", result.CheckTx.Code));
                    }
                    if ((result.CheckTx.Code == 1) && (result.CheckTx.Error == "sequence number does not match"))
                    {
                        throw new InvalidTxNonceException();
                    }
                    throw new Exception(String.Format("Failed to commit Tx: {0}", result.CheckTx.Error));
                }
                if (result.DeliverTx.Code != 0)
                {
                    if (string.IsNullOrEmpty(result.DeliverTx.Error))
                    {
                        throw new Exception(String.Format("Failed to commit Tx: {0}", result.DeliverTx.Code));
                    }
                    throw new Exception(String.Format("Failed to commit Tx: {0}", result.DeliverTx.Error));
                }
            }
            return result;
        }

        private class QueryParams
        {
            /// <summary>
            /// Contract address
            /// </summary>
            [JsonProperty("contract")]
            public string ContractAddress;

            /// <summary>
            /// Serialized protobuf of contract-specific query parameters
            /// </summary>
            [JsonProperty("query")]
            public byte[] Params;

            /// <summary>
            /// Optional caller address (including chain ID)
            /// </summary>
            [JsonProperty("caller")]
            public string CallerAddress;

            /// <summary>
            /// Virtual machine type.
            /// </summary>
            [JsonProperty("vmType")]
            public VMType VmType;
        }

        /// <summary>
        /// Queries the current state of a contract.
        /// </summary>
        /// <typeparam name="T">The expected response type, must be deserializable with Newtonsoft.Json.</typeparam>
        /// <param name="contract">Address of the contract to query.</param>
        /// <param name="query">Query parameters object.</param>
        /// <param name="caller">Optional caller address.</param>
        /// <returns>Deserialized response.</returns>
        public async Task<T> QueryAsync<T>(Address contract, IMessage query, Address caller = null, VMType vmType = VMType.Plugin)
        {
            return await QueryAsync<T>(contract, query.ToByteArray(), caller, vmType);
        }

        /// <summary>
        /// Queries the current state of a contract.
        /// </summary>
        /// <typeparam name="T">The expected response type, must be deserializable with Newtonsoft.Json.</typeparam>
        /// <param name="contract">Address of the contract to query.</param>
        /// <param name="query">Raw query parameters data.</param>
        /// <param name="caller">Optional caller address.</param>
        /// <returns>Deserialized response.</returns>
        public async Task<T> QueryAsync<T>(Address contract, byte[] query, Address caller = null, VMType vmType = VMType.Plugin)
        {
            var queryParams = new QueryParams
            {
                ContractAddress = contract.LocalAddressHexString,
                Params = query,
                VmType = vmType
            };
            if (caller != null)
            {
                queryParams.CallerAddress = caller.ToAddressString();
            }
            return await this.readClient.SendAsync<T, QueryParams>("query", queryParams);
        }

        private class NonceParams
        {
            [JsonProperty("key")]
            public string Key;
        }

        /// <summary>
        /// Gets a nonce for the given public key.
        /// </summary>
        /// <param name="key">A hex encoded public key, e.g. 441B9DCC47A734695A508EDF174F7AAF76DD7209DEA2D51D3582DA77CE2756BE</param>
        /// <returns>The nonce.</returns>
        public async Task<ulong> GetNonceAsync(string key)
        {
            return await this.readClient.SendAsync<ulong, NonceParams>(
                "nonce", new NonceParams { Key = key }
            );
        }

        private class ResolveParams
        {
            [JsonProperty("name")]
            public string ContractName;
        }

        /// <summary>
        /// Tries to resolve a contract name to an address.
        /// </summary>
        /// <param name="contractName">Name of a smart contract on a Loom DAppChain.</param>
        /// <returns>Contract address, or null if a contract matching the given name wasn't found.</returns>
        public async Task<Address> ResolveContractAddressAsync(string contractName)
        {
            var addrStr = await this.readClient.SendAsync<string, ResolveParams>(
                "resolve", new ResolveParams { ContractName = contractName }
            );
            if (string.IsNullOrEmpty(addrStr))
            {
                return null;
            }
            return Address.FromAddressString(addrStr);
        }
    }
}