using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Loom.Nethereum.ABI;
using Loom.Nethereum.ABI.Decoders;

namespace Loom.Client.Tests
{
    public class EvmTests
    {
        private string testsAbi;
        private EvmContract contract;
        byte[] bytes4 = { 1, 2, 3, 4 };

        [SetUp]
        public void SetUp() {
            this.testsAbi = Resources.Load<TextAsset>("Tests.abi").text;
        }

        [UnityTest]
        public IEnumerator AddressTest() {
            return ContractTest(async () =>
            {
                string testAddress = "0x1d655354f10499ef1e32e5a4e8b712606af33628";

                await this.contract.CallAsync("setTestAddress", testAddress);
                Assert.AreEqual(testAddress, await this.contract.StaticCallSimpleTypeOutputAsync<string>("getTestAddress"));
                Assert.AreEqual(testAddress, await this.contract.StaticCallSimpleTypeOutputAsync<string>("getStaticTestAddress"));
            });
        }

        [UnityTest]
        public IEnumerator UintTest() {
            return ContractTest(async () =>
            {
                await this.contract.CallAsync("setTestUint", new BigInteger(123456789));
                Assert.AreEqual(new BigInteger(123456789), await this.contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getTestUint"));
                Assert.AreEqual(new BigInteger(0xDEADBEEF), await this.contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestUint"));
            });
        }

        [UnityTest]
        public IEnumerator IntTest() {
            return ContractTest(async () =>
            {
                await this.contract.CallAsync("setTestInt", new BigInteger(-123456789));
                Assert.AreEqual(new BigInteger(-123456789), await this.contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getTestInt"));
                Assert.AreEqual(new BigInteger(0xDEADBEEF), await this.contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntPositive"));
                Assert.AreEqual(new BigInteger(-0xDEADBEEF), await this.contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntNegative"));
                Assert.AreEqual(new BigInteger(-1L), await this.contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus1"));
                Assert.AreEqual(new BigInteger(-255L), await this.contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus255"));
                Assert.AreEqual(new BigInteger(-256L), await this.contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus256"));
            });
        }

        [UnityTest]
        public IEnumerator ByteArrayTest() {
            return ContractTest(async () =>
            {
                await this.contract.CallAsync("setTestByteArray", this.bytes4);
                Assert.AreEqual(this.bytes4, await this.contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestByteArray"));
                Assert.AreEqual(this.bytes4, await this.contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestByteArray"));
            });
        }

        [UnityTest]
        public IEnumerator Fixed4ByteArrayTest() {
            return ContractTest(async () =>
            {
                await this.contract.CallAsync("setTestFixed4ByteArray", this.bytes4);
                Assert.AreEqual(this.bytes4, await this.contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestFixed4ByteArray"));
                Assert.AreEqual(
                    new BigInteger(0xDEADBEEF),
                    new BigInteger((await this.contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed4ByteArray")).Reverse().Concat(new byte[] { 0 }).ToArray())
                );
            });
        }

        [UnityTest]
        public IEnumerator Fixed32ByteArrayTest() {
            return ContractTest(async () =>
            {
                byte[] bytes32 = new byte[32];
                Array.Copy(this.bytes4, bytes32, this.bytes4.Length);

                await this.contract.CallAsync("setTestFixed32ByteArray", bytes32);
                Assert.AreEqual(bytes32, await this.contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestFixed32ByteArray"));
                Assert.AreEqual(
                    new BigInteger(0xDEADBEEF),
                    new BigInteger((await this.contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed32ByteArray")).Reverse().ToArray())
                );
            });
        }

        [UnityTest]
        public IEnumerator EventsSequentialityTest() {
            return ContractTest(async () =>
            {

                AutoResetEvent waitForEvents = new AutoResetEvent(false);
                List<int> testEventArguments = new List<int>();
                EventHandler<EvmChainEventArgs> handler = (sender, args) =>
                {
                    if (args.EventName != "TestEvent")
                    {
                        waitForEvents.Set();
                        throw new Exception("args.EventName != TestEvent");
                    }

                    int val = new IntTypeDecoder(false).DecodeInt(args.Data);
                    testEventArguments.Add(val);

                    if (testEventArguments.Count == 15)
                    {
                        waitForEvents.Set();
                    }

                    Debug.Log("TestEvent: " + val);
                };
                this.contract.EventReceived += handler;
                await this.contract.CallAsync("emitTestEvents", 0);
                waitForEvents.WaitOne(5000);
                this.contract.EventReceived -= handler;
                Debug.Log(String.Join(", ", testEventArguments.ToArray()));
                Assert.AreEqual(15, testEventArguments.Count);
                Assert.AreEqual(testEventArguments.OrderBy(i => i).ToList(), testEventArguments);
            });
        }

        private IEnumerator ContractTest(Func<Task> action) {
            return
                TaskAsIEnumerator(Task.Run(() =>
                {
                    try
                    {
                        EnsureContract().Wait();
                        action().Wait();
                    } catch (AggregateException e)
                    {
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }
                }));
        }

        private async Task EnsureContract() {
            if (this.contract != null)
                return;

            byte[] privateKey = CryptoUtils.GeneratePrivateKey();
            byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
            this.contract = await GetContract(privateKey, publicKey, this.testsAbi);
        }

        private async Task<EvmContract> GetContract(byte[] privateKey, byte[] publicKey, string abi)
        {
            ILogger logger = NullLogger.Instance;
            IRpcClient writer = RpcClientFactory.Configure()
                .WithLogger(logger)
                .WithWebSocket("ws://127.0.0.1:46657/websocket")
                .Create();

            IRpcClient reader = RpcClientFactory.Configure()
                .WithLogger(logger)
                .WithWebSocket("ws://127.0.0.1:9999/queryws")
                .Create();

            DAppChainClient client = new DAppChainClient(writer, reader)
                { Logger = logger };

            // required middleware
            client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]
            {
                new NonceTxMiddleware(publicKey, client),
                new SignedTxMiddleware(privateKey)
            });

            Address contractAddr = await client.ResolveContractAddressAsync("Tests");
            Address callerAddr = Address.FromPublicKey(publicKey);

            return new EvmContract(client, contractAddr, callerAddr, abi);
        }

        private static IEnumerator TaskAsIEnumerator(Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception;
        }
    }
}
