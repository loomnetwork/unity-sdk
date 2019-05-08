using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Loom.Client.Protobuf;
using Loom.Google.Protobuf;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loom.Client.Tests
{
    public class ContractTests
    {
        private string testsAbi;

        [SetUp]
        public void SetUp()
        {
            this.testsAbi = Resources.Load<TextAsset>("Tests.abi").text;
        }

        [UnityTest]
        public IEnumerator MultipleSimultaneousRpcCallsMustSucceedTest()
        {
            return AsyncEditorTestUtility.AsyncTest(async () =>
                {
                    byte[] privateKey = CryptoUtils.GeneratePrivateKey();
                    byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
                    EvmContract contract = await ContractTestUtility.GetEvmContract(privateKey, publicKey, this.testsAbi);

                    using (contract.Client)
                    {
                        const int callCount = 20;
                        Task[] callTasks = new Task[callCount];
                        for (int i = 0; i < callTasks.Length; i++)
                        {
                            // Any request will do. Use the nonce request, simple it is a simple one
                            callTasks[i] = new Task<ulong>(() => contract.Client.GetNonceAsync(CryptoUtils.BytesToHexString(publicKey)).Result);
                        }

                        foreach (Task task in callTasks)
                        {
                            task.Start();
                        }

                        await Task.WhenAll(callTasks);
                    }
                });
        }

        [UnityTest]
        public IEnumerator CallsMustWaitForOtherCallToComplete()
        {
            // If multiple calls are made at the same time, only one should be actually communicating
            // with the backend at a given moment, others must wait for completion
            return AsyncEditorTestUtility.AsyncTest(async () =>
            {
                byte[] privateKey = CryptoUtils.GeneratePrivateKey();
                byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
                EvmContract contract = await ContractTestUtility.GetEvmContract(privateKey, publicKey, this.testsAbi);

                using (contract.Client)
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
                            await contract.CallAsync("setTestUint", new BigInteger(iCopy));
                            Debug.Log($"Call {iCopy}: End");
                            callEndOrderList.Add(iCopy);
                        };
                        callTasks[i] = taskFunc();
                    }

                    await Task.WhenAll(callTasks);

                    Assert.AreEqual(callCount, callEndOrderList.Count);
                    Assert.AreEqual(callEndOrderList.OrderBy(i => i).ToList(), callEndOrderList);
                }
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
                        this.testsAbi,
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
                        this.testsAbi,
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
                        this.testsAbi,
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
