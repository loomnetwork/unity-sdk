using System;

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
    }
}