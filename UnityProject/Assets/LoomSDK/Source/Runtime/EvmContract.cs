using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Loom.Nethereum.ABI.Model;
using Loom.Nethereum.Contracts;
using Loom.Nethereum.RPC.Eth.DTOs;

namespace Loom.Client
{
    /// <summary>
    /// The EvmContract class streamlines interaction with a smart contract that was deployed on a EVM-based Loom DAppChain.
    /// Each instance of this class is bound to a specific smart contract, and provides a simple way of calling
    /// into and querying that contract.
    /// </summary>
    public class EvmContract : ContractBase<EvmChainEventArgs>
    {
        private readonly ContractBuilder contractBuilder;
        private readonly Dictionary<string, string> topicToEventName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client">Client to use to communicate with the contract.</param>
        /// <param name="contractAddress">Address of a contract on the Loom DAppChain.</param>
        /// <param name="callerAddress">Address of the caller, generated from the public key of the transaction signer.</param>
        /// <param name="contractBuilder">Contract Builder object representing the contract's interface.</param>
        public EvmContract(DAppChainClient client, Address contractAddress, Address callerAddress, ContractBuilder contractBuilder) : base(client, contractAddress, callerAddress)
        {
            if (contractBuilder == null)
                throw new ArgumentNullException(nameof(contractBuilder));

            this.contractBuilder = contractBuilder;
            this.topicToEventName = new Dictionary<string, string>();
            foreach (EventABI eventAbi in this.contractBuilder.ContractABI.Events)
            {
                this.topicToEventName.Add("0x" + eventAbi.Sha3Signature, eventAbi.Name);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client">Client to use to communicate with the contract.</param>
        /// <param name="contractAddress">Address of a contract on the Loom DAppChain.</param>
        /// <param name="callerAddress">Address of the caller, generated from the public key of the transaction signer.</param>
        /// <param name="abi">Contract Application Binary Interface as JSON object string.</param>
        public EvmContract(DAppChainClient client, Address contractAddress, Address callerAddress, string abi)
            : this(client, contractAddress, callerAddress, new ContractBuilder(abi, contractAddress.LocalAddress))
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client">Client to use to communicate with the contract.</param>
        /// <param name="contractAddress">Address of a contract on the Loom DAppChain.</param>
        /// <param name="callerAddress">Address of the caller, generated from the public key of the transaction signer.</param>
        /// <param name="abi">Deserialized Contract Application Binary Interface representation.</param>
        public EvmContract(DAppChainClient client, Address contractAddress, Address callerAddress, ContractABI abi)
            : this(client, contractAddress, callerAddress, CreateContractBuilderWithContractAbi(contractAddress, abi))
        {
        }

        /// <summary>
        /// Gets an instance of <see cref="EvmEvent{T}"/> set up for working with Solidity event named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Solidity event name.</param>
        /// <typeparam name="T">Event DTO type.</typeparam>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/calling-transactions-events/"/>
        public EvmEvent<T> GetEvent<T>(string name) where T : new()
        {
            return new EvmEvent<T>(this, this.contractBuilder.GetEventAbi(name));
        }

        #region CallAsync methods

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Arguments objects arrays for the smart contract method.</param>
        /// <returns>Nothing.</returns>
        public async Task<BroadcastTxResult> CallAsync(string method, params object[] functionInput)
        {
            FunctionBuilder function = this.contractBuilder.GetFunctionBuilder(method);
            CallInput callInput = function.CreateCallInput(functionInput);
            return await this.CallAsync(callInput.Data);
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <returns>Nothing.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<BroadcastTxResult> CallAsync<TInput>(TInput functionInput)
        {
            FunctionBuilder<TInput> function = this.contractBuilder.GetFunctionBuilder<TInput>();
            CallInput callInput = function.CreateCallInput(functionInput);
            return await this.CallAsync(callInput.Data);
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Arguments objects arrays for the smart contract method.</param>
        /// <returns>The return value of the smart contract method.</returns>
        public async Task<T> CallSimpleTypeOutputAsync<T>(string method, params object[] functionInput)
        {
            FunctionBuilder functionBuilder;
            return await this.CallAsync(this.CreateContractMethodCallInput(method, functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeSimpleTypeOutput<T>(s));
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Argument objects arrays for the smart contract method.</param>
        /// <returns>Return Data Transfer Object of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> CallDtoTypeOutputAsync<TReturn>(string method, params object[] functionInput) where TReturn : new()
        {
            FunctionBuilder functionBuilder;
            return await this.CallAsync(this.CreateContractMethodCallInput(method, functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeDTOTypeOutput<TReturn>(s));
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Argument objects arrays for the smart contract method.</param>
        /// <returns>Return Data Transfer Object of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        [Obsolete("Use CallDtoTypeOutputAsync", true)]
        public async Task<TReturn> CallDTOTypeOutputAsync<TReturn>(string method, params object[] functionInput) where TReturn : new()
        {
            return await CallDtoTypeOutputAsync<TReturn>(method, functionInput);
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Argument objects arrays for the smart contract method.</param>
        /// <param name="functionOutput">Return Data Transfer Object of the smart contract method. A pre-existing object can be reused.</param>
        /// <returns>Return Data Transfer Object of the smart contract method. Same object instance as <paramref name="functionOutput"/>.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> CallDtoTypeOutputAsync<TReturn>(TReturn functionOutput, string method, params object[] functionInput) where TReturn : new()
        {
            FunctionBuilder functionBuilder;
            return await this.CallAsync(this.CreateContractMethodCallInput(method, functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeDTOTypeOutput(functionOutput, s));
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Argument objects arrays for the smart contract method.</param>
        /// <param name="functionOutput">Return Data Transfer Object of the smart contract method. A pre-existing object can be reused.</param>
        /// <returns>Return Data Transfer Object of the smart contract method. Same object instance as <paramref name="functionOutput"/>.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        [Obsolete("Use CallDtoTypeOutputAsync", true)]
        public async Task<TReturn> CallDTOTypeOutputAsync<TReturn>(TReturn functionOutput, string method, params object[] functionInput) where TReturn : new()
        {
            return await CallDtoTypeOutputAsync(functionOutput, method, functionInput);
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <returns>The return value of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> CallSimpleTypeOutputAsync<TInput, TReturn>(TInput functionInput)
        {
            FunctionBuilder<TInput> functionBuilder;
            return await this.CallAsync(this.CreateContractMethodCallInput(functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeSimpleTypeOutput<TReturn>(s));
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <returns>Return Data Transfer Object of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> CallDtoTypeOutputAsync<TInput, TReturn>(TInput functionInput) where TReturn : new()
        {
            FunctionBuilder<TInput> functionBuilder;
            return await this.CallAsync(this.CreateContractMethodCallInput(functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeDTOTypeOutput<TReturn>(s));
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <returns>Return Data Transfer Object of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        [Obsolete("Use CallDtoTypeOutputAsync", true)]
        public async Task<TReturn> CallDTOTypeOutputAsync<TInput, TReturn>(TInput functionInput) where TReturn : new()
        {
            return await CallDtoTypeOutputAsync<TInput, TReturn>(functionInput);
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <param name="functionOutput">Return Data Transfer Object of the smart contract method. A pre-existing object can be reused.</param>
        /// <returns>Return Data Transfer Object of the smart contract method. Same object instance as <paramref name="functionOutput"/>.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> CallDtoTypeOutputAsync<TInput, TReturn>(TInput functionInput, TReturn functionOutput) where TReturn : new()
        {
            FunctionBuilder<TInput> functionBuilder;
            return await this.CallAsync(this.CreateContractMethodCallInput(functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeDTOTypeOutput(functionOutput, s));
        }

        /// <summary>
        /// Calls a smart contract method that mutates state.
        /// The call into the smart contract is accomplished by committing a transaction to the DAppChain.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <param name="functionOutput">Return Data Transfer Object of the smart contract method. A pre-existing object can be reused.</param>
        /// <returns>Return Data Transfer Object of the smart contract method. Same object instance as <paramref name="functionOutput"/>.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        [Obsolete("Use CallDtoTypeOutputAsync", true)]
        public async Task<TReturn> CallDTOTypeOutputAsync<TInput, TReturn>(TInput functionInput, TReturn functionOutput) where TReturn : new()
        {
            return await CallDtoTypeOutputAsync(functionInput, functionOutput);
        }

        #endregion

        #region StaticCallAsync methods

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Arguments objects arrays for the smart contract method.</param>
        /// <returns>Nothing.</returns>
        public async Task StaticCallAsync(string method, params object[] functionInput)
        {
            FunctionBuilder function = this.contractBuilder.GetFunctionBuilder(method);
            CallInput callInput = function.CreateCallInput(functionInput);
            await this.StaticCallAsync(callInput.Data);
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <returns>Nothing.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task StaticCallAsync<TInput>(TInput functionInput)
        {
            FunctionBuilder<TInput> function = this.contractBuilder.GetFunctionBuilder<TInput>();
            CallInput callInput = function.CreateCallInput(functionInput);
            await this.StaticCallAsync(callInput.Data);
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Arguments objects arrays for the smart contract method.</param>
        /// <returns>The return value of the smart contract method.</returns>
        public async Task<T> StaticCallSimpleTypeOutputAsync<T>(string method, params object[] functionInput)
        {
            FunctionBuilder functionBuilder;
            return await this.StaticCallAsync(this.CreateContractMethodCallInput(method, functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeSimpleTypeOutput<T>(s));
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Argument objects arrays for the smart contract method.</param>
        /// <returns>Return Data Transfer Object of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> StaticCallDtoTypeOutputAsync<TReturn>(string method, params object[] functionInput) where TReturn : new()
        {
            FunctionBuilder functionBuilder;
            return await this.StaticCallAsync(this.CreateContractMethodCallInput(method, functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeDTOTypeOutput<TReturn>(s));
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Argument objects arrays for the smart contract method.</param>
        /// <returns>Return Data Transfer Object of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        [Obsolete("Use StaticCallDtoTypeOutputAsync", true)]
        public async Task<TReturn> StaticCallDTOTypeOutputAsync<TReturn>(string method, params object[] functionInput) where TReturn : new()
        {
            return await StaticCallDtoTypeOutputAsync<TReturn>(method, functionInput);
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Argument objects arrays for the smart contract method.</param>
        /// <param name="functionOutput">Return Data Transfer Object of the smart contract method. A pre-existing object can be reused.</param>
        /// <returns>Return Data Transfer Object of the smart contract method. Same object instance as <paramref name="functionOutput"/>.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> StaticCallDtoTypeOutputAsync<TReturn>(TReturn functionOutput, string method, params object[] functionInput) where TReturn : new()
        {
            FunctionBuilder functionBuilder;
            return await this.StaticCallAsync(this.CreateContractMethodCallInput(method, functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeDTOTypeOutput(functionOutput, s));
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="method">Smart contract method name.</param>
        /// <param name="functionInput">Argument objects arrays for the smart contract method.</param>
        /// <param name="functionOutput">Return Data Transfer Object of the smart contract method. A pre-existing object can be reused.</param>
        /// <returns>Return Data Transfer Object of the smart contract method. Same object instance as <paramref name="functionOutput"/>.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        [Obsolete("Use StaticCallDtoTypeOutputAsync", true)]
        public async Task<TReturn> StaticCallDTOTypeOutputAsync<TReturn>(TReturn functionOutput, string method, params object[] functionInput) where TReturn : new()
        {
            return await StaticCallDtoTypeOutputAsync(functionOutput, method, functionInput);
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <returns>The return value of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> StaticCallSimpleTypeOutputAsync<TInput, TReturn>(TInput functionInput)
        {
            FunctionBuilder<TInput> functionBuilder;
            return await this.StaticCallAsync(this.CreateContractMethodCallInput(functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeSimpleTypeOutput<TReturn>(s));
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <returns>Return Data Transfer Object of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> StaticCallDtoTypeOutputAsync<TInput, TReturn>(TInput functionInput) where TReturn : new()
        {
            FunctionBuilder<TInput> functionBuilder;
            return await this.StaticCallAsync(this.CreateContractMethodCallInput(functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeDTOTypeOutput<TReturn>(s));
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <returns>Return Data Transfer Object of the smart contract method.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        [Obsolete("Use StaticCallDtoTypeOutputAsync", true)]
        public async Task<TReturn> StaticCallDTOTypeOutputAsync<TInput, TReturn>(TInput functionInput) where TReturn : new()
        {
            return await StaticCallDtoTypeOutputAsync<TInput, TReturn>(functionInput);
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <param name="functionOutput">Return Data Transfer Object of the smart contract method. A pre-existing object can be reused.</param>
        /// <returns>Return Data Transfer Object of the smart contract method. Same object instance as <paramref name="functionOutput"/>.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        public async Task<TReturn> StaticCallDtoTypeOutputAsync<TInput, TReturn>(TInput functionInput, TReturn functionOutput) where TReturn : new()
        {
            FunctionBuilder<TInput> functionBuilder;
            return await this.StaticCallAsync(this.CreateContractMethodCallInput(functionInput, out functionBuilder), functionBuilder, (fb, s) => fb.DecodeDTOTypeOutput(functionOutput, s));
        }

        /// <summary>
        /// Calls a contract method that doesn't mutate state.
        /// This method is usually used to query the current smart contract state, it doesn't commit any transactions.
        /// </summary>
        /// <param name="functionInput">Input Data Transfer Object for smart contract method.</param>
        /// <param name="functionOutput">Return Data Transfer Object of the smart contract method. A pre-existing object can be reused.</param>
        /// <returns>Return Data Transfer Object of the smart contract method. Same object instance as <paramref name="functionOutput"/>.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/functiondtos/"/>
        [Obsolete("Use StaticCallDtoTypeOutputAsync", true)]
        public async Task<TReturn> StaticCallDTOTypeOutputAsync<TInput, TReturn>(TInput functionInput, TReturn functionOutput) where TReturn : new()
        {
            return await StaticCallDtoTypeOutputAsync(functionInput, functionOutput);
        }

        #endregion

        protected override EvmChainEventArgs TransformChainEvent(RawChainEventArgs e)
        {
            if (e.Topics == null)
                throw new ArgumentNullException(nameof(e.Topics));

            // First topic is the signature of event itself
            string eventName;
            if (!this.topicToEventName.TryGetValue(e.Topics[0], out eventName))
                throw new EvmException($"Unknown topic {e.Topics[0]}, is the ABI out of sync with the contract?");

            return new EvmChainEventArgs(
                e.ContractAddress,
                e.CallerAddress,
                e.BlockHeight,
                e.Data,
                eventName,
                e.Topics
            );
        }

        #region Call helper methods

        private async Task<byte[]> StaticCallAsyncByteArray(string callInput)
        {
            return await this.Client.QueryAsync<byte[]>(
                this.Address,
                CryptoUtils.HexStringToBytes(callInput),
                this.Caller,
                Protobuf.VMType.Evm,
                new CallDescription("_evmQuery", true)
            );
        }

        private async Task StaticCallAsync(string callInput)
        {
            await this.StaticCallAsyncByteArray(callInput);
        }

        private async Task<TReturn> StaticCallAsync<TReturn>(string callInput, FunctionBuilderBase functionBuilder, Func<FunctionBuilderBase, string, TReturn> decodeFunc)
        {
            var result = await this.StaticCallAsyncByteArray(callInput);

            var validResult = result != null && result.Length != 0;
            return validResult ? decodeFunc(functionBuilder, CryptoUtils.BytesToHexString(result)) : default(TReturn);
        }

        private async Task<BroadcastTxResult> CallAsyncBroadcastTxResult(string callInput)
        {
            var tx = this.CreateContractMethodCallTx(callInput, Protobuf.VMType.Evm);
            return await this.Client.CommitTxAsync(tx, new CallDescription("_evmCall", false));
        }

        private async Task<BroadcastTxResult> CallAsync(string callInput)
        {
            return await this.CallAsyncBroadcastTxResult(callInput);
        }

        private async Task<TReturn> CallAsync<TReturn>(string callInput, FunctionBuilderBase functionBuilder, Func<FunctionBuilderBase, string, TReturn> decodeFunc)
        {
            var result = await CallAsyncBroadcastTxResult(callInput);
            var validResult = result?.DeliverTx.Data != null && result.DeliverTx.Data.Length != 0;
            return validResult ? decodeFunc(functionBuilder, CryptoUtils.BytesToHexString(result.DeliverTx.Data)) : default(TReturn);
        }

        private string CreateContractMethodCallInput(string method, object[] functionInput, out FunctionBuilder functionBuilder)
        {
            functionBuilder = this.contractBuilder.GetFunctionBuilder(method);
            CallInput callInput = functionBuilder.CreateCallInput(functionInput);
            return callInput.Data;
        }

        private string CreateContractMethodCallInput<TInput>(TInput functionInput, out FunctionBuilder<TInput> functionBuilder)
        {
            functionBuilder = this.contractBuilder.GetFunctionBuilder<TInput>();
            CallInput callInput = functionBuilder.CreateCallInput(functionInput);
            return callInput.Data;
        }

        private Protobuf.Transaction CreateContractMethodCallTx(string method, object[] functionInput, out FunctionBuilder functionBuilder)
        {
            functionBuilder = this.contractBuilder.GetFunctionBuilder(method);
            CallInput callInput = functionBuilder.CreateCallInput(functionInput);
            return this.CreateContractMethodCallTx(callInput.Data, Protobuf.VMType.Evm);
        }

        private Protobuf.Transaction CreateContractMethodCallTx<TInput>(TInput functionInput, out FunctionBuilder<TInput> functionBuilder)
        {
            functionBuilder = this.contractBuilder.GetFunctionBuilder<TInput>();
            CallInput callInput = functionBuilder.CreateCallInput(functionInput);
            return this.CreateContractMethodCallTx(callInput.Data, Protobuf.VMType.Evm);
        }

        #endregion

        private static ContractBuilder CreateContractBuilderWithContractAbi(Address contractAddress, ContractABI abi)
        {
            if (abi == null)
                throw new ArgumentNullException(nameof(abi));

            return new ContractBuilder("[]", contractAddress.LocalAddress)
            {
                ContractABI = abi
            };
        }
    }
}
