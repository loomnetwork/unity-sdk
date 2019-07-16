using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Loom.Nethereum.ABI.Decoders;
using Loom.Nethereum.Contracts;
using Loom.Nethereum.RPC.Eth.DTOs;
using Loom.Newtonsoft.Json;

namespace Loom.Client.Tests
{
    public class EvmTests
    {
        private readonly EvmTestContext testContext = new EvmTestContext();
        private readonly byte[] bytes4 = { 1, 2, 3, 4 };

        [SetUp]
        public void SetUp() {
            this.testContext.Setup();
        }

        [UnityTest]
        public IEnumerator LogsGetAllChangesTest() {
            return testContext.ContractTest(async () =>
            {
                BigInteger bigValue = new BigInteger(ulong.MaxValue) * ulong.MaxValue;
                BroadcastTxResult firstEvent1Result = await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 1);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 2);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 3);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", bigValue);
                EvmEvent<EvmTestContext.TestIndexedEvent1> event1 = this.testContext.Contract.GetEvent<EvmTestContext.TestIndexedEvent1>("TestIndexedEvent1");
                List<EventLog<EvmTestContext.TestIndexedEvent1>> decodedEvents1 = await event1.GetAllChanges(event1.EventAbi.CreateFilterInput(new BlockParameter(firstEvent1Result.Height), BlockParameter.CreatePending()));

                Assert.AreEqual((BigInteger) 1, decodedEvents1[0].Event.Number1);
                Assert.AreEqual((BigInteger) 2, decodedEvents1[1].Event.Number1);
                Assert.AreEqual((BigInteger) 3, decodedEvents1[2].Event.Number1);
                Assert.AreEqual(bigValue, decodedEvents1[3].Event.Number1);

