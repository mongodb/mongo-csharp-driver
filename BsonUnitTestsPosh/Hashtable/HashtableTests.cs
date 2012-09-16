using System;
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
$bsonDoc = [MongoDB.Bson.BsonDocument] @{
	Name = 'Justin Dearing'
	EmailAddresses = 'zippy1981@gmail.com','justin@mongodb.org'
    PhoneNumber = '718-555-1212'

};

$bsonDoc

#$bsonDoc.ToHashtable()
#New-Object PSObject -Property $bsonDoc.ToHashtable() 
";
            using (var pipeline = runspace.CreatePipeline())
            {
                pipeline.Commands.AddScript(script);
                var results = pipeline.Invoke();
                Assert.AreEqual(3, results.Count, "Expected three result sets");
                
            }
        }
    }
}
