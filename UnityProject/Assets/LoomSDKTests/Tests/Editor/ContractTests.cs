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
                            new InvalidNonceTxMiddleware(publicKey, client),
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
            });
        }

        private class InvalidNonceTxMiddleware : NonceTxMiddleware
        {
            public InvalidNonceTxMiddleware(byte[] publicKey, DAppChainClient client) : base(publicKey, client)
            {
            }

            public override Task<byte[]> Handle(byte[] txData)
            {
                var tx = new NonceTx
                {
                    Inner = ByteString.CopyFrom(txData),
                    Sequence = int.MaxValue
                };
                return Task.FromResult(tx.ToByteArray());
            }
        }
    }
}
