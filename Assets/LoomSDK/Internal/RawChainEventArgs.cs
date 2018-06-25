using System;

namespace Loom.Unity3d.Internal
{
    public sealed class RawChainEventArgs
    {
        public Address ContractAddress { get; }
        public Address CallerAddress { get; }
        public UInt64 BlockHeight { get; }
        public byte[] Data { get; }

        /// <summary>
        /// Ethereum log topics for the event.
        /// </summary>
        public string[] Topics { get; }

        public RawChainEventArgs(Address contractAddress, Address callerAddress, ulong blockHeight, byte[] data, string[] topics)
        {
            this.ContractAddress = contractAddress;
            this.CallerAddress = callerAddress;
            this.BlockHeight = blockHeight;
            this.Data = data;
            this.Topics = topics;
        }
    }
}
