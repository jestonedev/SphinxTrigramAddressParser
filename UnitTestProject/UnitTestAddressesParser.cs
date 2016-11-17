using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SphinxTrigramAddressParser;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTestAddressesParser
    {
        [TestMethod]
        public void TestParseRawAddressNormal()
        {
            var parser = new AddressesParser(new List<Premise>
            {
                new Premise
                {
                    RawAddress = "ул. Комсомольская, д.77, кв.701"
                }
            }, new ConsoleLogger() );
            Assert.AreEqual(1, parser.PreparedPremises.Count);
            Assert.AreEqual(1, parser.PreparedPremises[0].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][0].Count);
            Assert.AreEqual("701", parser.PreparedPremises[0][0][0].PremiseNumber);
            Assert.AreEqual(0, parser.PreparedPremises[0][0][0].SubPremises.Count);
        }

        [TestMethod]
        public void TestParseRawAddressTwoPremises()
        {
            var parser = new AddressesParser(new List<Premise>
            {
                new Premise
                {
                    RawAddress = "ул. Комсомольская, д.77, кв.701,702"
                }
            }, new ConsoleLogger());
            Assert.AreEqual(1, parser.PreparedPremises.Count);
            Assert.AreEqual(2, parser.PreparedPremises[0].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][0].Count);
            Assert.AreEqual(2, parser.PreparedPremises[0][1].Count);
            Assert.AreEqual("701,702", parser.PreparedPremises[0][0][0].PremiseNumber);
            Assert.AreEqual("701", parser.PreparedPremises[0][1][0].PremiseNumber);
            Assert.AreEqual("702", parser.PreparedPremises[0][1][1].PremiseNumber);
            Assert.AreEqual(0, parser.PreparedPremises[0][0][0].SubPremises.Count);
            Assert.AreEqual(0, parser.PreparedPremises[0][1][0].SubPremises.Count);
            Assert.AreEqual(0, parser.PreparedPremises[0][1][1].SubPremises.Count);
        }

        [TestMethod]
        public void TestParseRawAddressThreePremisesSlashedWithZ()
        {
            var parser = new AddressesParser(new List<Premise>
            {
                new Premise
                {
                    RawAddress = "Юго-Западная ул., д.23-а, кв.14/15/16/з"
                }
            }, new ConsoleLogger());
            Assert.AreEqual(1, parser.PreparedPremises.Count);
            Assert.AreEqual(4, parser.PreparedPremises[0].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][0].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][1].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][2].Count);
            Assert.AreEqual(3, parser.PreparedPremises[0][3].Count);
            Assert.AreEqual("14", parser.PreparedPremises[0][0][0].PremiseNumber);
            Assert.AreEqual("14,15", parser.PreparedPremises[0][1][0].PremiseNumber);
            Assert.AreEqual("14,15,16", parser.PreparedPremises[0][2][0].PremiseNumber);
            Assert.AreEqual("14", parser.PreparedPremises[0][3][0].PremiseNumber);
            Assert.AreEqual("15", parser.PreparedPremises[0][3][1].PremiseNumber);
            Assert.AreEqual("16", parser.PreparedPremises[0][3][2].PremiseNumber);
            Assert.AreEqual(2, parser.PreparedPremises[0][0][0].SubPremises.Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][1][0].SubPremises.Count);
            Assert.AreEqual(0, parser.PreparedPremises[0][2][0].SubPremises.Count);
            Assert.AreEqual(0, parser.PreparedPremises[0][3][0].SubPremises.Count);
            Assert.AreEqual(0, parser.PreparedPremises[0][3][1].SubPremises.Count);
            Assert.AreEqual(0, parser.PreparedPremises[0][3][2].SubPremises.Count);
        }

        [TestMethod]
        public void TestParseRawAddressTwoPremisesSlashed()
        {
            var parser = new AddressesParser(new List<Premise>
            {
                new Premise
                {
                    RawAddress = "ул. Комсомольская, д.77, кв.701/702"
                }
            }, new ConsoleLogger());
            Assert.AreEqual(1, parser.PreparedPremises.Count);
            Assert.AreEqual(3, parser.PreparedPremises[0].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][0].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][1].Count);
            Assert.AreEqual(2, parser.PreparedPremises[0][2].Count);
            Assert.AreEqual("701", parser.PreparedPremises[0][0][0].PremiseNumber);
            Assert.AreEqual("701,702", parser.PreparedPremises[0][1][0].PremiseNumber);
            Assert.AreEqual("701", parser.PreparedPremises[0][2][0].PremiseNumber);
            Assert.AreEqual("702", parser.PreparedPremises[0][2][1].PremiseNumber);
            Assert.AreEqual(1, parser.PreparedPremises[0][0][0].SubPremises.Count);
            Assert.AreEqual(0, parser.PreparedPremises[0][1][0].SubPremises.Count);
            Assert.AreEqual(0, parser.PreparedPremises[0][2][0].SubPremises.Count);
            Assert.AreEqual(0, parser.PreparedPremises[0][2][1].SubPremises.Count);
        }

        [TestMethod]
        public void TestParseRawAddressPremiseAndRoomSlashed()
        {
            var parser = new AddressesParser(new List<Premise>
            {
                new Premise
                {
                    RawAddress = "Южная ул., д.18, кв.513/б"
                }
            }, new ConsoleLogger());
            Assert.AreEqual(1, parser.PreparedPremises.Count);
            Assert.AreEqual(1, parser.PreparedPremises[0].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][0].Count);
            Assert.AreEqual("513", parser.PreparedPremises[0][0][0].PremiseNumber);
            Assert.AreEqual(1, parser.PreparedPremises[0][0][0].SubPremises.Count);
            Assert.AreEqual("Б", parser.PreparedPremises[0][0][0].SubPremises[0].SubPremiseNumber);
        }

        [TestMethod]
        public void TestParseRawAddressPremiseAndRoomsSlashed()
        {
            var parser = new AddressesParser(new List<Premise>
            {
                new Premise
                {
                    RawAddress = "Южная ул., д.18, кв.513/а,б"
                }
            }, new ConsoleLogger());
            Assert.AreEqual(1, parser.PreparedPremises.Count);
            Assert.AreEqual(1, parser.PreparedPremises[0].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][0].Count);
            Assert.AreEqual("513", parser.PreparedPremises[0][0][0].PremiseNumber);
            Assert.AreEqual(2, parser.PreparedPremises[0][0][0].SubPremises.Count);
            Assert.AreEqual("А", parser.PreparedPremises[0][0][0].SubPremises[0].SubPremiseNumber);
            Assert.AreEqual("Б", parser.PreparedPremises[0][0][0].SubPremises[1].SubPremiseNumber);
        }

        [TestMethod]
        public void TestParseRawAddressPremiseAndThreeRoomsSlashed()
        {
            var parser = new AddressesParser(new List<Premise>
            {
                new Premise
                {
                    RawAddress = "Южная ул., д.18, кв.513/к. а/к. б"
                }
            }, new ConsoleLogger());
            Assert.AreEqual(1, parser.PreparedPremises.Count);
            Assert.AreEqual(1, parser.PreparedPremises[0].Count);
            Assert.AreEqual(1, parser.PreparedPremises[0][0].Count);
            Assert.AreEqual("513", parser.PreparedPremises[0][0][0].PremiseNumber);
            Assert.AreEqual(2, parser.PreparedPremises[0][0][0].SubPremises.Count);
            Assert.AreEqual("А", parser.PreparedPremises[0][0][0].SubPremises[0].SubPremiseNumber);
            Assert.AreEqual("Б", parser.PreparedPremises[0][0][0].SubPremises[1].SubPremiseNumber);
        }
    }
}
