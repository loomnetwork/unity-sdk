using Loom.Nethereum.ABI.FunctionEncoding;

namespace Loom.Unity3d {
    public class EvmChainEventArgs : RawChainEventArgs
    {
        /// <summary>
        /// Ethereum log topics for the event.
        /// </summary>
        public byte[][] Topics { get; }

        /// <summary>
        /// Event name from Solidity contract.
        /// </summary>
        public string EventName { get; }

        public EvmChainEventArgs(Address contractAddress, Address callerAddress, ulong blockHeight, byte[] data, byte[][] topics, string eventName)
            : base(contractAddress, callerAddress, blockHeight, data) {
            this.Topics = topics;
            this.EventName = eventName;
        }

        /// <summary>
        /// Decodes event data into event DTO.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Decoded event DTO.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/calling-transactions-events/"/>
        public T DecodeEventDTO<T>() where T : new()
        {
            EventTopicDecoder eventTopicDecoder = new EventTopicDecoder();
            object[] topicStrings = new object[this.Topics.Length];
            for (int i = 0; i < this.Topics.Length; i++)
            {
                topicStrings[i] = CryptoUtils.BytesToHexString(this.Topics[i]);
            }

            return eventTopicDecoder.DecodeTopics<T>(topicStrings, CryptoUtils.BytesToHexString(this.Data));
        }
    }
}