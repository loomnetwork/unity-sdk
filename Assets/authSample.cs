using UnityEngine;
using UnityEngine.UI;
using Loom.Unity3d;
using Newtonsoft.Json;
using Google.Protobuf;
using System;
using System.Threading.Tasks;

public class authSample : MonoBehaviour
{
    public Text statusTextRef;

    private Address contractAddr;
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

    private IAuthClient CreateAuthClient()
    {
#if !UNITY_WEBGL
        try
        {
            CertValidationBypass.Enable();
            return AuthClientFactory.Configure()
                .WithLogger(Debug.unityLogger)
                .WithClientId("25pDQvX4O5j7wgwT052Sh3UzXVR9X6Ud") // unity3d sdk
                .WithDomain("loomx.auth0.com")
                .WithScheme("io.loomx.unity3d")
                .WithAudience("https://keystore.loomx.io/")
                .WithScope("openid profile email picture")
                .WithRedirectUrl("http://127.0.0.1:9999/auth/auth0/")
                .Create();
        }
        finally
        {
            CertValidationBypass.Disable();
        }
#else
        return AuthClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            .WithHostPageHandlers(new Loom.Unity3d.WebGL.HostPageHandlers
            {
                SignIn = "authenticateFromGame",
                GetUserInfo = "getUserInfo",
                SignOut = "clearUserInfo"
            })
            .Create();
#endif
    }

#if !UNITY_WEBGL // In WebGL all interactions with the key store should be done in the host page.
    private async Task<IKeyStore> CreateKeyStore(string accessToken)
    {
        return await KeyStoreFactory.CreateVaultStore(new VaultStoreConfig
        {
            Url = "https://stage-vault.delegatecall.com/v1/",
            VaultPrefix = "unity3d-sdk",
            AccessToken = accessToken
        });
    }
#endif

    public async void SignIn()
    {
#if !UNITY_WEBGL
        try
        {
            CertValidationBypass.Enable();
            var authClient = this.CreateAuthClient();
            var accessToken = await authClient.GetAccessTokenAsync();
            var keyStore = await this.CreateKeyStore(accessToken);
            this.identity = await authClient.GetIdentityAsync(accessToken, keyStore);
        }
        finally
        {
            CertValidationBypass.Disable();
        }
#else
        var authClient = this.CreateAuthClient();
        this.identity = await authClient.GetIdentityAsync("", null);
#endif
        this.statusTextRef.text = "Signed in as " + this.identity.Username;
        // This DAppChain client will connect to the example REST server in the Loom Go SDK. 
        this.chainClient = new DAppChainClient("http://localhost:46657", "http://localhost:47000")
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

        // There is only one contract address at the moment...
        this.contractAddr = new Address
        {
            ChainId = "default",
            Local = ByteString.CopyFrom(CryptoUtils.HexStringToBytes("0x005B17864f3adbF53b1384F2E6f2120c6652F779"))
        };
        this.callerAddr = this.identity.ToAddress("default");
    }

    public async void SignOut()
    {
        var authClient = this.CreateAuthClient();
        await authClient.ClearIdentityAsync();
    }

    public async void ResetPrivateKey()
    {
#if !UNITY_WEBGL
        try
        {
            CertValidationBypass.Enable();
            var authClient = this.CreateAuthClient();
            var accessToken = await authClient.GetAccessTokenAsync();
            var keyStore = await this.CreateKeyStore(accessToken);
            this.identity = await authClient.CreateIdentityAsync(accessToken, keyStore);
        }
        finally
        {
            CertValidationBypass.Disable();
        }
#else
        // TODO
        throw new NotImplementedException();
#endif
    }
    public async void SendTx()
    {
        if (this.identity == null)
        {
            throw new System.Exception("Not signed in!");
        }

        float r = (UnityEngine.Random.value * 100000);
        var tx = new DummyTx
        {
            Key = r.ToString(),
            Val = "Hello World"
        };
        var contract = new Address
        {
            ChainId = "helloworld",
            Local = ByteString.CopyFrom(new byte[20])
        };
        var result = await this.chainClient.CallAsync(this.callerAddr, contract, "whatever", tx);
        this.statusTextRef.text = "Committed Tx to Block " + result.Height;
    }

    public async void Query()
    {
        // TODO: Specify a valid method.
        var method = "mycontract.DoSomething";
        var result = await this.chainClient.QueryAsync<SampleQueryResult>(this.contractAddr, method, new SampleQueryParams{ Body = "hello" });
        this.statusTextRef.text = "Query Response: " + result.Body;
    }
}