                BroadcastTxResult firstEvent2Result = await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 4, 5);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 6, 7);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 8, 9);
                EvmEvent<EvmTestContext.TestIndexedEvent2> event2 = this.testContext.Contract.GetEvent<EvmTestContext.TestIndexedEvent2>("TestIndexedEvent2");
                List<EventLog<EvmTestContext.TestIndexedEvent2>> decodedEvents2 = await event2.GetAllChanges(event2.EventAbi.CreateFilterInput(new BlockParameter(firstEvent2Result.Height), BlockParameter.CreatePending()));

                Assert.AreEqual((BigInteger) 4, decodedEvents2[0].Event.Number1);
                Assert.AreEqual((BigInteger) 5, decodedEvents2[0].Event.Number2);
                Assert.AreEqual((BigInteger) 6, decodedEvents2[1].Event.Number1);
                Assert.AreEqual((BigInteger) 7, decodedEvents2[1].Event.Number2);
                Assert.AreEqual((BigInteger) 8, decodedEvents2[2].Event.Number1);
                Assert.AreEqual((BigInteger) 9, decodedEvents2[2].Event.Number2);
            }, 20000);
        }

        [UnityTest]
        public IEnumerator LogsGetAllChangesFilteredTest() {
            return testContext.ContractTest(async () =>
            {
                BroadcastTxResult firstEvent1Result = await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 1);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 2);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 3);
                EvmEvent<EvmTestContext.TestIndexedEvent1> event1 = this.testContext.Contract.GetEvent<EvmTestContext.TestIndexedEvent1>("TestIndexedEvent1");
                NewFilterInput filterInput = event1.EventAbi.CreateFilterInput(2, new BlockParameter(firstEvent1Result.Height), BlockParameter.CreatePending());
                List<EventLog<EvmTestContext.TestIndexedEvent1>> decodedEvents = await event1.GetAllChanges(filterInput);

                Assert.NotZero(decodedEvents.Count);
                decodedEvents.ForEach(log => Assert.AreEqual((BigInteger) 2, log.Event.Number1));

                Debug.Log(JsonConvert.SerializeObject(decodedEvents, Formatting.Indented));
            }, 20000);
        }

        [Test]
        public void EventFilterInputTest()
        {
            ContractBuilder contractBuilder = new ContractBuilder(this.testContext.TestsAbi, Address.FromPublicKey(CryptoUtils.PublicKeyFromPrivateKey(CryptoUtils.GeneratePrivateKey())).LocalAddress);
            EvmEvent<EvmTestContext.TestIndexedEvent1> event1 = new EvmEvent<EvmTestContext.TestIndexedEvent1>(null, contractBuilder.GetEventAbi("TestIndexedEvent1"));
            NewFilterInput filterInput = event1.EventAbi.CreateFilterInput(2, new BlockParameter(3), BlockParameter.CreatePending());
            Assert.NotNull(filterInput);
        }

        [UnityTest]
        public IEnumerator GetBlockHeightTest() {
            return testContext.ContractTest(async () =>
            {
                BigInteger height1 = await this.testContext.Contract.GetBlockHeight();
                await this.testContext.Contract.CallAsync("setTestUint", new BigInteger(123456789));
                BigInteger height2 = await this.testContext.Contract.GetBlockHeight();

                Assert.AreEqual(height1 + 1, height2);
            });
        }

        [UnityTest]
        public IEnumerator AddressTest() {
            return testContext.ContractTest(async () =>
            {
                string testAddress = "0x1d655354f10499ef1e32e5a4e8b712606af33628";

                await this.testContext.Contract.CallAsync("setTestAddress", testAddress);
                Assert.AreEqual(testAddress, await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<string>("getTestAddress"));
                Assert.AreEqual(testAddress, await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<string>("getStaticTestAddress"));
            });
        }

        [UnityTest]
        public IEnumerator UintTest() {
            return testContext.ContractTest(async () =>
            {
                await this.testContext.Contract.CallAsync("setTestUint", new BigInteger(123456789));
                Assert.AreEqual(new BigInteger(123456789), await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getTestUint"));
                Assert.AreEqual(new BigInteger(0xDEADBEEF), await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestUint"));
            });
        }

        [UnityTest]
        public IEnumerator IntTest() {
            return testContext.ContractTest(async () =>
            {
                await this.testContext.Contract.CallAsync("setTestInt", new BigInteger(-123456789));
                Assert.AreEqual(new BigInteger(-123456789), await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getTestInt"));
                Assert.AreEqual(new BigInteger(0xDEADBEEF), await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntPositive"));
                Assert.AreEqual(new BigInteger(-0xDEADBEEF), await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntNegative"));
                Assert.AreEqual(new BigInteger(-1L), await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus1"));
                Assert.AreEqual(new BigInteger(-255L), await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus255"));
                Assert.AreEqual(new BigInteger(-256L), await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<BigInteger>("getStaticTestIntMinus256"));
            });
        }

        [UnityTest]
        public IEnumerator ByteArrayTest() {
            return testContext.ContractTest(async () =>
            {
                await this.testContext.Contract.CallAsync("setTestByteArray", this.bytes4);
                Assert.AreEqual(this.bytes4, await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestByteArray"));
                Assert.AreEqual(this.bytes4, await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestByteArray"));
            });
        }

        [UnityTest]
        public IEnumerator Fixed4ByteArrayTest() {
            return testContext.ContractTest(async () =>
            {
                await this.testContext.Contract.CallAsync("setTestFixed4ByteArray", this.bytes4);
                Assert.AreEqual(this.bytes4, await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestFixed4ByteArray"));
                Assert.AreEqual(
                    new BigInteger(0xDEADBEEF),
                    new BigInteger((await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed4ByteArray")).Reverse().Concat(new byte[] { 0 }).ToArray())
                );
            });
        }

        [UnityTest]
        public IEnumerator Fixed32ByteArrayTest() {
            return testContext.ContractTest(async () =>
            {
                byte[] bytes32 = new byte[32];
                Array.Copy(this.bytes4, bytes32, this.bytes4.Length);

                await this.testContext.Contract.CallAsync("setTestFixed32ByteArray", bytes32);
                Assert.AreEqual(bytes32, await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<byte[]>("getTestFixed32ByteArray"));

                byte[] bytes = await this.testContext.Contract.StaticCallSimpleTypeOutputAsync<byte[]>("getStaticTestFixed32ByteArray");
                bytes = bytes.Take(4).Reverse().Concat(new[] { (byte) 0 }).ToArray();
                Assert.AreEqual(
                    new BigInteger(0xDEADBEEF),
                    new BigInteger(bytes)
                );
            });
        }

        [UnityTest]
        public IEnumerator EventsAreSequentialTest() {
            return testContext.ContractTest(async () =>
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
                this.testContext.Contract.EventReceived += handler;
                await this.testContext.Contract.Client.SubscribeToAllEvents();
                await this.testContext.Contract.CallAsync("emitTestEvents", 0);
                waitForEvents.WaitOne(5000);
                this.testContext.Contract.EventReceived -= handler;
                Debug.Log(String.Join(", ", testEventArguments.ToArray()));
                Assert.AreEqual(eventCount, testEventArguments.Count);
                Assert.AreEqual(testEventArguments.OrderBy(i => i).ToList(), testEventArguments);
            });
        }
    }
}
