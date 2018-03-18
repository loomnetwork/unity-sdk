using System.Threading.Tasks;

namespace Loom.Unity3d
{
    public class VaultStoreConfig
    {
        private string vaultPrefix;

        /// <summary>
        /// Vault HTTPS endpoint (required).
        /// </summary>
        public string Url { get; set; }
        
        public string VaultPrefix
        {
            get
            {
                return this.vaultPrefix;
            }
            internal set
            {
                this.vaultPrefix = value.EndsWith("/") ? value : (value + "/");
            }
        }

        /// <summary>
        /// Access token obtained from AuthClient (required).
        /// </summary>
        public string AccessToken { get; set; }
    }

    /// <summary>
    /// Creates key stores.
    /// </summary>
    public class KeyStoreFactory
    {
        public static async Task<IKeyStore> CreateVaultStore(VaultStoreConfig cfg)
        {
            // exchange the Auth0 access token for a Vault client token
            var vaultClient = new VaultClient(cfg.Url);
            var resp = await vaultClient.PutAsync<VaultCreateTokenResponse, VaultCreateTokenRequest>("auth/auth0/create_token",
                new VaultCreateTokenRequest
                {
                    AccessToken = cfg.AccessToken
                }
            );
            vaultClient.Token = resp.Auth.ClientToken;
            return new VaultStore(vaultClient, cfg.VaultPrefix);
        }
    }
}