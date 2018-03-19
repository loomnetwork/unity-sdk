using UnityEngine;
using UnityEngine.UI;
using Loom.Unity3d;

public class authSample : MonoBehaviour
{
    public Text statusTextRef;

    private Identity identity;

    // Use this for initialization
    void Start()
    {
        // By default the editor won't respond to network IO or anything if it doesn't have input focus,
        // which is super annoying when input focus is given to the web browser for the Auth0 sign-in.
        Application.runInBackground = true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public async void SignIn()
    {
        try
        {
            CertValidationBypass.Enable();
            var authClient = AuthClientFactory.Configure()
                .WithLogger(Debug.unityLogger)
                .WithClientId("25pDQvX4O5j7wgwT052Sh3UzXVR9X6Ud") // unity3d sdk
                .WithDomain("loomx.auth0.com")
                .WithScheme("io.loomx.unity3d")
                .WithAudience("https://keystore.loomx.io/")
                .WithScope("openid profile email picture")
                .WithRedirectUrl("http://127.0.0.1:9999/auth/auth0/")
                .Create();
            var accessToken = await authClient.GetAccessTokenAsync();
            var keyStore = await KeyStoreFactory.CreateVaultStore(new VaultStoreConfig
            {
                Url = "https://stage-vault.delegatecall.com/v1/",
                VaultPrefix = "unity3d-sdk",
                AccessToken = accessToken
            });
            this.identity = await authClient.GetIdentityAsync(accessToken, keyStore);
        }
        finally
        {
            CertValidationBypass.Disable();
        }
        this.statusTextRef.text = "Signed in as " + this.identity.Username;
    }

    public async void SendTx()
    {
        if (this.identity == null)
        {
            throw new System.Exception("Not signed in!");
        }
        var chainClient = new DAppChainClient("http://stage-rancher.loomapps.io:46657")
        {
            Logger = Debug.unityLogger
        };
        var tx = new DummyTx
        {
            Val = "Hello World " + (Random.value * 100000)
        };
        Debug.Log("Tx Val: " + tx.Val);
        var signedTx = chainClient.SignTx(tx, this.identity.PrivateKey);
        var result = await chainClient.CommitTx(signedTx);
        this.statusTextRef.text = "Committed Tx to Block " + result.Height;
    }
}
