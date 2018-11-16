using System.Threading.Tasks;
using UnityEngine;

namespace Loom.Client.Tests
{
    public static class ContractTestUtility
    {
        public delegate ITxMiddlewareHandler[] CustomTxMiddlewareFunc(DAppChainClient client, byte[] privateKey, byte[] publicKey);

        public static async Task<EvmContract> GetEvmContract(byte[] privateKey, byte[] publicKey, string abi, CustomTxMiddlewareFunc customTxMiddlewareFunc = null)
        {
            ILogger logger = Debug.unityLogger;
            IRpcClient writer = RpcClientFactory.Configure()
                .WithLogger(logger)
                .WithWebSocket("ws://127.0.0.1:46658/websocket")
                .Create();

            IRpcClient reader = RpcClientFactory.Configure()
                .WithLogger(logger)
                .WithWebSocket("ws://127.0.0.1:46658/queryws")
                .Create();

            DAppChainClient client = new DAppChainClient(writer, reader)
                { Logger = logger };

            // required middleware
            ITxMiddlewareHandler[] txMiddlewareHandlers;
            if (customTxMiddlewareFunc != null)
            {
                txMiddlewareHandlers = customTxMiddlewareFunc(client, privateKey, publicKey);
            }
            else
            {
                txMiddlewareHandlers = new ITxMiddlewareHandler[]
                {
                    new NonceTxMiddleware(publicKey, client),
                    new SignedTxMiddleware(privateKey)
                };
            }

            client.TxMiddleware = new TxMiddleware(txMiddlewareHandlers);

            Address contractAddress = await client.ResolveContractAddressAsync("Tests");
            Address callerAddress = Address.FromPublicKey(publicKey);

            return new EvmContract(client, contractAddress, callerAddress, abi);
        }
    }
}
