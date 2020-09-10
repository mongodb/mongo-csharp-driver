/* Copyright 2020-present MongoDB Inc.
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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests.Specifications.Runner;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.atlas_data_lake
{
    [Trait("Category", "AtlasDataLake")]
    public class AtlasDataLakeTestRunner : MongoClientJsonDrivenTestRunnerBase
    {
        protected override string[] ExpectedSharedColumns => new[] { "_path", "database_name", "collection_name", "tests" };
        protected override string[] ExpectedTestColumns => new[] { "description", "operations", "expectations", "async" };

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_DATA_LAKE_TESTS_ENABLED");

            SetupAndRunTest(testCase);
        }

        protected override void CreateCollection(IMongoClient client, string databaseName, string collectionName, BsonDocument test, BsonDocument shared)
        {
            // do nothing
        }

        protected override void DropCollection(MongoClient client, string databaseName, string collectionName, BsonDocument test, BsonDocument shared)
        {
            // do nothing
        }

        protected override string GetCollectionName(BsonDocument definition)
        {
            if (definition.TryGetValue(CollectionNameKey, out var collectionName))
            {
                return collectionName.AsString;
            }
            else
            {
                return DriverTestConfiguration.CollectionNamespace.CollectionName;
            }
        }

        protected override string GetDatabaseName(BsonDocument definition)
        {
            if (definition.TryGetValue(DatabaseNameKey, out var databaseName))
            {
                return databaseName.AsString;
            }
            else
            {
                return DriverTestConfiguration.DatabaseNamespace.DatabaseName;
            }
        }

        // nested types
        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.atlas_data_lake.tests.";

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                foreach (var testCase in base.CreateTestCases(document))
                {
                    foreach (var async in new[] { false, true })
                    {
                        var name = $"{testCase.Name}:async={async}";
                        var test = testCase.Test.DeepClone().AsBsonDocument.Add("async", async);
                        yield return new JsonDrivenTestCase(name, testCase.Shared, test);
                    }
                }
            }
        }
    }
}
