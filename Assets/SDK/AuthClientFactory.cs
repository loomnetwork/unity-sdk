namespace Loom.Unity3d
{
    public class AuthClientFactory
    {
        private string vaultPrefix;
        private string redirectUrl;

        public static AuthClientFactory Configure()
        {
            return new AuthClientFactory();
        }

        public AuthClientFactory WithVaultPrefix(string prefix)
        {
            this.vaultPrefix = prefix;
            return this;
        }

        public AuthClientFactory WithRedirectUrl(string url)
        {
            this.redirectUrl = url;
            return this;
        }

        public IAuthClient Create()
        {
#if UNITY_ANDROID
            return new Android.AuthClient
            {
                VaultPrefix = this.vaultPrefix
            };
#else
            return new Desktop.AuthClient
            {
                VaultPrefix = this.vaultPrefix,
                RedirectUrl = this.redirectUrl
            };
#endif
        }
    }
}
