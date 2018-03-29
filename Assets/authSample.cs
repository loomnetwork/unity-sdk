using UnityEngine;
using UnityEngine.UI;
using Loom.Unity3d;

public class authSample : MonoBehaviour
{
    public Text statusTextRef;

    private Identity identity;
    private DAppChainClient chainClient;

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

        // This DAppChain client will connect to the example REST server in the Loom Go SDK. 
        this.chainClient = new DAppChainClient("http://localhost:8998")
        {
            TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]{
                new SignedTxMiddleware(this.identity.PrivateKey)
            }),
            Logger = Debug.unityLogger
        };
    }

    public async void SendTx()
    {
        if (this.identity == null)
        {
            throw new System.Exception("Not signed in!");
        }

        float r = (Random.value * 100000);
        var tx = new DummyTx
        {
            Key = r.ToString(),
            Val = "Hello World " + r
        };
        Debug.Log("Tx Val: " + tx.Val);
        var result = await this.chainClient.CommitTx(tx);
        this.statusTextRef.text = "Committed Tx to Block " + result.Height;
    }

    public async void Query()
    {
        // NOTE: Query results can be of any type that can be deserialized via Newtonsoft.Json.
        var lastKey = await this.chainClient.QueryAsync<string>("app/last-key");
        var data = await this.chainClient.QueryAsync<DummyTx>("app/dummy/" + lastKey);
        this.statusTextRef.text = string.Format("Last Key: {0} & Value: {1}", data.Key, data.Val);
    }
}
