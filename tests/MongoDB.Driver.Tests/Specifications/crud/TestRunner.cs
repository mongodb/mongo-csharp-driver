/* Copyright 2010-2016 MongoDB Inc.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using System.Collections;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class TestRunner
    {
        private static Dictionary<string, Func<ICrudOperationTest>> _tests;
        private static bool __oneTimeSetupHasRun = false;
        private static object __oneTimeSetupLock = new object();

        public TestRunner()
        {
            lock (__oneTimeSetupLock)
            {
                __oneTimeSetupHasRun = __oneTimeSetupHasRun || OneTimeSetup();
            }
        }

        public bool OneTimeSetup()
        {
            _tests = new Dictionary<string, Func<ICrudOperationTest>>
            {
                { "aggregate", () => new AggregateTest() },
                { "count", () => new CountTest() },
                { "deleteMany", () => new DeleteManyTest() },
                { "deleteOne", () => new DeleteOneTest() },
                { "distinct", () => new DistinctTest() },
                { "find", () => new FindTest() },
                { "findOneAndDelete", () => new FindOneAndDeleteTest() },
                { "findOneAndReplace", () => new FindOneAndReplaceTest() },
                { "findOneAndUpdate", () => new FindOneAndUpdateTest() },
                { "insertMany", () => new InsertManyTest() },
                { "insertOne", () => new InsertOneTest() },
                { "replaceOne", () => new ReplaceOneTest() },
                { "updateOne", () => new UpdateOneTest() },
                { "updateMany", () => new UpdateManyTest() }
            };

            return true;
        }

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(BsonDocument definition, BsonDocument test, bool async)
        {
            BsonValue minServerVersion;
            if (definition.TryGetValue("minServerVersion", out minServerVersion))
            {
                RequireServer.Check().VersionGreaterThanOrEqualTo(minServerVersion.AsString);
            }

            BsonValue maxServerVersion;
            if (definition.TryGetValue("maxServerVersion", out maxServerVersion))
            {
                RequireServer.Check().VersionLessThanOrEqualTo(maxServerVersion.AsString);
            }

            var database = DriverTestConfiguration.Client
                .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database
                .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            database.DropCollection(collection.CollectionNamespace.CollectionName);
            collection.InsertMany(definition["data"].AsBsonArray.Cast<BsonDocument>());

            ExecuteOperation(database, collection, (BsonDocument)test["operation"], (BsonDocument)test["outcome"], async);
        }

        private void ExecuteOperation(IMongoDatabase database, IMongoCollection<BsonDocument> collection, BsonDocument operation, BsonDocument outcome, bool async)
        {
            var name = (string)operation["name"];
            Func<ICrudOperationTest> factory;
            if (!_tests.TryGetValue(name, out factory))
            {
                throw new NotImplementedException("The operation " + name + " has not been implemented.");
            }

            var arguments = (BsonDocument)operation.GetValue("arguments", new BsonDocument());
            var test = factory();
            string reason;
            if (!test.CanExecute(DriverTestConfiguration.Client.Cluster.Description, arguments, out reason))
            {
                throw new SkipTestException(reason);
            }

            test.Execute(DriverTestConfiguration.Client.Cluster.Description, database, collection, arguments, outcome, async);
        }

        private class TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                const string prefix = "MongoDB.Driver.Tests.Specifications.crud.tests.";
                var definitions = typeof(TestCaseFactory).GetTypeInfo().Assembly
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .Select(path => ReadDefinition(path));

                var testCases = new List<object[]>();
                foreach (var definition in definitions)
                {
                    foreach (BsonDocument test in definition["tests"].AsBsonArray)
                    {
                        foreach (var async in new[] { false, true})
                        {
                            //var testCase = new TestCaseData(definition, test, async);
                            //testCase.SetCategory("Specifications");
                            //testCase.SetCategory("crud");
                            //testCase.SetName($"{test["description"]}({async})");
                            var testCase = new object[] { definition, test, async };
                            testCases.Add(testCase);
                        }
                    }
                }

                return testCases.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static BsonDocument ReadDefinition(string path)
            {
                using (var definitionStream = typeof(TestCaseFactory).GetTypeInfo().Assembly.GetManifestResourceStream(path))
                using (var definitionStringReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStringReader.ReadToEnd();
                    var definition = BsonDocument.Parse(definitionString);
                    definition.InsertAt(0, new BsonElement("path", path));
                    return definition;
                }
            }
        }
    }
}
