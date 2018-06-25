using System;
using Loom.Nethereum.ABI.FunctionEncoding;

namespace Loom.Unity3d
{
    public class EvmChainEventArgs : IChainEventArgs
    {
        public Address ContractAddress { get; }
        public Address CallerAddress { get; }
        public ulong BlockHeight { get; }
        public byte[] Data { get; }
        public string EventName { get; }

        /// <summary>
        /// Ethereum log topics for the event.
        /// </summary>
        public string[] Topics { get; }

        public EvmChainEventArgs(Address contractAddress, Address callerAddress, ulong blockHeight, byte[] data, string eventName, string[] topics)
        {
            this.ContractAddress = contractAddress;
            this.CallerAddress = callerAddress;
            this.BlockHeight = blockHeight;
            this.Data = data;
            this.EventName = eventName;
            this.Topics = topics;
        }

        /// <summary>
        /// Decodes event data into event DTO.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Decoded event DTO.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/calling-transactions-events/"/>
        public T DecodeEventDto<T>() where T : new()
        {
            EventTopicDecoder eventTopicDecoder = new EventTopicDecoder();
            return eventTopicDecoder.DecodeTopics<T>(this.Topics, CryptoUtils.BytesToHexString(this.Data));
        }

        /// <summary>
        /// Decodes event data into event DTO.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Decoded event DTO.</returns>
        /// <see href="https://nethereum.readthedocs.io/en/latest/contracts/calling-transactions-events/"/>
        [Obsolete("Use DecodeEventDto")]
        public T DecodeEventDTO<T>() where T : new()
        {
            return DecodeEventDto<T>();
        }
    }
}
