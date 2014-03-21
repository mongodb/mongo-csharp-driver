using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira
{
    [TestFixture]
    public class CSharp926Tests
    {
        public enum Enum1 : byte
        {
            E11, E12, E13
        }

        public enum Enum2 : long 
        {
            E21 = 2147483647,
            E22 = 3147483647,
            E23 = 4147483647
        }

        public enum Enum3 : short
        {
            E31, E32, E33
        }

        public class C
        {
            public ObjectId Id;
            public Enum1 E1;
            public Enum1? E1Nullable;
            public Enum2 E2;
            public Enum2? E2Nullable;
            public Enum3 E3;
            public Enum3? E3Nullable;
            
            public List<Enum1> ListE1;
            public List<Enum1?> ListE1Nullable;
            public List<Enum2> ListE2;
            public List<Enum2?> ListE2Nullable;
            public List<Enum3> ListE3;
            public List<Enum3?> ListE3Nullable;
        }

        private MongoCollection<C> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _collection = Configuration.GetTestCollection<C>();
        }

        [Test]
        public void LinqEnumPropertyTest()
        {
            var expectdResult = new C
            {
                E1 = Enum1.E11,
                E1Nullable = Enum1.E11,
                E2 = Enum2.E22,
                E2Nullable = Enum2.E22,
                E3 = Enum3.E33,
                E3Nullable = Enum3.E33,
                ListE1 = new List<Enum1> {Enum1.E11, Enum1.E12},
                ListE1Nullable = new List<Enum1?> { Enum1.E12 },
                ListE2 = new List<Enum2> {Enum2.E21, Enum2.E22},
                ListE2Nullable = new List<Enum2?> { Enum2.E21 },
                ListE3 = new List<Enum3> {Enum3.E31, Enum3.E33},
                ListE3Nullable = new List<Enum3?> { Enum3.E33 }
            };

            _collection.RemoveAll();
            _collection.Save(expectdResult);

            var realResult = _collection.AsQueryable().Where(c => c.E1 == Enum1.E11).ToList();
            Assert.AreEqual(expectdResult.E1, realResult[0].E1);

            realResult = _collection.AsQueryable().Where(c => c.E1Nullable == Enum1.E11).ToList();
            Assert.AreEqual(expectdResult.E1Nullable, realResult[0].E1Nullable);

            realResult = _collection.AsQueryable().Where(c => c.E2 == Enum2.E22).ToList();
            Assert.AreEqual(expectdResult.E2, realResult[0].E2);

            realResult = _collection.AsQueryable().Where(c => c.E2Nullable == Enum2.E22).ToList();
            Assert.AreEqual(expectdResult.E2Nullable, realResult[0].E2Nullable);

            realResult = _collection.AsQueryable().Where(c => c.E3 == Enum3.E33).ToList();
            Assert.AreEqual(expectdResult.E3, realResult[0].E3);

            realResult = _collection.AsQueryable().Where(c => c.E3Nullable == Enum3.E33).ToList();
            Assert.AreEqual(expectdResult.E3Nullable, realResult[0].E3Nullable);

            realResult = _collection.AsQueryable().Where(c => c.ListE1.Contains(Enum1.E11)).ToList();
            Assert.AreEqual(expectdResult.ListE1, realResult[0].ListE1);

            realResult = _collection.AsQueryable().Where(c => c.ListE1Nullable.Contains(Enum1.E12)).ToList();
            Assert.AreEqual(expectdResult.ListE1Nullable, realResult[0].ListE1Nullable);

            realResult = _collection.AsQueryable().Where(c => c.ListE2.Contains(Enum2.E21)).ToList();
            Assert.AreEqual(expectdResult.ListE2, realResult[0].ListE2);

            realResult = _collection.AsQueryable().Where(c => c.ListE2Nullable.Contains(Enum2.E21)).ToList();
            Assert.AreEqual(expectdResult.ListE2Nullable, realResult[0].ListE2Nullable);

            realResult = _collection.AsQueryable().Where(c => c.ListE3.Contains(Enum3.E33)).ToList();
            Assert.AreEqual(expectdResult.ListE3, realResult[0].ListE3);

            realResult = _collection.AsQueryable().Where(c => c.ListE3Nullable.Contains(Enum3.E33)).ToList();
            Assert.AreEqual(expectdResult.ListE3Nullable, realResult[0].ListE3Nullable);
        }
    }
}
