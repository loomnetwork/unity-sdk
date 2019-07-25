using System.Text;
using System.Threading.Tasks;
using Loom.Client.Protobuf;
using Loom.Google.Protobuf;
using Loom.Newtonsoft.Json;

namespace Loom.Client
{
    /// <summary>
    /// The Contract class streamlines interaction with a smart contract that was deployed on a Loom DAppChain.
    /// Each instance of this class is bound to a specific smart contract, and provides a simple way of calling
    /// into and querying that contract.
    /// </summary>
    public abstract class Contract<TChainEvent> : ContractBase<TChainEvent>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client">Client to use to communicate with the contract.</param>
        /// <param name="contractAddress">Address of a contract on the Loom DAppChain.</param>
        /// <param name="callerAddress">Address of the caller, generated from the public key of the transaction signer.</param>
        public Contract(DAppChainClient client, Address contractAddress, Address callerAddress) : base(client, contractAddress, callerAddress) {
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="args">Arguments object for the smart contract method.</param>
        /// <returns>Nothing.</returns>
        public async Task<BroadcastTxResult> CallAsync(string method, IMessage args)
        {
            Transaction tx = this.CreateContractMethodCallTx(method, args);
            return await CallAsync(tx, new CallDescription(method, false));
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <typeparam name="T">Smart contract method return type.</typeparam>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="args">Arguments object for the smart contract method.</param>
        /// <returns>The return value of the smart contract method.</returns>
        public async Task<T> CallAsync<T>(string method, IMessage args) where T : IMessage, new()
        {
            var tx = this.CreateContractMethodCallTx(method, args);
            return await CallAsync<T>(tx, new CallDescription(method, false));
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <typeparam name="T">Smart contract method return type.</typeparam>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="args">Arguments object for the smart contract method.</param>
        /// <returns>The return value of the smart contract method.</returns>
        public async Task<T> StaticCallAsync<T>(string method, IMessage args) where T : IMessage, new()
        {
            this.Client.Logger.Log("Executing static call: " + method);
            var query = new ContractMethodCall
            {
                Method = method,
                Args = args.ToByteString()
            };
            var result = await this.Client.QueryAsync<byte[]>(this.Address, query, this.Caller, VMType.Plugin, new CallDescription(method, true));
            T msg = new T();
            if (result != null)
            {
                msg.MergeFrom(result);
            }

            return msg;
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <typeparam name="T">Smart contract method return type.</typeparam>
        /// <param name="tx">Transaction message.</param>
        /// <param name="callDescription">Call high-level description.</param>
        /// <returns>The return value of the smart contract method.</returns>
        private async Task<T> CallAsync<T>(Transaction tx, CallDescription callDescription) where T : IMessage, new()
        {
            var result = await this.Client.CommitTxAsync(tx, callDescription);
            if (result != null && result.DeliverTx.Data != null)
            {
                var resp = new Response();
                resp.MergeFrom(result.DeliverTx.Data);
                if (resp.Body != null)
                {
                    T msg = new T();
                    msg.MergeFrom(resp.Body);
                    return msg;
                }
            }
            return default(T);
        }

        private Transaction CreateContractMethodCallTx(string method, IMessage args)
        {
            this.Client.Logger.Log("Executing call: " + method);
            var methodTx = new ContractMethodCall
            {
                Method = method,
                Args = args.ToByteString()
            };

            var requestBytes = new Request
            {
                ContentType = EncodingType.Protobuf3,
                Accept = EncodingType.Protobuf3,
                Body = methodTx.ToByteString()
            }.ToByteString();

            return CreateContractMethodCallTx(requestBytes, VMType.Plugin);
        }
    }

    /// <summary>
    /// The Contract class streamlines interaction with a smart contract that was deployed on a Loom DAppChain.
    /// Each instance of this class is bound to a specific smart contract, and provides a simple way of calling
    /// into and querying that contract.
    /// </summary>
    /// <remarks>Expects the event data to be a UTF8 string containing a <see cref="JsonRpcEvent"/></remarks>
    public class Contract : Contract<ChainEventArgs> {
        public Contract(DAppChainClient client, Address contractAddress, Address callerAddress)
            : base(client, contractAddress, callerAddress)
        {
        }

        protected override ChainEventArgs TransformChainEvent(RawChainEventArgs e) {
            string jsonRpcEventString = Encoding.UTF8.GetString(e.Data);
            JsonRpcEvent jsonRpcEvent = JsonConvert.DeserializeObject<JsonRpcEvent>(jsonRpcEventString);
            byte[] eventData = Encoding.UTF8.GetBytes(jsonRpcEvent.Data);

            return new ChainEventArgs(
                e.ContractAddress,
                e.CallerAddress,
                e.BlockHeight,
                eventData,
                jsonRpcEvent.Method
            );
        }

        private struct JsonRpcEvent
        {
            [JsonProperty("Data")]
            public string Data;

            [JsonProperty("Method")]
            public string Method;
        }
    }
}
