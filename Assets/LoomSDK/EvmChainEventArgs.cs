using Loom.Nethereum.ABI.FunctionEncoding;

namespace Loom.Unity3d {
    public class EvmChainEventArgs : RawChainEventArgs
    {
        public byte[][] Topics { get; }
        public string EventName { get; }

        public EvmChainEventArgs(Address contractAddress, Address callerAddress, ulong blockHeight, byte[] data, byte[][] topics, string eventName)
            : base(contractAddress, callerAddress, blockHeight, data) {
            Topics = topics;
            EventName = eventName;
        }

        /// <summary>
        /// Decodes event data into event DTO.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Decoded event DTO.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/calling-transactions-events/"/>
        public T DecodeEvent<T>() where T : new()
        {
            EventTopicDecoder eventTopicDecoder = new EventTopicDecoder();
            object[] topicStrings = new object[Topics.Length];
            for (int i = 0; i < Topics.Length; i++)
            {
                topicStrings[i] = CryptoUtils.BytesToHexString(Topics[i]);
            }

            return eventTopicDecoder.DecodeTopics<T>(topicStrings, CryptoUtils.BytesToHexString(Data));
        }
    }
}