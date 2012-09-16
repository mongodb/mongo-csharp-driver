using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using NUnit.Framework;

namespace MonogoDB.BsonUnitTestsPosh.Hashtable
{
    [TestFixture]
    public sealed class HashtableTests
    {
        private Runspace runspace;

        [TestFixtureSetUp]
        public void Startup()
        {
            runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            using (var pipeline = runspace.CreatePipeline())
            {

                pipeline.Commands.AddScript("Add-Type -Path 'MongoDB.Bson.dll'");
                var results = pipeline.Invoke();
                foreach (var result in results)
                {
                    Debug.WriteLine(result);
                }
            }
        }
        [TestFixtureTearDown]
        public void Stop()
        {
            runspace.Close();
            runspace.Dispose();
        }

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
