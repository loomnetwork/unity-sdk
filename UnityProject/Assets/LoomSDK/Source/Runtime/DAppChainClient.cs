using Loom.Google.Protobuf;
using Loom.Chaos.NaCl;
using UnityEngine;
using System.Threading.Tasks;
using System;
using Loom.Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Loom.Client.Protobuf;

namespace Loom.Client
{
    /// <summary>
    /// Writes to & reads from a Loom DAppChain.
    /// </summary>
    public class DAppChainClient : IDisposable
    {
        /// <summary>
        /// Topic name corresponding to all events emitted by the contract.
        /// </summary>
        public const string GlobalContractEventTopicName = "contract";

        private const string LogTag = "Loom.DAppChainClient";

        private readonly IRpcClient writeClient;
        private readonly IRpcClient readClient;

        private readonly HashSet<string> subscribedTopics = new HashSet<string>();

        private ILogger logger = NullLogger.Instance;

        /// <summary>
        /// RPC client to use for submitting transactions.
        /// </summary>
        public IRpcClient WriteClient => this.writeClient;

        /// <summary>
        /// RPC client to use for querying DAppChain state.
        /// </summary>
        public IRpcClient ReadClient => this.readClient;

        /// <summary>
        /// Middleware to apply when committing transactions.
        /// </summary>
        public TxMiddleware TxMiddleware { get; set; }

        /// <summary>
        /// Client options container.
        /// </summary>
        public DAppChainClientConfiguration Configuration { get; }

        /// <summary>
        /// Controls the flow of blockchain calls.
        /// </summary>
        public IDAppChainClientCallExecutor CallExecutor { get; }

        /// <summary>
        /// List of topics this client is currently subscribed to.
        /// </summary>
        public IReadOnlyCollection<string> SubscribedTopics => subscribedTopics;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger
        {
            get
            {
                return this.logger;
            }
            set
            {
                if (value == null)
                {
                    value = NullLogger.Instance;
                }

                this.logger = value;
            }
        }

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Events emitted by the DAppChain.
        /// </summary>
        public event EventHandler<RawChainEventArgs> ChainEventReceived;

        /// <summary>
        /// Constructs a client to read & write data from/to a Loom DAppChain.
        /// </summary>
        /// <param name="writeClient">RPC client to use for submitting transactions.</param>
        /// <param name="readClient">RPC client to use for querying DAppChain state.</param>
        /// <param name="configuration">Client configuration structure.</param>
        /// <param name="callExecutor">Blockchain call execution flow controller.</param>
        public DAppChainClient(
            IRpcClient writeClient,
            IRpcClient readClient,
            DAppChainClientConfiguration configuration = null,
            IDAppChainClientCallExecutor callExecutor = null)
        {
            if (writeClient == null && readClient == null)
                throw new ArgumentException("Both write and read clients can't be null");

            this.writeClient = writeClient;
            this.readClient = readClient;

            this.Configuration = configuration ?? new DAppChainClientConfiguration();
            this.CallExecutor = callExecutor ?? new DefaultDAppChainClientCallExecutor(this.Configuration);

            if (this.readClient != null)
            {
                this.readClient.EventReceived += ReadClientOnEventReceived;
                this.readClient.ConnectionStateChanged += ReadClientOnConnectionStateChanged;
            }
        }

        public virtual void Dispose()
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(nameof(DAppChainClient));

            this.writeClient?.Dispose();
            this.readClient?.Dispose();

            this.IsDisposed = true;
        }

        /// <summary>
        /// Gets a nonce for the given public key.
        /// </summary>
        /// <param name="key">A hex encoded public key, e.g. 441B9DCC47A734695A508EDF174F7AAF76DD7209DEA2D51D3582DA77CE2756BE</param>
        /// <returns>The nonce.</returns>
        public async Task<ulong> GetNonceAsync(string key)
        {
            return await this.CallExecutor.StaticCall(async () => await GetNonceAsyncRaw(key), new CallDescription("nonce", true));
        }

        /// <summary>
        /// Gets a nonce for the given public key, ignoring the call queue, if one is present.
        /// </summary>
        /// <param name="key">A hex encoded public key, e.g. 441B9DCC47A734695A508EDF174F7AAF76DD7209DEA2D51D3582DA77CE2756BE</param>
        /// <returns>The nonce.</returns>
        public async Task<ulong> GetNonceAsyncNonBlocking(string key)
        {
            return await this.CallExecutor.NonBlockingStaticCall(async () => await GetNonceAsyncRaw(key), new CallDescription("nonce", true));
        }

