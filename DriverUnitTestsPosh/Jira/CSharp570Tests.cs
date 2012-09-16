using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonogoDB.DriverUnitTestsPosh;
using NUnit.Framework;

namespace DriverUnitTestsPosh.Jira.CSharp570
{
    [TestFixture]
    public class CSharp570Tests : PoshTestsBase
    {


        [Test]
        public void Test()
        {
            var script = @"
$cn = [MongoDB.Driver.MongoServer]::Create('mongodb://localhost/');
$db = $cn['EventLog'];
[MongoDB.Driver.MongoCollection[System.Diagnostics.EventLogEntry]]$events = $db['Events.Simple'];
";
            RunScript(script);
        }
    }
}
