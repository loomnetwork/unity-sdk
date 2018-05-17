using System.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;

namespace Loom.Unity3d
{
    /// <summary>
    /// The Contract class streamlines interaction with a smart contract that was deployed on a Loom DAppChain.
    /// Each instance of this class is bound to a specific smart contract, and provides a simple way of calling
    /// into and querying that contract.
    /// </summary>
    public class Contract
    {
        private DAppChainClient client;

        /// <summary>
        /// Smart contract address.
        /// </summary>
        public Address Address { get; internal set; }
        /// <summary>
        /// Caller/sender address to use when calling smart contract methods that mutate state.
        /// </summary>
        public Address Caller { get; internal set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client">Client to use to communicate with the contract.</param>
        /// <param name="contractAddr">Address of a contract on the Loom DAppChain.</param>
        /// <param name="callerAddr">Address of the caller, generated from the public key of the tx signer.</param>
        public Contract(DAppChainClient client, Address contractAddr, Address callerAddr)
        {
            this.client = client;
            this.Address = contractAddr;
            this.Caller = callerAddr;
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="args">Arguments object for the smart contract method.</param>
        /// <returns>Nothing.</returns>
        public async Task CallAsync(string method, IMessage args)
        {
            var tx = this.CreateContractMethodCallTx(method, args);
            await this.client.CommitTxAsync(tx);
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
            var result = await this.client.CommitTxAsync(tx);
            if (result != null && result.DeliverTx.Data != null && result.DeliverTx.Data.Length != 0)
            {
                var resp = new Response();
                resp.MergeFrom(result.DeliverTx.Data);
                if (resp.Body != null && resp.Body.Length != 0)
                {
                    T msg = new T();
                    msg.MergeFrom(resp.Body);
                    return msg;
                }
            }
            return default(T);
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
            var query = new ContractMethodCall
            {
                Method = method,
                Args = args.ToByteString()
            };
            var result = await this.client.QueryAsync<byte[]>(this.Address, query);
            if (result != null)
            {
                T msg = new T();
                msg.MergeFrom(result);
                return msg;
            }
            return default(T);
        }

        private Transaction CreateContractMethodCallTx(string method, IMessage args)
        {
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

            var callTxBytes = new CallTx
            {
                VmType = VMType.Plugin,
                Input = requestBytes
            }.ToByteString();

            var msgTxBytes = new MessageTx
            {
                From = this.Caller,
                To = this.Address,
                Data = callTxBytes
            }.ToByteString();

            return new Transaction
            {
                Id = 2,
                Data = msgTxBytes
            };
        }
    }
}
