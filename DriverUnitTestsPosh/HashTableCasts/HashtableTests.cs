using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;


namespace MonogoDB.DriverUnitTestsPosh.HashtableCasts
{
    [TestFixture]
    public sealed class HashtableTests : PoshTestsBase
    {
        const string scriptFormat = @"
[{0}] @{{
	Name = 'Justin Dearing'
	EmailAddresses = 'zippy1981@gmail.com','justin@mongodb.org'
    PhoneNumber = '718-555-1212'

}};
";
        private void TestHashTableCast(Type type)
        {
            var results = RunScript(string.Format(scriptFormat, type.FullName));
            Assert.AreEqual(3, results.Count, "Expected three result sets");
            Assert.IsTrue(results.Contains(new PSObject(new BsonElement("Name", "Justin Dearing"))));
            Assert.IsTrue(results.Contains(new PSObject(new BsonElement("PhoneNumber", "718-555-1212"))));
        }

        [Test]
        public void TestCollectionOptionsDocument()
        {
            TestHashTableCast(typeof(CollectionOptionsDocument));
        }

        [Test]
        public void TestCommandDocument()
        {
            TestHashTableCast(typeof(CommandDocument));
        }

        [Test]
        public void TestFieldsDocument()
        {
            TestHashTableCast(typeof(FieldsDocument));
        }

        [Test]
        public void TestGeoHaystackSearchOptionsDocument()
        {
            TestHashTableCast(typeof(GeoHaystackSearchOptionsDocument));
        }

        [Test]
        public void TestGeoNearOptionsDocument()
        {
            TestHashTableCast(typeof(GeoNearOptionsDocument));
        }

        [Test]
        public void TestGroupByDocument()
        {
            TestHashTableCast(typeof(GroupByDocument));
        }

        [Test]
        public void TestIndexKeysDocument()
        {
            TestHashTableCast(typeof(IndexKeysDocument));
        }

        [Test]
        public void TestIndexOptionsDocument()
        {
            TestHashTableCast(typeof(IndexOptionsDocument));
        }

        [Test]
        public void TestMapReduceOptionsDocument()
        {
            TestHashTableCast(typeof(MapReduceOptionsDocument));
        }

        [Test]
        public void TestQueryDocument()
        {
            TestHashTableCast(typeof(QueryDocument));
        }

        [Test]
        public void TestScopeDocument()
        {
            TestHashTableCast(typeof(ScopeDocument));
        }
    }



}
