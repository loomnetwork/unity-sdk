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
using Loom.Nethereum.ABI.FunctionEncoding.Attributes;
using Loom.Nethereum.Contracts;
using Loom.Nethereum.RPC.Eth.DTOs;
using Loom.Newtonsoft.Json;

namespace Loom.Client.Tests
{
    public class EvmTests
    {
        private string testsAbi;
        private EvmContract contract;
        private readonly byte[] bytes4 = { 1, 2, 3, 4 };

        [SetUp]
        public void SetUp() {
            this.testsAbi = Resources.Load<TextAsset>("Tests.abi").text;
        }

        public class TestIndexedEvent1
        {
            [Parameter("uint", "number1", 1, true)]
            public uint Number1 {get; set; }
        }

        [UnityTest]
        public IEnumerator LogsGetAllChangesTest() {
            return ContractTest(async () =>
            {
                await this.contract.CallAsync("emitTestIndexedEvent1", 1);
                await this.contract.CallAsync("emitTestIndexedEvent1", 2);
                await this.contract.CallAsync("emitTestIndexedEvent1", 3);
                EvmEvent<TestIndexedEvent1> event1 = this.contract.GetEvent<TestIndexedEvent1>("TestIndexedEvent1");
                List<EventLog<TestIndexedEvent1>> decodedEvents = await event1.GetAllChanges(event1.CreateFilterInput(BlockParameter.CreateEarliest(), BlockParameter.CreatePending()));

                Assert.AreEqual(1, decodedEvents[decodedEvents.Count - 3].Event.Number1);
                Assert.AreEqual(2, decodedEvents[decodedEvents.Count - 2].Event.Number1);
                Assert.AreEqual(3, decodedEvents[decodedEvents.Count - 1].Event.Number1);
            });
        }

        [UnityTest]
        public IEnumerator LogsGetAllChangesFilteredTest() {
            return ContractTest(async () =>
            {
                await this.contract.CallAsync("emitTestIndexedEvent1", 1);
                await this.contract.CallAsync("emitTestIndexedEvent1", 2);
                await this.contract.CallAsync("emitTestIndexedEvent1", 3);
                EvmEvent<TestIndexedEvent1> event1 = this.contract.GetEvent<TestIndexedEvent1>("TestIndexedEvent1");
                List<EventLog<TestIndexedEvent1>> decodedEvents =
                    await event1.GetAllChanges(event1.CreateFilterInput(new object[] { 2 }, BlockParameter.CreateEarliest(), BlockParameter.CreatePending()));

                Assert.NotZero(decodedEvents.Count);
                decodedEvents.ForEach(log => Assert.AreEqual(2, log.Event.Number1));

                Debug.Log(JsonConvert.SerializeObject(decodedEvents, Formatting.Indented));
            });
        }

        [UnityTest]
        public IEnumerator GetBlockHeightTest() {
            return ContractTest(async () =>
            {
                BigInteger height1 = await this.contract.GetBlockHeight();
                await this.contract.CallAsync("setTestUint", new BigInteger(123456789));
                BigInteger height2 = await this.contract.GetBlockHeight();

                Assert.AreEqual(height1 + 1, height2);
            });
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
        public IEnumerator EventsAreSequentialTest() {
            return ContractTest(async () =>
            {
                const int eventCount = 15;
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

                    if (testEventArguments.Count == eventCount)
                    {
                        waitForEvents.Set();
                    }
                };
                this.contract.EventReceived += handler;
                await this.contract.CallAsync("emitTestEvents", 0);
                waitForEvents.WaitOne(5000);
                this.contract.EventReceived -= handler;
                Debug.Log(String.Join(", ", testEventArguments.ToArray()));
                Assert.AreEqual(eventCount, testEventArguments.Count);
                Assert.AreEqual(testEventArguments.OrderBy(i => i).ToList(), testEventArguments);
            });
        }

        private IEnumerator ContractTest(Func<Task> action)
        {
            return AsyncEditorTestUtility.AsyncTest(action, EnsureContract);
        }

        private async Task EnsureContract() {
            if (this.contract != null)
                return;

            byte[] privateKey = CryptoUtils.GeneratePrivateKey();
            byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
            this.contract = await ContractTestUtility.GetEvmContract(privateKey, publicKey, this.testsAbi);
        }
    }
}
