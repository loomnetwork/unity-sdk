using System;
using System.Threading.Tasks;

namespace Loom.Unity3d
{
    public class VaultStore : IKeyStore
    {
        private VaultClient client;
        private string prefix;

        public VaultStore(VaultClient client, string prefix = "")
        {
            this.client = client;
            this.prefix = "entcubbyhole/" + prefix;
        }

        public async Task SetAsync(string key, byte[] privateKey)
        {
            var data = new VaultStorePrivateKeyRequest
            {
                PrivateKey = Convert.ToBase64String(privateKey)
            };
            await this.client.PutAsync(this.prefix + key, data);
        }

        public async Task<byte[]> GetPrivateKeyAsync(string key)
        {
            var resp = await this.client.GetAsync<VaultGetPrivateKeyResponse>(this.prefix + key);
            return Convert.FromBase64String(resp.Data.PrivateKey);
        }

        public async Task<string[]> GetKeysAsync()
        {
            try
            {
                var resp = await this.client.ListAsync(this.prefix);
                if (resp != null)
                {
                    return resp.Data.Keys;
                }
            }
            catch (VaultError e)
            {
                // allow 404 on path to pass
                if (e.Errors != null && e.Errors.Length > 0)
                {
                    throw e;
                }
            }
            return new string[] { };
        }
    }
}
