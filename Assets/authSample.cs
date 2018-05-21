using UnityEngine;
using UnityEngine.UI;
using Loom.Unity3d;
using Loom.Unity3d.Samples;
using System;
using System.Threading.Tasks;

public class authSample : MonoBehaviour
{
    public Text statusTextRef;
    public GameObject cube;
    public Vector3 spinDirection;

    private Identity identity;
    private Contract contract;

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
        if (this.cube)
        {
            this.cube.transform.Rotate(this.spinDirection * Time.deltaTime);
        }
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
                .WithRedirectUrl("http://127.0.0.1:9998/auth/auth0/")
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

        var writer = RPCClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            .WithHTTP("http://127.0.0.1:46658/rpc")
            //.WithWebSocket("ws://127.0.0.1:46657/websocket")
            .Create();

        var reader = RPCClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            .WithHTTP("http://127.0.0.1:46658/query")
            //.WithWebSocket("ws://127.0.0.1:47000/queryws")
            .Create();

        var client = new DAppChainClient(writer, reader)
        {
            Logger = Debug.unityLogger
        };
        client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]{
            new NonceTxMiddleware{
                PublicKey = this.identity.PublicKey,
                Client = client
            },
            new SignedTxMiddleware(this.identity.PrivateKey)
        });

        var contractAddr = await client.ResolveContractAddressAsync("BluePrint");
        var callerAddr = this.identity.ToAddress("default");
        this.contract = new Contract(client, contractAddr, callerAddr);
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

    public async void CallContract()
    {
        if (this.identity == null)
        {
            throw new Exception("Not signed in!");
        }

        this.statusTextRef.text = "Calling smart contract...";

        await this.contract.CallAsync("SetMsg", new MapEntry
        {
            Key = "123",
            Value = "hello!"
        });

        this.statusTextRef.text = "Smart contract method finished executing.";
    }

    public async void CallContractWithResult()
    {
        if (this.identity == null)
        {
            throw new Exception("Not signed in!");
        }

        this.statusTextRef.text = "Calling smart contract...";

        var result = await this.contract.CallAsync<MapEntry>("SetMsgEcho", new MapEntry
        {
            Key = "321",
            Value = "456"
        });

        if (result != null)
        {
            this.statusTextRef.text = "Smart contract returned: " + result.ToString();
        }
        else
        {
            this.statusTextRef.text = "Smart contract didn't return anything!";
        }
    }

    public async void StaticCallContract()
    {
        this.statusTextRef.text = "Calling smart contract...";

        var result = await this.contract.StaticCallAsync<MapEntry>("GetMsg", new MapEntry
        {
            Key = "123"
        });

        this.statusTextRef.text = "Smart contract returned: " + result.ToString();
    }
}
