using UnityEngine;
using UnityEngine.UI;
using Loom.Unity3d;
using Loom.Unity3d.Samples;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Numerics;

public class AssetTransferGameObject : MonoBehaviour
{
    public Text statusTextRef;
    public GameObject cube;
    public UnityEngine.Vector3 spinDirection;

    private Address gameContractAddr;
    private Address coinContractAddr;
    private DAppChainClient client;
    private Address callerAddr;
    private string owner;
    
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

    public async void Connect()
    {
        var writer = RPCClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            .WithHTTP("http://127.0.0.1:46658/rpc")
            .Create();

        var reader = RPCClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            //.WithHTTP("http://127.0.0.1:46658/query")
            .WithWebSocket("ws://127.0.0.1:9999/queryws")
            .Create();

        this.client = new DAppChainClient(writer, reader)
        {
            Logger = Debug.unityLogger
        };

        this.gameContractAddr = await this.client.ResolveContractAddressAsync("etherboycore");
        this.coinContractAddr = await this.client.ResolveContractAddressAsync("coin");
    }

    /// <summary>
    /// Generates a new player identity.
    /// </summary>
    public async void GenerateNewIdentity()
    {
        var privateKey = CryptoUtils.GeneratePrivateKey();
        var publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
        this.callerAddr = Address.FromPublicKey(publicKey);
        this.owner = "Bob";

        var contract = new Contract(this.client, this.gameContractAddr, this.callerAddr);
        await contract.CallAsync("CreateAccount", new EtherboyCreateAccountTx
        {
            Owner = this.owner
        });

        this.statusTextRef.text = "Signed in as " + this.callerAddr.ToAddressString();

        this.client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]{
            new NonceTxMiddleware{
                PublicKey = publicKey,
                Client = client
            },
            new SignedTxMiddleware(privateKey)
        });
    }

    /// <summary>
    /// Awards a single DAppChain token to the player.
    /// </summary>
    public async void AwardDAppTokenToPlayer()
    {
        if (this.callerAddr == null)
        {
            throw new Exception("No identity specified");
        }

        this.statusTextRef.text = "Awarding token on DAppChain...";

        var contract = new Contract(this.client, this.gameContractAddr, this.callerAddr);
        await contract.CallAsync("EndGame", new EtherboyEndGameTx
        {
            Owner = this.owner
        });

        this.statusTextRef.text = "Token awarded on DAppChain";
    }

    /// <summary>
    /// Starts the transfer to mainnet by burning the DAppChain token so that it can't be transferred again.
    /// </summary>
    public async void ApproveDAppTokenWithdraw()
    {
        if (this.callerAddr == null)
        {
            throw new Exception("No identity specified");
        }

        this.statusTextRef.text = "Approving token withdrawal on DAppChain...";

        var contract = new Contract(this.client, this.coinContractAddr, this.callerAddr);
        await contract.CallAsync("Approve", new ApproveRequest
        {
            Spender = this.gameContractAddr,
            Amount = BigInteger.One.ToBigUIntByteString()
        });
        
        this.statusTextRef.text = "Token withdrawal approved on DAppChain";
    }

    /// <summary>
    /// Generates a hash for the token transfer.
    /// </summary>
    public async void GenerateTransferHash()
    {
        if (this.callerAddr == null)
        {
            throw new Exception("No identity specified");
        }

        this.statusTextRef.text = "Generating transfer hash on DAppChain...";
        await GenerateTransferHashAsync();
        this.statusTextRef.text = "Transfer hash received from DAppChain";
    }

    public class TransferTokenEventData
    {
        public string Owner;
        public string ToChain;
        public string ToAddr;
        public byte[] Hash;
    }

    private async Task<TransferTokenEventData> GenerateTransferHashAsync()
    {
        var contract = new Contract(this.client, this.gameContractAddr, this.callerAddr);
        var tcs = new TaskCompletionSource<TransferTokenEventData>();
        EventHandler<DAppChainClient.ChainEventArgs> handler = null;
        handler = (sender, e) =>
        {
            if (e.CallerAddress.Equals(this.callerAddr))
            {
                contract.OnEvent -= handler;
                var jsonStr = System.Text.Encoding.UTF8.GetString(e.Data);
                var data = JsonConvert.DeserializeObject<TransferTokenEventData>(jsonStr);
                tcs.TrySetResult(data);
            }
        };
        contract.OnEvent += handler;
        try
        {
            await contract.CallAsync("TransferToken", new EtherboyTransferTokenTx
            {
                Owner = this.owner,
                ToAddr = Address.FromHexString("0x0", "eth")
            });
        }
        catch (Exception e)
        {
            contract.OnEvent -= handler;
            throw e;
        }
        return await tcs.Task;
    }

    private class SampleToken
    {
        public string to;
        public string hash;
    }

    /// <summary>
    /// Finishes the transfer to mainnet by sending a tx containing the transfer hash
    /// via MetaMask (WebGL) or TrustWallet (iOS/Android).
    /// </summary>
    public async void SendTransferHashToEthereum()
    {
        if (this.callerAddr == null)
        {
            throw new Exception("No identity specified");
        }

        this.statusTextRef.text = "Sending transfer hash to Ethereum...";

        // TODO: put TransferTokenEventData in here
        var result = await AssetTransfer.TransferAsset(new SampleToken
        {
            to = "0x1234",
            hash = "0x4321"
        });

        Debug.Log("Asset transfer result: " + result.ToString());

        this.statusTextRef.text = "Asset transfer complete";
    }
}
