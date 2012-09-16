using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using MongoDB.Bson;
using NUnit.Framework;

namespace MonogoDB.BsonUnitTestsPosh
{
}

namespace MonogoDB.BsonUnitTestsPosh.Hashtable
{
    [TestFixture]
    public sealed class HashtableTests : PoshTestsBase
    {
        [Test]
        public void TestHashTable()
        {
            const string script = @"
[MongoDB.Bson.BsonDocument] @{
	Name = 'Justin Dearing'
	EmailAddresses = 'zippy1981@gmail.com','justin@mongodb.org'
    PhoneNumber = '718-555-1212'

};
";
            var results = RunScript(script);
            Assert.AreEqual(3, results.Count, "Expected three result sets");
            results.Contains(new PSObject(new BsonElement("Name", "Dearing")));
            results.Contains(new PSObject(new BsonElement("PoneNumber", "718-555-1212")));
        }
    }
}
