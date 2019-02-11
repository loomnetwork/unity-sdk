namespace Loom.Client
{
    public class DAppChainClientConfigurationProvider : IDAppChainClientConfigurationProvider
    {
        public DAppChainClientConfiguration Configuration { get; }

        public DAppChainClientConfigurationProvider(DAppChainClientConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}