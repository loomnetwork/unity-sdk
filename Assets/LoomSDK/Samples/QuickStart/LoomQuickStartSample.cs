using System;
using System.Threading.Tasks;
using UnityEngine;
using Loom.Unity3d;
using Loom.Unity3d.Samples;

public class LoomQuickStartSample : MonoBehaviour {

    async Task<Contract> GetContract(byte[] privateKey, byte[] publicKey)
    {
        var writer = RpcClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            .WithHTTP("http://127.0.0.1:46658/rpc")
            //.WithWebSocket("ws://127.0.0.1:46657/websocket")
            .Create();

        var reader = RpcClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            .WithHTTP("http://127.0.0.1:46658/query")
            //.WithWebSocket("ws://127.0.0.1:9999/queryws")
            .Create();

        var client = new DAppChainClient(writer, reader)
        {
            Logger = Debug.unityLogger
        };
        // required middleware
        client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]{
            new NonceTxMiddleware(publicKey, client),
            new SignedTxMiddleware(privateKey)
        });

        var contractAddr = await client.ResolveContractAddressAsync("BluePrint");
        var callerAddr = Address.FromPublicKey(publicKey);
        return new Contract(client, contractAddr, callerAddr);
    }

    async Task CallContract(Contract contract)
    {
        await contract.CallAsync("SetMsg", new MapEntry
        {
            Key = "123",
            Value = "hello!"
        });
    }

    async Task CallContractWithResult(Contract contract)
    {
        var result = await contract.CallAsync<MapEntry>("SetMsgEcho", new MapEntry
        {
            Key = "321",
            Value = "456"
        });

        if (result != null)
        {
            // This should print: { "key": "321", "value": "456" } in the Unity console window.
            Debug.Log("Smart contract returned: " + result.ToString());
        }
        else
        {
            throw new Exception("Smart contract didn't return anything!");
        }
    }

    async Task StaticCallContract(Contract contract)
    {
        var result = await contract.StaticCallAsync<MapEntry>("GetMsg", new MapEntry
        {
            Key = "123"
        });

        if (result != null)
        {
            // This should print: { "key": "123", "value": "hello!" } in the Unity console window
            // provided `LoomQuickStartSample.CallContract()` was called first.
            Debug.Log("Smart contract returned: " + result.ToString());
        }
        else
        {
            throw new Exception("Smart contract didn't return anything!");
        }
    }

    // Use this for initialization
    async void Start () {
        // The private key is used to sign transactions sent to the DAppChain.
        // Usually you'd generate one private key per player, or let them provide their own.
        // In this sample we just generate a new key every time.
        var privateKey = CryptoUtils.GeneratePrivateKey();
        var publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);

        var contract = await GetContract(privateKey, publicKey);
        await CallContract(contract);
        // This should print: { "key": "123", "value": "hello!" } in the Unity console window
        await StaticCallContract(contract);
        // This should print: { "key": "321", "value": "456" } in the Unity console window
        await CallContractWithResult(contract);
    }
}
