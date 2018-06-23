using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Loom.Unity3d.Tests
{
    public class GenericTests
    {
        [Test]
        public void AddressTest() {
            const string testStringAddress = "0x1d655354f10499ef1e32e5a4e8b712606af33628";
            Address address = (Address) testStringAddress;

            Assert.AreEqual(testStringAddress.ToLowerInvariant(), address.LocalAddress.ToLowerInvariant());
            Assert.AreEqual(address, Address.FromString(testStringAddress));
            Assert.AreNotEqual(address, Address.FromString(testStringAddress, "test"));

            Assert.AreEqual(Address.FromString(testStringAddress), address);

            Assert.Throws<ArgumentException>(() => Address.FromString("whatever"));

            Address addressWithChainId = Address.FromString("test:" + testStringAddress);
            Assert.AreEqual("test", addressWithChainId.ChainId);
            Assert.AreEqual(testStringAddress, addressWithChainId.LocalAddress.ToLowerInvariant());
        }
    }
}
