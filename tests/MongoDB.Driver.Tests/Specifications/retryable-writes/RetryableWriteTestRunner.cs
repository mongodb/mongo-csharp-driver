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
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes
{
    public class RetryableWriteTestRunner
    {
        private readonly string _databaseName = DriverTestConfiguration.DatabaseNamespace.DatabaseName;
        private readonly string _collectionName = DriverTestConfiguration.CollectionNamespace.CollectionName;

        // public methods
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(TestCase testCase)
        {
            var definition = testCase.Definition;
            var test = testCase.Test;
            var async = testCase.Async;

            VerifyServerRequirements(definition);
            VerifyFields(definition, "path", "data", "runOn", "tests");

            InitializeCollection(definition);
            RunTest(test, async);
        }

        // private methods
        private void VerifyServerRequirements(BsonDocument definition)
        {
            if (definition.TryGetValue("runOn", out var runOn))
            {
                RequireServer.Check().RunOn(runOn.AsBsonArray);
            }

            if (CoreTestConfiguration.GetStorageEngine() == "mmapv1")
            {
                throw new SkipException("Test skipped because mmapv1 does not support retryable writes.");
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

        private void InitializeCollection(BsonDocument definition)
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName);
            var collection = database.GetCollection<BsonDocument>(_collectionName);

            database.DropCollection(collection.CollectionNamespace.CollectionName);
            collection.InsertMany(definition["data"].AsBsonArray.Cast<BsonDocument>());
        }

        private void RunTest(BsonDocument test, bool async)
        {
            VerifyFields(test, "description", "clientOptions", "failPoint", "operation", "outcome", "useMultipleMongoses");
            var failPoint = (BsonDocument)test.GetValue("failPoint", null);
            var operation = test["operation"].AsBsonDocument;
            var outcome = test["outcome"].AsBsonDocument;

            using (var client = CreateDisposableClient(test))
            {
                var database = client.GetDatabase(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName);
                var executableTest = CreateExecutableTest(operation);

                using (ConfigureFailPoint(client, failPoint))
                {
                    executableTest.Execute(collection, async);
                }

                executableTest.VerifyOutcome(collection, outcome);
            }
        }

        private DisposableMongoClient CreateDisposableClient(BsonDocument test)
        {
            var useMultipleShardRouters = test.GetValue("useMultipleMongoses", false).AsBoolean;
            var clientOptions = (BsonDocument)test.GetValue("clientOptions", null);

            return DriverTestConfiguration.CreateDisposableClient(
                settings =>
                {
                    ParseClientOptions(settings, clientOptions);
                },
                useMultipleShardRouters);
        }

        private void ParseClientOptions(MongoClientSettings settings, BsonDocument clientOptions)
        {
            if (clientOptions == null)
            {
                settings.RetryWrites = true;
            }
            else
            {
                foreach (var element in clientOptions)
                {
                    var name = element.Name;
                    var value = element.Value;

                    switch (name)
                    {
                        case "retryWrites":
                            settings.RetryWrites = value.ToBoolean();
                            break;

                        default:
                            throw new FormatException($"Invalid clientOptions field: {name}.");
                    }
                }
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
                case "deleteMany": executableTest = new DeleteManyTest(); break;
                case "findOneAndDelete": executableTest = new FindOneAndDeleteTest(); break;
                case "findOneAndReplace": executableTest = new FindOneAndReplaceTest(); break;
                case "findOneAndUpdate": executableTest = new FindOneAndUpdateTest(); break;
                case "insertOne": executableTest = new InsertOneTest(); break;
                case "insertMany": executableTest = new InsertManyTest(); break;
                case "replaceOne": executableTest = new ReplaceOneTest(); break;
                case "updateOne": executableTest = new UpdateOneTest(); break;
                case "updateMany": executableTest = new UpdateManyTest(); break;
                default: throw new ArgumentException($"Unexpected operation name: {operationName}.");
            }
            executableTest.Initialize(operation);

            return executableTest;
        }

        private IDisposable ConfigureFailPoint(IMongoClient client, BsonDocument failPoint)
        {
            if (failPoint == null)
            {
                return null;
            }
            else
            {
                var adminDatabase = client.GetDatabase("admin");
                adminDatabase.RunCommand<BsonDocument>(failPoint);

                var failPointName = failPoint["configureFailPoint"].AsString;
                var disableFailPointCommand = new BsonDocument
                {
                    { "configureFailPoint", failPointName },
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
