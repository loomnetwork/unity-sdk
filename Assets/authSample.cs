using UnityEngine;
using UnityEngine.UI;
using Loom.Unity3d;
using Newtonsoft.Json;

public class authSample : MonoBehaviour
{
    public Text statusTextRef;

    private Identity identity;
    private Address callerAddr;
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
        this.chainClient = new DAppChainClient("http://localhost", 46657, 47000)
        {
            Logger = Debug.unityLogger
        };
        this.chainClient.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]{
            new NonceTxMiddleware{
                PublicKey = this.identity.PublicKey,
                Client = this.chainClient
            },
            new SignedTxMiddleware(this.identity.PrivateKey)
        });
        this.callerAddr = this.identity.ToAddress("default");
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
            Val = "Hello World"
        };
        var contract = new Address
        {
            ChainId = "helloworld",
            Local = Google.Protobuf.ByteString.CopyFrom(new byte[20])
        };
        var result = await this.chainClient.CallAsync(this.callerAddr, contract, "whatever", tx);
        this.statusTextRef.text = "Committed Tx to Block " + result.Height;
    }

    // NOTE: The structure of the query params is defined by the contract author,
    // the only constraint is that it must be serializable to JSON.
    private class QueryParams
    {
        [JsonProperty("body")]
        public string Body { get; set; }
    }

    // NOTE: The structure of the query result is defined by the contract author,
    // the only constraint is that it must be deserializable from JSON.
    private class QueryResult
    {
        [JsonProperty("body")]
        public string Body { get; set; }
    }

    public async void Query()
    {
        // TODO: Get the contract address from somewhere.
        var contract = new Address();
        // TODO: Specify a valid method.
        var method = "whatever";
        // NOTE: Query results can be of any type that can be deserialized via Newtonsoft.Json.
        var result = await this.chainClient.QueryAsync<QueryResult>(contract, method, new QueryParams{ Body = "hello" });
        this.statusTextRef.text = "Query Response: " + result.Body;
    }
}
