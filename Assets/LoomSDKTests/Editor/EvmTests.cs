using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Org.BouncyCastle.Math;

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

                contract.CallAsync("setTestAddress", testAddress).Wait();
                Assert.AreEqual(testAddress, await contract.StaticCallSimpleTypeOutputAsync<string>("getTestAddress"));
                Assert.AreEqual(testAddress, await  contract.StaticCallSimpleTypeOutputAsync<string>("getStaticTestAddress"));
            });
        }

        [UnityTest]
        public IEnumerator UintTest() {
            return ContractTest(async () =>
            {
                contract.CallAsync("setTestUint", BigInteger.ValueOf(123456789)).Wait();
                Assert.AreEqual(BigInteger.ValueOf(123456789), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getTestUint"));
                Assert.AreEqual(BigInteger.ValueOf(0xDEADBEEF), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestUint"));
            });
        }

        [UnityTest]
        public IEnumerator IntTest() {
            return ContractTest(async () =>
            {
                contract.CallAsync("setTestInt", BigInteger.ValueOf(-123456789)).Wait();
                Assert.AreEqual(BigInteger.ValueOf(-123456789), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getTestInt"));
                Assert.AreEqual(BigInteger.ValueOf(0xDEADBEEF), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntPositive"));
                Assert.AreEqual(BigInteger.ValueOf(-0xDEADBEEF), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntNegative"));
                Assert.AreEqual(BigInteger.ValueOf(-1L), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus1"));
                Assert.AreEqual(BigInteger.ValueOf(-255L), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus255"));
                Assert.AreEqual(BigInteger.ValueOf(-256L), await contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus256"));
            });
        }

        [UnityTest]
        public IEnumerator ByteArrayTest() {
            return ContractTest(async () =>
            {
                await contract.CallAsync("setTestByteArray", bytes4);
                Assert.IsTrue(bytes4.SequenceEqual(await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestByteArray")));
                Assert.IsTrue(bytes4.SequenceEqual(await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestByteArray")));
            });
        }

        [UnityTest]
        public IEnumerator Fixed4ByteArrayTest() {
            return ContractTest(async () =>
            {
                await contract.CallAsync("setTestFixed4ByteArray", bytes4);
                Assert.IsTrue(bytes4.SequenceEqual(await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestFixed4ByteArray")));
                Assert.AreEqual(BigInteger.ValueOf(0xDEADBEEF).ToByteArrayUnsigned(), await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed4ByteArray"));
            });
        }

        [UnityTest]
        public IEnumerator Fixed32ByteArrayTest() {
            return ContractTest(async () =>
            {
                byte[] bytes32 = new byte[32];
                Array.Copy(bytes4, bytes32, bytes4.Length);

                await contract.CallAsync("setTestFixed32ByteArray", bytes32);
                Assert.IsTrue(bytes32.SequenceEqual(await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestFixed32ByteArray")));
                Assert.AreEqual(0xDEADBEEF, new BigInteger(await contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed32ByteArray")).LongValue);
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
    }
}
