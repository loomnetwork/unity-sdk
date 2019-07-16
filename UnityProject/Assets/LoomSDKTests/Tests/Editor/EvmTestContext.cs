using System;
using System.Collections;
using System.Numerics;
using System.Threading.Tasks;
using Loom.Nethereum.ABI.FunctionEncoding.Attributes;
using UnityEngine;

namespace Loom.Client.Tests
{
    public class EvmTestContext
    {
        public string TestsAbi { get; private set; }
        public EvmContract Contract { get; private set; }

        public void Setup()
        {
            this.TestsAbi = Resources.Load<TextAsset>("Tests.abi").text;
        }

        public IEnumerator ContractTest(Func<Task> action, int timeout = 10000)
        {
            return AsyncEditorTestUtility.AsyncTest(async () =>
            {
                try
                {
                    await EnsureContract();
                    await action();
                } finally
                {
                    this.Contract?.Client?.Dispose();
                    this.Contract = null;
                }
            }, timeout: timeout);
        }

        public async Task EnsureContract() {
            if (this.Contract != null)
            {
                this.Contract?.Client?.Dispose();
                this.Contract = null;
            }

            byte[] privateKey = CryptoUtils.GeneratePrivateKey();
            byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
            this.Contract = await ContractTestUtility.GetEvmContract(privateKey, publicKey, this.TestsAbi);
        }

        public class TestEvent
        {
            [Parameter("uint", "number", 1, false)]
            public BigInteger Number { get; set; }
        }

        public class TestIndexedEvent1
        {
            [Parameter("uint", "number1", 1, true)]
            public BigInteger Number1 { get; set; }
        }

        public class TestIndexedEvent2
        {
            [Parameter("uint", "number1", 1, false)]
            public BigInteger Number1 { get; set; }

            [Parameter("uint", "number2", 2, true)]
            public BigInteger Number2 { get; set; }
        }

        public class TestIndexedEvent3
        {
            [Parameter("uint", "number1", 1, true)]
            public BigInteger Number1 { get; set; }

            [Parameter("uint", "number2", 2, true)]
            public BigInteger Number2 { get; set; }
        }
    }
}
