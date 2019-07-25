using System;
using System.Numerics;
using NUnit.Framework;

namespace Loom.Client.Tests
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

        [Test]
        public void BigIntegerTest() {
            BigInteger a = new BigInteger(1337);
            BigInteger b = BigInteger.Parse("1337");
            Assert.AreEqual(a, b);
        }

        [Test]
        public void Ed25519Test() {
            byte[] seed = {
                22, 218, 117, 80, 91, 159, 10, 154, 78, 89, 67, 85, 7, 57, 215, 103,
                68, 178, 222, 219, 152, 180, 172, 239, 75, 116, 88, 17, 42, 67, 227, 172,
            };

            byte[] privateKey = CryptoUtils.GeneratePrivateKey(seed);
            string hex = CryptoUtils.BytesToHexString(privateKey);
            Assert.AreEqual("16DA75505B9F0A9A4E5943550739D76744B2DEDB98B4ACEF4B7458112A43E3AC25AE76342B3E06911DC2CDFF70C60736A45C9DC40D7D14BBC455501779DCB04D", hex);
        }
    }
}
