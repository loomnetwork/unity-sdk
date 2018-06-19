using System;
using System.Threading.Tasks;
using Google.Protobuf;

namespace Loom.Unity3d {
    /// <summary>
    /// The Contract class streamlines interaction with a smart contract that was deployed on a Loom DAppChain.
    /// Each instance of this class is bound to a specific smart contract, and provides a simple way of calling
    /// into and querying that contract.
    /// </summary>
    public abstract class ContractBase<TChainEvent> {
        protected DAppChainClient client;
        protected event EventHandler<TChainEvent> eventReceived;

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
        /// <param name="contractAddr">Address of a contract on the Loom DAppChain.</param>
        /// <param name="callerAddr">Address of the caller, generated from the public key of the tx signer.</param>
        public ContractBase(DAppChainClient client, Address contractAddr, Address callerAddr)
        {
            this.client = client;
            this.Address = contractAddr;
            this.Caller = callerAddr;
        }

        /// <summary>
        /// Event emitted by the corresponding smart contract.
        /// </summary>
        public event EventHandler<TChainEvent> EventReceived
        {
            add
            {
                var isFirstSub = this.eventReceived == null;
                this.eventReceived += value;
                if (isFirstSub)
                {
                    this.client.ChainEventReceived += this.NotifyContractEventReceived;
                }
            }
            remove
            {
                this.eventReceived -= value;
                if (this.eventReceived == null)
                {
                    this.client.ChainEventReceived -= this.NotifyContractEventReceived;
                }
            }
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="tx">Transaction message.</param>
        /// <returns>Nothing.</returns>
        protected async Task CallAsync(Transaction tx)
        {
            await this.client.CommitTxAsync(tx);
        }

        protected Transaction CreateContractMethodCallTx(string hexData, VMType vmType) {
            return CreateContractMethodCallTx(ByteString.CopyFrom(CryptoUtils.HexStringToBytes(hexData)), vmType);
        }

        protected Transaction CreateContractMethodCallTx(ByteString callTxInput, VMType vmType) {
            var callTxBytes = new CallTx
            {
                VmType = vmType,
                Input = callTxInput
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

        protected void InvokeChainEvent(object sender, RawChainEventArgs e)
        {
            if (this.eventReceived != null)
            {
                this.eventReceived(this, TransformChainEvent(e));
            }
        }

        protected abstract TChainEvent TransformChainEvent(RawChainEventArgs e);

        protected virtual void NotifyContractEventReceived(object sender, RawChainEventArgs e)
        {
            if (e.ContractAddress.Equals(this.Address))
            {
                InvokeChainEvent(sender, e);
            }
        }
    }
}