/* Copyright 2010-2013 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp216
{
    [TestFixture]
    public class CSharp216Tests
    {
        private MongoServer _server;
        private MongoDatabase _database;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
        }

        [Test]
        public void TestAmbiguousEvalArguments()
        {
            var code = "function (x, y) { return y; }";
            var objectArrayArg = new object[] { 1, 2, 3 };
            var boolArg = true;
            var result = _database.Eval(code, objectArrayArg, boolArg); // before change boolArg was being misinterpreted as nolock argument
            Assert.AreEqual(BsonType.Boolean, result.BsonType);
            Assert.AreEqual(true, result.AsBoolean);
        }

        [Test]
        public void TestNoLock()
        {
            var code = "function (x, y) { return y; }";
            var objectArrayArg = new object[] { 1, 2, 3 };
            var boolArg = true;
            var result = _database.Eval(EvalFlags.NoLock, code, objectArrayArg, boolArg); // before change boolArg was being misinterpreted as nolock argument
            Assert.AreEqual(BsonType.Boolean, result.BsonType);
            Assert.AreEqual(true, result.AsBoolean);
        }
    }
}
