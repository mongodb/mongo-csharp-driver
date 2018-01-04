/* Copyright 2017-present MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes
{
    public class TestRunner
    {
        // public methods
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(TestCase testCase)
        {
            var definition = testCase.Definition;
            var test = testCase.Test;
            var async = testCase.Async;

            VerifyServerRequirements(definition);
            VerifyFields(definition, "path", "data", "minServerVersion", "maxServerVersion", "tests");

            var collection = InitializeCollection(definition);
            RunTest(collection, test, async);
        }

        // private methods
        private void VerifyServerRequirements(BsonDocument definition)
        {
            RequireServer.Check().ClusterType(ClusterType.ReplicaSet);

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
        }

        private void VerifyFields(BsonDocument document, params string[] expectedNames)
        {
            foreach (var name in document.Names)
            {
                if (!expectedNames.Contains(name))
                {
                    throw new FormatException($"Unexpected field: \"{name}\".");
                }
            }
        }

        private IMongoCollection<BsonDocument> InitializeCollection(BsonDocument definition)
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            clientSettings.RetryWrites = true;
            var client = new MongoClient(clientSettings);
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            database.DropCollection(collection.CollectionNamespace.CollectionName);
            collection.InsertMany(definition["data"].AsBsonArray.Cast<BsonDocument>());

            return collection;
        }

        private void RunTest(IMongoCollection<BsonDocument> collection, BsonDocument test, bool async)
        {
            VerifyFields(test, "description", "failPoint", "operation", "outcome");
            var failPoint = (BsonDocument)test.GetValue("failPoint", null);
            var operation = test["operation"].AsBsonDocument;
            var outcome = test["outcome"].AsBsonDocument;

            var executableTest = CreateExecutableTest(operation);
            using (ConfigureFailPoint(failPoint))
            {
                executableTest.Execute(collection, async);
                executableTest.VerifyOutcome(collection, outcome);
            }
        }

        private IRetryableWriteTest CreateExecutableTest(BsonDocument operation)
        {
            var operationName = operation["name"].AsString;

            IRetryableWriteTest executableTest;
            switch (operationName)
            {
                case "bulkWrite": executableTest = new BulkWriteTest(); break;
                case "deleteOne": executableTest = new DeleteOneTest(); break;
                case "findOneAndDelete": executableTest = new FindOneAndDeleteTest(); break;
                case "findOneAndReplace": executableTest = new FindOneAndReplaceTest(); break;
                case "findOneAndUpdate": executableTest = new FindOneAndUpdateTest(); break;
                case "insertOne": executableTest = new InsertOneTest(); break;
                case "insertMany": executableTest = new InsertManyTest(); break;
                case "replaceOne": executableTest = new ReplaceOneTest(); break;
                case "updateOne": executableTest = new UpdateOneTest(); break;
                default: throw new ArgumentException($"Unexpected operation name: {operationName}.");
            }
            executableTest.Initialize(operation);

            return executableTest;
        }

        private IDisposable ConfigureFailPoint(BsonDocument failPoint)
        {
            if (failPoint == null)
            {
                return null;
            }
            else
            {
                var adminDatabase = DriverTestConfiguration.Client.GetDatabase("admin");
                var enableFailPointCommand = new BsonDocument
                {
                    { "configureFailPoint", "onPrimaryTransactionalWrite" }
                }
                .AddRange(failPoint);
                adminDatabase.RunCommand<BsonDocument>(enableFailPointCommand);

                var disableFailPointCommand = new BsonDocument
                {
                    { "configureFailPoint", "onPrimaryTransactionalWrite" },
                    { "mode", "off" }
                };
                return new ActionDisposer(() => adminDatabase.RunCommand<BsonDocument>(disableFailPointCommand));
            }
        }

        // nested types
        public class TestCase : IXunitSerializable
        {
            public string Name;
            public BsonDocument Definition;
            public BsonDocument Test;
            public bool Async;

            public TestCase()
            {
            }

            public TestCase(string name, BsonDocument definition, BsonDocument test, bool async)
            {
                Name = name;
                Definition = definition;
                Test = test;
                Async = async;
            }

            public void Deserialize(IXunitSerializationInfo info)
            {
                Name = info.GetValue<string>(nameof(Name));
                Definition = BsonDocument.Parse(info.GetValue<string>(nameof(Definition)));
                Test = BsonDocument.Parse(info.GetValue<string>(nameof(Test)));
                Async = info.GetValue<bool>(nameof(Async));
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                info.AddValue(nameof(Name), Name);
                info.AddValue(nameof(Definition), Definition.ToString());
                info.AddValue(nameof(Test), Test.ToString());
                info.AddValue(nameof(Async), Async);
            }

            public override string ToString()
            {
                return Async ? $"{Name}(Async)" : Name;
            }
        }

        private class TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                const string prefix = "MongoDB.Driver.Tests.Specifications.retryable_writes.tests.";
                var definitions = typeof(TestCaseFactory).GetTypeInfo().Assembly
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .Select(path => ReadDefinition(path));

                var testCases = new List<object[]>();
                foreach (var definition in definitions)
                {
                    foreach (BsonDocument test in definition["tests"].AsBsonArray)
                    {
                        foreach (var async in new[] { false, true })
                        {
                            var name = test["description"].ToString();
                            var testCase = new object[] { new TestCase(name, definition, test, async) };
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

        private class ActionDisposer : IDisposable
        {
            private readonly Action _action;

            public ActionDisposer(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}
