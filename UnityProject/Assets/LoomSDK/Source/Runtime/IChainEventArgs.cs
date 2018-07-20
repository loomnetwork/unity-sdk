using System;

namespace Loom.Client {
    public interface IChainEventArgs
    {
        Address ContractAddress { get; }
        Address CallerAddress { get; }
        UInt64 BlockHeight { get; }
        byte[] Data { get; }
        string EventName { get; }
    }
}