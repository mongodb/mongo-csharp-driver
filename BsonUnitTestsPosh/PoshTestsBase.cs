using System.Diagnostics;
using System.Management.Automation.Runspaces;
using NUnit.Framework;

namespace MonogoDB.BsonUnitTestsPosh
{
    public class PoshTestsBase
    {
        protected Runspace runspace;

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
    }
}