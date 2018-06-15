namespace Loom.Unity3d
{
    public class ChainEventArgs : RawChainEventArgs
    {
        public string EventName { get; }

        public ChainEventArgs(Address contractAddress, Address callerAddress, ulong blockHeight, byte[] data, string eventName)
            : base(contractAddress, callerAddress, blockHeight, data)
        {
            this.EventName = eventName;
        }
    }
}
