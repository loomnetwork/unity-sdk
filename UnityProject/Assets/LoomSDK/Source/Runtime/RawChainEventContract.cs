namespace Loom.Client
{
    /// <summary>
    /// A <see cref="Contract{TChainEvent}"/> that skips the transforming of the binary event data
    /// and uses it without any modifications.
    /// </summary>
    public class RawChainEventContract : Contract<RawChainEventArgs>
    {
        public RawChainEventContract(DAppChainClient client, Address contractAddress, Address callerAddress)
            : base(client, contractAddress, callerAddress)
        {
        }

        protected override RawChainEventArgs TransformChainEvent(RawChainEventArgs e)
        {
            return e;
        }
    }
}
