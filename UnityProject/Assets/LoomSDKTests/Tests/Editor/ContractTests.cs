using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loom.Client.Tests
{
    public class ContractTests
    {
        private readonly EvmTestContext testContext = new EvmTestContext();

        [SetUp]
        public void SetUp() {
            this.testContext.Setup();
        }

        [UnityTest]
        public IEnumerator MultipleSimultaneousRpcCallsMustSucceedTest()
        {
            return this.testContext.ContractTest(async () =>
                {
                    const int callCount = 20;
                    Task[] callTasks = new Task[callCount];
                    for (int i = 0; i < callTasks.Length; i++)
                    {
                        // Any request will do. Use the nonce request, simple it is a simple one
                        callTasks[i] = new Task<ulong>(() =>
                            this.testContext.Contract.Client.GetNonceAsync(this.testContext.Contract.Caller.LocalAddress.Substring(2)).Result);
                    }

                    foreach (Task task in callTasks)
                    {
                        task.Start();
                    }

                    await Task.WhenAll(callTasks);
                });
        }

        [UnityTest]
        public IEnumerator CallsMustWaitForOtherCallToComplete()
        {
            // If multiple calls are made at the same time, only one should be actually communicating
            // with the backend at a given moment, others must wait for completion
            return this.testContext.ContractTest(async () =>
            {
                const int callCount = 10;
                List<int> callEndOrderList = new List<int>();
                Task[] callTasks = new Task[callCount];
                for (int i = 0; i < callTasks.Length; i++)
                {
                    int iCopy = i;
                    Func<Task> taskFunc = async () =>
                    {
                        Debug.Log($"Call {iCopy}: Start");
                        await this.testContext.Contract.CallAsync("setTestUint", new BigInteger(iCopy));
                        Debug.Log($"Call {iCopy}: End");
                        callEndOrderList.Add(iCopy);
                    };
                    callTasks[i] = taskFunc();
                }

                await Task.WhenAll(callTasks);

                Assert.AreEqual(callCount, callEndOrderList.Count);
                Assert.AreEqual(callEndOrderList.OrderBy(i => i).ToList(), callEndOrderList);
            }, timeout: 25000);
        }

        [UnityTest]
        public IEnumerator InvalidTxNonceExceptionTest()
        {
            return AsyncEditorTestUtility.AsyncTest(async () =>
            {
                byte[] privateKey = CryptoUtils.GeneratePrivateKey();
                byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
                EvmContract invalidNonceContract =
                    await ContractTestUtility.GetEvmContract(
                        privateKey,
                        publicKey,
                        this.testContext.TestsAbi,
                        (client, _, __) => new ITxMiddlewareHandler[]
                        {
                            new ControllableNonceTxMiddleware(publicKey, client)
                            {
                                ForcedNextNonce = UInt64.MaxValue
                            },
                            new SignedTxMiddleware(privateKey)
                        });

                try
                {
                    await invalidNonceContract.CallAsync("setTestUint", new BigInteger(123456789));
                } catch (InvalidTxNonceException e)
                {
                    Assert.Catch<InvalidTxNonceException>(() =>
                    {
                        throw e;
                    });
                }
            }, timeout: 35000);
        }

        [UnityTest]
        public IEnumerator SpeculativeNextNonceTest()
        {
            return AsyncEditorTestUtility.AsyncTest(async () =>
            {
                byte[] privateKey = CryptoUtils.GeneratePrivateKey();
                byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);

                int callCounter = 0;

                EvmContract invalidNonceContract =
                    await ContractTestUtility.GetEvmContract(
                        privateKey,
                        publicKey,
                        this.testContext.TestsAbi,
                        (client, _, __) =>
                        {
                            ControllableNonceTxMiddleware controllableNonceTxMiddleware = new ControllableNonceTxMiddleware(publicKey, client);
                            controllableNonceTxMiddleware.GetNextNonceAsyncCalled += nextNonce =>
                            {
                                switch (callCounter)
                                {
                                    case 0:
                                        Assert.AreEqual(1, nextNonce);
                                        controllableNonceTxMiddleware.FailOnGetNonceFromNode = true;
                                        break;
                                    case 1:
                                        Assert.AreEqual(2, nextNonce);
                                        break;
                                    case 2:
                                        Assert.AreEqual(3, nextNonce);
                                        controllableNonceTxMiddleware.FailOnGetNonceFromNode = false;
                                        controllableNonceTxMiddleware.ForcedNextNonce = UInt64.MaxValue;
                                        break;
                                    case 3:
                                        Assert.AreEqual(UInt64.MaxValue, nextNonce);
                                        break;
                                    case 4:
                                        Assert.AreEqual(UInt64.MaxValue, nextNonce);
                                        break;
                                    case 5:
                                        Assert.AreEqual(UInt64.MaxValue, nextNonce);
                                        controllableNonceTxMiddleware.ForcedNextNonce = null;
                                        break;
                                    case 6:
                                        Assert.AreEqual(4, nextNonce);
                                        break;
                                    case 7:
                                        Assert.AreEqual(5, nextNonce);
                                        break;
                                    default:
                                        Assert.Fail($"Unexpected call #{callCounter}");
                                        break;
                                }
                                callCounter++;
                            };
                            return new ITxMiddlewareHandler[]
                            {
                                controllableNonceTxMiddleware,
                                new SignedTxMiddleware(privateKey)
                            };
                        });

                try
                {
                    await invalidNonceContract.CallAsync("setTestUint", new BigInteger(123456789));
                    await invalidNonceContract.CallAsync("setTestUint", new BigInteger(123456789));
                    await invalidNonceContract.CallAsync("setTestUint", new BigInteger(123456789));
                    await invalidNonceContract.CallAsync("setTestUint", new BigInteger(123456789));
                    await invalidNonceContract.CallAsync("setTestUint", new BigInteger(123456789));

                    Assert.AreEqual(8, callCounter);
                } catch (InvalidTxNonceException e)
                {
                    Assert.Catch<InvalidTxNonceException>(() =>
                    {
                        throw e;
                    });
                }
            }, timeout: 20000);
        }

        [UnityTest]
        public IEnumerator TxAlreadyExistsInCacheRecoverTest()
        {
            return AsyncEditorTestUtility.AsyncTest(async () =>
            {
                byte[] privateKey = CryptoUtils.GeneratePrivateKey();
                byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);

                int callCounter = 0;

                EvmContract invalidNonceContract =
                    await ContractTestUtility.GetEvmContract(
                        privateKey,
                        publicKey,
                        this.testContext.TestsAbi,
                        (client, _, __) =>
                        {
                            ControllableNonceTxMiddleware controllableNonceTxMiddleware = new ControllableNonceTxMiddleware(publicKey, client);
                            controllableNonceTxMiddleware.GetNextNonceAsyncCalled += nextNonce =>
                            {
                                switch (callCounter)
                                {
                                    case 0:
                                        Assert.AreEqual(1, nextNonce);
                                        controllableNonceTxMiddleware.ForcedNextNonce = 1;
                                        break;
                                    case 1:
                                        Assert.AreEqual(1, nextNonce);
                                        controllableNonceTxMiddleware.ForcedNextNonce = null;
                                        break;
                                    case 2:
                                        Assert.AreEqual(2, nextNonce);
                                        break;
                                    default:
                                        Assert.Fail($"Unexpected call #{callCounter}");
                                        break;
                                }
                                callCounter++;
                            };
                            return new ITxMiddlewareHandler[]
                            {
                                controllableNonceTxMiddleware,
                                new SignedTxMiddleware(privateKey)
                            };
                        });
                await invalidNonceContract.CallAsync("setTestUint", new BigInteger(123456789));
                await invalidNonceContract.CallAsync("setTestUint", new BigInteger(123456789));
            }, timeout: 20000);
        }

        [UnityTest]
        public IEnumerator EventSubscriptionTest()
        {
            return this.testContext.ContractTest(async () =>
            {
                List<string> receivedEvents = new List<string>();
                EventHandler<EvmChainEventArgs> onEventReceived = (sender, args) =>
                {
                    Debug.Log("received: " + args.EventName);
                    receivedEvents.Add(args.EventName);
                };

                this.testContext.Contract.EventReceived += onEventReceived;

                // Subscribe to all events
                await this.testContext.Contract.Client.SubscribeToAllEvents();

                await this.testContext.Contract.CallAsync("emitTestEvent", 1);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 2);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 3, 4);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent3", 5, 6);

                bool timedOut = await AsyncEditorTestUtility.WaitWithTimeout(5000, () => receivedEvents.Count == 4);
                Assert.False(timedOut, "timed out");
                Assert.That(receivedEvents, Is.EquivalentTo(new[]
                {
                    "TestEvent",
                    "TestIndexedEvent1",
                    "TestIndexedEvent2",
                    "TestIndexedEvent3",
                }));
                Assert.AreEqual(1, this.testContext.Contract.Client.SubscribedTopics.Count);
                Assert.AreEqual(DAppChainClient.GlobalContractEventTopicName, this.testContext.Contract.Client.SubscribedTopics.First());

                receivedEvents.Clear();

                // Unsubscribe, shouldn't receive any event when unsubscribed
                await this.testContext.Contract.Client.UnsubscribeFromAllEvents();
                Assert.AreEqual(0, this.testContext.Contract.Client.SubscribedTopics.Count);

                await this.testContext.Contract.CallAsync("emitTestEvent", 1);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 2);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 3, 4);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent3", 5, 6);

                await Task.Delay(5000);
                Assert.AreEqual(0, receivedEvents.Count, "shouldn't receive any events");

                receivedEvents.Clear();

                // FIXME: check after EVM event filter are implemented
                // Subscribe only to TestIndexedEvent2
                /*await this.testContext.Contract.Client.SubscribeToEvents(new[] { "TestIndexedEvent2" });

                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 3, 4);
                await this.testContext.Contract.CallAsync("emitTestEvent", 1);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 2);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 3, 4);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent3", 5, 6);

                await Task.Delay(5000);
                Assert.That(receivedEvents, Is.EquivalentTo(new[]
                {
                    "TestIndexedEvent2",
                    "TestIndexedEvent2"
                }));
                Assert.AreEqual(1, this.testContext.Contract.Client.SubscribedTopics.Count);
                Assert.AreEqual(DAppChainClient.GlobalContractEventTopicName, this.testContext.Contract.Client.SubscribedTopics.First());

                receivedEvents.Clear();

                // Add TestIndexedEvent3 to subscriptions
                await this.testContext.Contract.Client.SubscribeToEvents(new[] { "TestIndexedEvent3" });

                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 3, 4);
                await this.testContext.Contract.CallAsync("emitTestEvent", 1);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent3", 5, 6);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 2);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 3, 4);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent3", 5, 6);

                await Task.Delay(5000);
                Assert.That(receivedEvents, Is.EquivalentTo(new[]
                {
                    "TestIndexedEvent2",
                    "TestIndexedEvent3",
                    "TestIndexedEvent2",
                    "TestIndexedEvent3"
                }));
                Assert.AreEqual(2, this.testContext.Contract.Client.SubscribedTopics.Count);
                Assert.That(this.testContext.Contract.Client.SubscribedTopics.OrderBy(s => s).ToArray(), Is.EquivalentTo(new []
                {
                    "TestIndexedEvent2",
                    "TestIndexedEvent3",
                }));

                receivedEvents.Clear();

                // Add all contract events to subscription
                await this.testContext.Contract.Client.SubscribeToAllEvents();

                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 3, 4);
                await this.testContext.Contract.CallAsync("emitTestEvent", 1);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent3", 5, 6);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent1", 2);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent2", 3, 4);
                await this.testContext.Contract.CallAsync("emitTestIndexedEvent3", 5, 6);

                await Task.Delay(5000);
                Assert.That(receivedEvents, Is.EquivalentTo(new[]
                {
                    "TestIndexedEvent2",
                    "TestEvent",
                    "TestIndexedEvent3",
                    "TestIndexedEvent1",
                    "TestIndexedEvent2",
                    "TestIndexedEvent3"
                }));
                Assert.AreEqual(3, this.testContext.Contract.Client.SubscribedTopics.Count);
                Assert.That(this.testContext.Contract.Client.SubscribedTopics.OrderBy(s => s).ToArray(), Is.EquivalentTo(new []
                {
                    "contract",
                    "TestEvent",
                    "TestIndexedEvent1",
                    "TestIndexedEvent2",
                    "TestIndexedEvent3",
                }));

                receivedEvents.Clear();*/
            }, timeout: 35000);
        }

        private class ControllableNonceTxMiddleware : NonceTxMiddleware
        {
            public ulong? ExpectedNextNonce => this.NextNonce;

            public ulong? ForcedNextNonce { get; set; }

            public bool FailOnGetNonceFromNode { get; set; }

            public event Action<ulong> GetNextNonceAsyncCalled;

            public ControllableNonceTxMiddleware(byte[] publicKey, DAppChainClient client) : base(publicKey, client)
            {
            }

            protected override async Task<ulong> GetNextNonceAsync()
            {
                if (this.ForcedNextNonce != null)
                {
                    ulong forcedNextNonce = this.ForcedNextNonce.Value;
                    this.GetNextNonceAsyncCalled?.Invoke(this.ForcedNextNonce.Value);
                    return forcedNextNonce;
                }

                ulong nextNonce = await base.GetNextNonceAsync();
                this.GetNextNonceAsyncCalled?.Invoke(nextNonce);

                return nextNonce;
            }

            protected override Task<ulong> GetNonceFromNodeAsync()
            {
                if (this.FailOnGetNonceFromNode)
                {
                    Assert.Fail("Call to GetNonceFromNodeAsync is not expected");
                }

                return base.GetNonceFromNodeAsync();
            }
        }
    }
}
