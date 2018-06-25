using System;
using NUnit.Framework;
using Org.BouncyCastle.Math;
using UnityEngine;

namespace Loom.Unity3d.Tests
{
    public class GenericTests
    {
        [Test]
        public void AddressTest() {
            const string testStringAddress = "0x1d655354f10499ef1e32e5a4e8b712606af33628";
            Address address = (Address) testStringAddress;
            Debug.Log(address);

            Assert.AreEqual(testStringAddress.ToLowerInvariant(), address.LocalAddress.ToLowerInvariant());
            Assert.AreEqual(address, Address.FromString(testStringAddress));
            Assert.AreNotEqual(address, Address.FromString(testStringAddress, "test"));

            Assert.AreEqual(Address.FromString(testStringAddress), address);

            Assert.Throws<ArgumentException>(() => Address.FromString("whatever"));

            Address addressWithChainId = Address.FromString("test:" + testStringAddress);
            Assert.AreEqual("test", addressWithChainId.ChainId);
            Assert.AreEqual(testStringAddress, addressWithChainId.LocalAddress.ToLowerInvariant());
        }

        [Test]
        public void BigIntegerTest() {
            BigInteger a = BigInteger.ValueOf(1337);
            BigInteger b = new BigInteger("1337");
            Assert.AreEqual(a, b);
        }
    }
}
