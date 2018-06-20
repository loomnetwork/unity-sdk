using System;

namespace Loom.Unity3d {
    public interface IChainEventArgs
    {
        Address ContractAddress { get; }
        Address CallerAddress { get; }
        UInt64 BlockHeight { get; }
        byte[] Data { get; }
        string EventName { get; }
    }
}