        /// <summary>
        /// Tries to resolve a contract name to an address.
        /// </summary>
        /// <param name="contractName">Name of a smart contract on a Loom DAppChain.</param>
        /// <exception cref="Exception">If a contract matching the given name wasn't found</exception>
        public async Task<Address> ResolveContractAddressAsync(string contractName)
        {
            if (this.readClient == null)
                throw new InvalidOperationException("Read client is not set");

            return await this.CallExecutor.StaticCall(
                async () =>
                {
                    await EnsureConnected();
                    var addressStr = await this.readClient.SendAsync<string, ResolveParams>(
                        "resolve",
                        new ResolveParams
                        {
                            ContractName = contractName
                        }
                    );

                    if (String.IsNullOrEmpty(addressStr))
                        throw new LoomException("Unable to find a contract with a matching name");

                    return Address.FromString(addressStr);
                },
                new CallDescription("resolve", true)
            );
        }

        /// <summary>
        /// Subscribes to chain events. Use <see cref="ChainEventReceived"/> to listen for events.
        /// </summary>
        /// <param name="topics">Topics to subscribe to. If empty, will subscribe to all topics.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SubscribeToEvents(ICollection<string> topics)
        {
            if (topics == null)
                throw new ArgumentNullException(nameof(topics));

            if (this.readClient == null)
                throw new InvalidOperationException("Read client is not set");

            if (this.readClient.ConnectionState != RpcConnectionState.Connected)
                throw new InvalidOperationException("Read client must be connected");

            // Account for empty list meaning subscribing to all events
            if (this.subscribedTopics.IsSupersetOf(topics))
                return;

            await this.CallExecutor.Call(
                async () =>
                {
                    await EnsureConnected();
                    await this.readClient.SubscribeToEventsAsync(topics);
                    this.subscribedTopics.UnionWith(topics);
                },
                new CallDescription("_subscribe", false)
            );
        }

        /// <summary>
        /// Subscribes to all chain events.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        [Obsolete("Use " + nameof(SubscribeToAllEvents))]
        public Task SubscribeToEvents()
        {
            return SubscribeToAllEvents();
        }

        /// <summary>
        /// Subscribes to all chain events.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public Task SubscribeToAllEvents()
        {
            return SubscribeToEvents(new[] { GlobalContractEventTopicName });
        }

        /// <summary>
        /// Unsubscribes from all chain events.
        /// </summary>
        [Obsolete("Use " + nameof(UnsubscribeFromAllEvents))]
        public async Task UnsubscribeFromEvents()
        {
            await UnsubscribeFromAllEvents();
        }

        /// <summary>
        /// Unsubscribes from all chain events.
        /// </summary>
        public async Task UnsubscribeFromAllEvents()
        {
            await UnsubscribeFromEvents(this.subscribedTopics.ToArray());
        }

        /// <summary>
        /// Unsubscribes from chain events listed in <paramref name="topics"/>.
        /// If <paramref name="topics"/>, the client won't unsubscribe from any events.
        /// </summary>
        public async Task UnsubscribeFromEvents(IReadOnlyList<string> topics)
        {
            if (topics == null)
                throw new ArgumentNullException(nameof(topics));

            if (this.readClient == null)
                throw new InvalidOperationException("Read client is not set");

            if (this.readClient.ConnectionState != RpcConnectionState.Connected)
                throw new InvalidOperationException("Read client must be connected");

            // Not unsubscribing from anything, exit early
            if (topics.Count == 0)
                return;

            foreach (string topic in topics)
            {
                if (!this.subscribedTopics.Contains(topic))
                    throw new ArgumentException("Not subscribed to topic: " + topic);
            }

            await this.CallExecutor.Call(
                async () =>
                {
                    await EnsureConnected();

                    // For whatever reason, "subevents" takes a list of topics,
                    // but "unsubevents" takes a single topic, so we have to unsubcribe one topic per call.
                    List<Exception> exceptions = null;
                    foreach (string topic in topics)
                    {
                        Logger.Log("Unsubscribing from topic " + topic);

                        try
                        {
                            await this.readClient.UnsubscribeFromEventAsync(topic);
                            this.subscribedTopics.Remove(topic);
                        }
                        catch (Exception e)
                        {
                            if (exceptions == null)
                            {
                                exceptions = new List<Exception>();
                            }

                            exceptions.Add(e);
                        }
                    }

                    if (exceptions != null && exceptions.Count != 0)
                    {
                        throw new RpcClientException(
                            "Unsubscribing from one or more topics has failed",
                            new AggregateException(exceptions),
                            -1,
                            this.readClient
                        );
                    }
                },
                new CallDescription("_unsubscribe", false)
            );
        }

