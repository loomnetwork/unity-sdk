namespace Loom.Unity3d
{
    public class ChainEventArgs : IChainEventArgs
    {
        public Address ContractAddress { get; }
        public Address CallerAddress { get; }
        public ulong BlockHeight { get; }
        public byte[] Data { get; }
        public string EventName { get; }

        public ChainEventArgs(Address contractAddress, Address callerAddress, ulong blockHeight, byte[] data, string eventName)
        {
            this.ContractAddress = contractAddress;
            this.CallerAddress = callerAddress;
            this.BlockHeight = blockHeight;
            this.Data = data;
            this.EventName = eventName;
        }
    }
}
