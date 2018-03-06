using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class VaultStore
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
        var data = await this.client.GetAsync<VaultGetPrivateKeyResponse>(this.prefix + key);
        return Convert.FromBase64String(data.PrivateKey);
    }

    public async Task<string[]> GetKeysAsync()
    {
        try
        {
            var keyList = await this.client.ListAsync(this.prefix);
            return keyList.Keys;
        }
        catch (VaultError e)
        {
            // allow 404 on path to pass
            if (e.Errors != null) {
                throw e;
            }
            return new string[] { };
        }
    }
}