        /// <summary>
        /// Commits a transaction to the DAppChain.
        /// </summary>
        /// <param name="tx">Transaction to commit.</param>
        /// <param name="callDescription">Call high-level description.</param>
        /// <returns>Commit metadata.</returns>
        /// <exception cref="InvalidTxNonceException">Thrown when transaction is rejected by the DAppChain due to a bad nonce.</exception>
        internal async Task<BroadcastTxResult> CommitTxAsync(IMessage tx, CallDescription callDescription)
        {
            if (this.writeClient == null)
                throw new InvalidOperationException("Write client was not set");

            return await this.CallExecutor.Call(
                async () =>
                {
                    await EnsureConnected();

                    byte[] txBytes = tx.ToByteArray();
                    if (this.TxMiddleware != null)
                    {
                        txBytes = await this.TxMiddleware.Handle(txBytes);
                    }

                    try
                    {
                        string payload = CryptoBytes.ToBase64String(txBytes);
                        var result = await this.writeClient.SendAsync<BroadcastTxResult, string[]>("broadcast_tx_commit", new[] { payload });
                        if (result == null)
                            return null;

                        CheckForTxError(result.CheckTx);
                        CheckForTxError(result.DeliverTx);

                        if (this.TxMiddleware != null)
                        {
                            this.TxMiddleware.HandleTxResult(result);
                        }

                        return result;
                    }
                    catch (LoomException e)
                    {
                        if (this.TxMiddleware != null)
                        {
                            this.TxMiddleware.HandleTxException(e);
                        }

                        throw;
                    }
                },
                callDescription
            );
        }

        /// <summary>
        /// Queries the current state of a contract.
        /// </summary>
        /// <typeparam name="T">The expected response type, must be deserializable with Newtonsoft.Json.</typeparam>
        /// <param name="contract">Address of the contract to query.</param>
        /// <param name="query">Query parameters object.</param>
        /// <param name="caller">Optional caller address.</param>
        /// <param name="vmType">Virtual machine type.</param>
        /// <param name="callDescription">Call high-level description.</param>
        /// <returns>Deserialized response.</returns>
        internal async Task<T> QueryAsync<T>(Address contract, IMessage query, Address caller, VMType vmType, CallDescription callDescription)
        {
            return await QueryAsync<T>(contract, query.ToByteArray(), caller, vmType, callDescription);
        }

        /// <summary>
        /// Queries the current state of a contract.
        /// </summary>
        /// <typeparam name="T">The expected response type, must be deserializable with Newtonsoft.Json.</typeparam>
        /// <param name="contract">Address of the contract to query.</param>
        /// <param name="query">Raw query parameters data.</param>
        /// <param name="caller">Optional caller address.</param>
        /// <param name="vmType">Virtual machine type.</param>
        /// <param name="callDescription">Call high-level description.</param>
        /// <returns>Deserialized response.</returns>
        internal async Task<T> QueryAsync<T>(Address contract, byte[] query, Address caller, VMType vmType, CallDescription callDescription)
        {
            if (this.readClient == null)
                throw new InvalidOperationException("Read client is not set");

            var queryParams = new QueryParams
            {
                ContractAddress = contract.LocalAddress,
                Params = query,
                VmType = vmType
            };

            if (caller.LocalAddress != null && caller.ChainId != null)
            {
                queryParams.CallerAddress = caller.QualifiedAddress;
            }

            return await this.CallExecutor.StaticCall(
                async () =>
                {
                    await EnsureConnected();
                    return await this.readClient.SendAsync<T, QueryParams>("query", queryParams);
                },
                callDescription
            );
        }

        private async Task<ulong> GetNonceAsyncRaw(string key)
        {
            await EnsureConnected();

            IRpcClient client = this.writeClient ?? this.readClient;
            string nonce = await client.SendAsync<string, NonceParams>(
                "nonce",
                new NonceParams { Key = key }
            );
            return UInt64.Parse(nonce);
        }

        private void ReadClientOnEventReceived(object sender, JsonRpcEventData e)
        {
            RawChainEventArgs rawChainEventArgs =
                new RawChainEventArgs(
                    e.ContractAddress,
                    e.CallerAddress,
                    UInt64.Parse(e.BlockHeight),
                    e.Data,
                    e.Topics
                );

            this.ChainEventReceived?.Invoke(this, rawChainEventArgs);
        }

        private void ReadClientOnConnectionStateChanged(IRpcClient sender, RpcConnectionState state)
        {
            this.subscribedTopics.Clear();
        }

        private async Task EnsureConnected()
        {
            if (!Configuration.AutoReconnect)
                return;

            if (this.readClient != null)
            {
                await EnsureConnected(this.readClient);
            }

            if (this.writeClient != null)
            {
                await EnsureConnected(this.writeClient);
            }
        }

        private async Task EnsureConnected(IRpcClient rpcClient)
        {
            // TODO: handle edge-case when ConnectionState == RpcConnectionState.Connecting
            if (rpcClient.ConnectionState != RpcConnectionState.Connected)
            {
                await rpcClient.ConnectAsync();
            }
        }

        private void CheckForTxError(BroadcastTxResult.TxResult result)
        {
            if (result.Code != 0)
            {
                if ((result.Code == 1) && (result.Error.StartsWith("sequence number does not match")))
                {
                    throw new InvalidTxNonceException(result.Code, result.Error);
                }

                throw new TxCommitException(result.Code, result.Error);
            }
        }

        private struct NonceParams
        {
            [JsonProperty("key")]
            public string Key;
        }

        private struct ResolveParams
        {
            [JsonProperty("name")]
            public string ContractName;
        }

        private struct QueryParams
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
    }
}
