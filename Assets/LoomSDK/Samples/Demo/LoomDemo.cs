using UnityEngine;
using UnityEngine.UI;
using Loom.Unity3d;
using Loom.Unity3d.Samples;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class LoomDemo : MonoBehaviour
{
    public Text statusTextRef;
    public GameObject cube;
    public Vector3 spinDirection;

    private Contract contract;

    public class SampleEvent
    {
        public string Method;
        public string Key;
        public string Value;
    }

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

    public async void SignIn()
    {
        var privateKey = CryptoUtils.GeneratePrivateKey();
        var publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
        var callerAddr = Address.FromPublicKey(publicKey);
        this.statusTextRef.text = "Signed in as " + callerAddr.ToAddressString();

        var writer = RPCClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            //.WithHTTP("http://127.0.0.1:46658/rpc")
            .WithWebSocket("ws://127.0.0.1:46657/websocket")
            .Create();

        var reader = RPCClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            //.WithHTTP("http://127.0.0.1:46658/query")
            .WithWebSocket("ws://127.0.0.1:9999/queryws")
            .Create();

        var client = new DAppChainClient(writer, reader)
        {
            Logger = Debug.unityLogger
        };

        client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]{
            new NonceTxMiddleware{
                PublicKey = publicKey,
                Client = client
            },
            new SignedTxMiddleware(privateKey)
        });

        var contractAddr = await client.ResolveContractAddressAsync("BluePrint");
        this.contract = new Contract(client, contractAddr, callerAddr);

        // Subscribe to DAppChainClient.OnChainEvent to receive all events
        /*
        client.OnChainEvent += (sender, e) =>
        {
            var jsonStr = System.Text.Encoding.UTF8.GetString(e.Data);
            var data = JsonConvert.DeserializeObject<SampleEvent>(jsonStr);
            Debug.Log(string.Format("Chain Event: {0}, {1}, {2} from block {3}", data.Method, data.Key, data.Value, e.BlockHeight));
        };
        */

        // Subscribe to DAppChainClient.ChainEventReceived to receive events from a specific smart contract
        this.contract.EventReceived += (sender, e) =>
        {
            var jsonStr = System.Text.Encoding.UTF8.GetString(e.Data);
            var data = JsonConvert.DeserializeObject<SampleEvent>(jsonStr);
            Debug.Log(string.Format("Contract Event: {0}, {1}, {2} from block {3}", data.Method, data.Key, data.Value, e.BlockHeight));
        };
    }

    public async void CallContract()
    {
        if (this.contract == null)
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
        if (this.contract == null)
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
        if (this.contract == null)
        {
            throw new Exception("Not signed in!");
        }

        this.statusTextRef.text = "Calling smart contract...";

        var result = await this.contract.StaticCallAsync<MapEntry>("GetMsg", new MapEntry
        {
            Key = "123"
        });

        this.statusTextRef.text = "Smart contract returned: " + result.ToString();
    }

    private class SampleAsset
    {
        public string to;
        public string hash;
    }

    public async void TransferAsset()
    {
        this.statusTextRef.text = "Transfering asset...";

        var result = await AssetTransfer.TransferAsset(new SampleAsset
        {
            to = "0x1234",
            hash = "0x4321"
        });

        Debug.Log("Asset transfer result: " + result.ToString());

        this.statusTextRef.text = "Asset transfer complete.";
    }
}
