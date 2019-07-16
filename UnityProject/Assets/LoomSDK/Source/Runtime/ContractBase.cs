using System;
using System.Numerics;
using System.Threading.Tasks;
using Loom.Client.Protobuf;
using Loom.Google.Protobuf;

namespace Loom.Client {
    /// <summary>
    /// The Contract class streamlines interaction with a smart contract that was deployed on a Loom DAppChain.
    /// Each instance of this class is bound to a specific smart contract, and provides a simple way of calling
    /// into and querying that contract.
    /// </summary>
    public abstract class ContractBase
    {
        /// <summary>
        /// Client that writes to and reads from a Loom DAppChain.
        /// </summary>
        public DAppChainClient Client { get; }

        /// <summary>
        /// Smart contract address.
        /// </summary>
        public Address Address { get; }

        /// <summary>
        /// Caller/sender address to use when calling smart contract methods that mutate state.
        /// </summary>
        public Address Caller { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client">Client to use to communicate with the contract.</param>
        /// <param name="contractAddress">Address of a contract on the Loom DAppChain.</param>
        /// <param name="callerAddress">Address of the caller, generated from the public key of the transaction signer.</param>
        protected ContractBase(DAppChainClient client, Address contractAddress, Address callerAddress)
        {
            this.Client = client;
            this.Address = contractAddress;
            this.Caller = callerAddress;
        }

        /// <summary>
        /// Retrieves the current block height.
        /// </summary>
        /// <returns></returns>
        public async Task<BigInteger> GetBlockHeight()
        {
            return await this.Client.CallExecutor.StaticCall(
                async () =>
                {
                    string heightString = await this.Client.ReadClient.SendAsync<string, object>("getblockheight", null);
                    return BigInteger.Parse(heightString);
                },
                new CallDescription("getblockheight", true)
            );
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="tx">Transaction message.</param>
        /// <param name="callDescription">Call high-level description.</param>
        /// <returns>Nothing.</returns>
        internal async Task<BroadcastTxResult> CallAsync(Transaction tx, CallDescription callDescription)
        {
            return await this.Client.CommitTxAsync(tx, callDescription);
        }

        internal Transaction CreateContractMethodCallTx(string hexData, VMType vmType) {
            return CreateContractMethodCallTx(ByteString.CopyFrom(CryptoUtils.HexStringToBytes(hexData)), vmType);
        }

        internal Transaction CreateContractMethodCallTx(ByteString callTxInput, VMType vmType) {
            var callTxBytes = new CallTx
            {
                VmType = vmType,
                Input = callTxInput
            }.ToByteString();

            var msgTxBytes = new MessageTx
            {
                From = this.Caller.ToProtobufAddress(),
                To = this.Address.ToProtobufAddress(),
                Data = callTxBytes
            };

            return new Transaction
            {
                Id = 2,
                Data = msgTxBytes.ToByteString()
            };
        }
    }

    /// <summary>
    /// The Contract class streamlines interaction with a smart contract that was deployed on a Loom DAppChain.
    /// Each instance of this class is bound to a specific smart contract, and provides a simple way of calling
    /// into and querying that contract.
    /// </summary>
    public abstract class ContractBase<TChainEvent> : ContractBase {
        protected ContractBase(DAppChainClient client, Address contractAddress, Address callerAddress) 
            : base(
            client,
            contractAddress,
            callerAddress)
        {
            this.Client.ChainEventReceived += NotifyContractEventReceived;
        }

        /// <summary>
        /// Event emitted by the corresponding smart contract.
        /// </summary>
        public event EventHandler<TChainEvent> EventReceived;

        protected void InvokeChainEvent(object sender, RawChainEventArgs e)
        {
            this.EventReceived?.Invoke(this, TransformChainEvent(e));
        }

        protected abstract TChainEvent TransformChainEvent(RawChainEventArgs e);

        protected virtual void NotifyContractEventReceived(object sender, RawChainEventArgs e)
        {
            if (e.ContractAddress == this.Address)
            {
                InvokeChainEvent(sender, e);
            }
        }
    }
}
