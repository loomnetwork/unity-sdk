using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Loom.Unity3d.Tests
{
    public class EvmTests
    {
        private string testsAbi;
        private EvmContract contract;
        byte[] bytes4 = { 1, 2, 3, 4 };

        [SetUp]
        public void SetUp() {
            testsAbi = Resources.Load<TextAsset>("Tests.abi").text;
        }

        [UnityTest]
        public IEnumerator AddressTest() {
            return ContractTest(async () =>
            {
                string testAddress = "0x1d655354f10499ef1e32e5a4e8b712606af33628";

                await contract.CallAsync("setTestAddress", testAddress);
                Assert.AreEqual(testAddress, await contract.StaticCallSimpleTypeOutputAsync<string>("getTestAddress"));
                Assert.AreEqual(testAddress, await  contract.StaticCallSimpleTypeOutputAsync<string>("getStaticTestAddress"));
            });
        }

        [UnityTest]
        public IEnumerator UintTest() {
            return ContractTest(async () =>
            {
                await contract.CallAsync("setTestUint", new BigInteger(123456789));
                Assert.AreEqual(new BigInteger(123456789), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getTestUint"));
                Assert.AreEqual(new BigInteger(0xDEADBEEF), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestUint"));
            });
        }

        [UnityTest]
        public IEnumerator IntTest() {
            return ContractTest(async () =>
            {
                await contract.CallAsync("setTestInt", new BigInteger(-123456789));
                Assert.AreEqual(new BigInteger(-123456789), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getTestInt"));
                Assert.AreEqual(new BigInteger(0xDEADBEEF), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntPositive"));
                Assert.AreEqual(new BigInteger(-0xDEADBEEF), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntNegative"));
                Assert.AreEqual(new BigInteger(-1L), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus1"));
                Assert.AreEqual(new BigInteger(-255L), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus255"));
                Assert.AreEqual(new BigInteger(-256L), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus256"));
            });
        }

        [UnityTest]
        public IEnumerator ByteArrayTest() {
            return ContractTest(async () =>
            {
                await contract.CallAsync("setTestByteArray", bytes4);
                Assert.AreEqual(bytes4, await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestByteArray"));
                Assert.AreEqual(bytes4, await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestByteArray"));
            });
        }

        [UnityTest]
        public IEnumerator Fixed4ByteArrayTest() {
            return ContractTest(async () =>
            {
                Debug.Log(FormatBytes(await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed4ByteArray")));
                await contract.CallAsync("setTestFixed4ByteArray", bytes4);
                Assert.AreEqual(bytes4, await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestFixed4ByteArray"));
                Assert.AreEqual(
                    new BigInteger(0xDEADBEEF),
                    new BigInteger((await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed4ByteArray")).Reverse().Concat(new byte[] { 0 }).ToArray())
                );
            });
        }

        [UnityTest]
        public IEnumerator Fixed32ByteArrayTest() {
            return ContractTest(async () =>
            {
                Debug.Log(FormatBytes(await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed32ByteArray")));
                byte[] bytes32 = new byte[32];
                Array.Copy(bytes4, bytes32, bytes4.Length);

                await contract.CallAsync("setTestFixed32ByteArray", bytes32);
                Assert.AreEqual(bytes32, await this.contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestFixed32ByteArray"));
                Assert.AreEqual(
                    new BigInteger(0xDEADBEEF),
                    new BigInteger((await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed32ByteArray")).Reverse().ToArray())
                );
            });
        }

        private IEnumerator ContractTest(Func<Task> action) {
            return
                Task.Run(() =>
                {
                    EnsureContract().Wait();
                    try
                    {
                        action().Wait();
                    } catch (AggregateException e)
                    {
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }
                }).AsIEnumerator();
        }

        private async Task EnsureContract() {
            if (contract != null)
                return;

            var privateKey = CryptoUtils.GeneratePrivateKey();
            var publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
            contract = await GetContract(privateKey, publicKey, testsAbi);
        }

        private async Task<EvmContract> GetContract(byte[] privateKey, byte[] publicKey, string abi) {
            var writer = RpcClientFactory.Configure()
                .WithLogger(Debug.unityLogger)
                .WithWebSocket("ws://127.0.0.1:46657/websocket")
                .Create();

            var reader = RpcClientFactory.Configure()
                .WithLogger(Debug.unityLogger)
                .WithWebSocket("ws://127.0.0.1:9999/queryws")
                .Create();

            var client = new DAppChainClient(writer, reader)
                { Logger = Debug.unityLogger };

            // required middleware
            client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]
            {
                new NonceTxMiddleware(publicKey, client),
                new SignedTxMiddleware(privateKey)
            });

            var contractAddr = await client.ResolveContractAddressAsync("Tests");
            var callerAddr = Address.FromPublicKey(publicKey);

            return new EvmContract(client, contractAddr, callerAddr, abi);
        }

        private static string FormatBytes(byte[] bytes)
        {
            return "[" + string.Join(", ", bytes) + "]";
        }
    }
}
