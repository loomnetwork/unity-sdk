using System;

namespace Loom.Unity3d {
    public class RawChainEventArgs
    {
        public Address ContractAddress { get; }
        public Address CallerAddress { get;}
        public UInt64 BlockHeight { get; }
        public byte[] Data { get; }

        public RawChainEventArgs(Address contractAddress, Address callerAddress, ulong blockHeight, byte[] data) {
            ContractAddress = contractAddress;
            CallerAddress = callerAddress;
            BlockHeight = blockHeight;
            Data = data;
        }
    }
}