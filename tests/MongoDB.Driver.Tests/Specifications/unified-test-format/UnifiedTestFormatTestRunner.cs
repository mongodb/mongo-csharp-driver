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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests.UnifiedTestOperations;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format
{
    public sealed class UnifiedTestFormatTestRunner : IDisposable
    {
        private UnifiedEntityMap _entityMap;
        private List<FailPoint> _failPoints = new List<FailPoint>();

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            // Top-level fields
            var schemaVersion = testCase.Shared["schemaVersion"].AsString; // cannot be null
            var testSetRunOnRequirements = testCase.Shared.GetValue("runOnRequirements", null)?.AsBsonArray;
            var entities = testCase.Shared.GetValue("createEntities", null)?.AsBsonArray;
            var initialData = testCase.Shared.GetValue("initialData", null)?.AsBsonArray;
            // Test fields
            var runOnRequirements = testCase.Test.GetValue("runOnRequirements", null)?.AsBsonArray;
            var skipReason = testCase.Test.GetValue("skipReason", null)?.AsString;
            var operations = testCase.Test["operations"].AsBsonArray; // cannot be null
            var expectEvents = testCase.Test.GetValue("expectEvents", null)?.AsBsonArray;
            var outcome = testCase.Test.GetValue("outcome", null)?.AsBsonArray;
            var async = testCase.Test["async"].AsBoolean; // cannot be null

            Run(schemaVersion, testSetRunOnRequirements, entities, initialData, runOnRequirements, skipReason, operations, expectEvents, outcome, async);
        }

        public void Run(
            string schemaVersion,
            BsonArray testSetRunOnRequirements,
            BsonArray entities,
            BsonArray initialData,
            BsonArray runOnRequirements,
            string skipReason,
            BsonArray operations,
            BsonArray expectedEvents,
            BsonArray outcome,
            bool async)
        {
            var schemaSemanticVersion = SemanticVersion.Parse(schemaVersion);
            if (schemaSemanticVersion < new SemanticVersion(1, 0, 0) ||
                schemaSemanticVersion > new SemanticVersion(1, 1, 0))
            {
                throw new FormatException($"Schema version '{schemaVersion}' is not supported.");
            }
            if (testSetRunOnRequirements != null)
            {
                RequireServer.Check().RunOn(testSetRunOnRequirements);
            }
            if (runOnRequirements != null)
            {
                RequireServer.Check().RunOn(runOnRequirements);
            }
            if (skipReason != null)
            {
                throw new SkipException($"Test skipped because '{skipReason}'.");
            }
            KillOpenTransactions(DriverTestConfiguration.Client);

            _entityMap = new UnifiedEntityMapBuilder().Build(entities);

            if (initialData != null)
            {
                AddInitialData(DriverTestConfiguration.Client, initialData);
            }

            foreach (var operation in operations)
            {
                var cancellationToken = CancellationToken.None;
                AssertOperation(operation.AsBsonDocument, async, cancellationToken);
            }

            if (expectedEvents != null)
            {
                AssertEvents(expectedEvents, _entityMap);
            }
            if (outcome != null)
            {
                AssertOutcome(DriverTestConfiguration.Client, outcome);
            }
        }

        public void Dispose()
        {
            if (_failPoints != null)
            {
                foreach (var failPoint in _failPoints)
                {
                    failPoint?.Dispose();
                }
            }
            try
            {
                KillOpenTransactions(DriverTestConfiguration.Client);
            }
            catch
            {
                // Ignored because Dispose shouldn't fail
            }
            _entityMap?.Dispose();
        }

        // private methods
        private void AddInitialData(IMongoClient client, BsonArray initialData)
        {
            foreach (var dataItem in initialData)
            {
                var collectionName = dataItem["collectionName"].AsString;
                var databaseName = dataItem["databaseName"].AsString;
                var documents = dataItem["documents"].AsBsonArray.Cast<BsonDocument>().ToList();

                var database = client.GetDatabase(databaseName);
                var collection = database
                    .GetCollection<BsonDocument>(collectionName)
                    .WithWriteConcern(WriteConcern.WMajority);

                database.DropCollection(collectionName);
                if (documents.Any())
                {
                    collection.InsertMany(documents);
                }
                else
                {
                    database.WithWriteConcern(WriteConcern.WMajority).CreateCollection(collectionName);
                }
            }
        }

        private void AssertEvents(BsonArray eventItems, UnifiedEntityMap entityMap)
        {
            var unifiedEventMatcher = new UnifiedEventMatcher(new UnifiedValueMatcher(entityMap));
            foreach (var eventItem in eventItems)
            {
                var clientId = eventItem["client"].AsString;
                var eventCapturer = entityMap.GetEventCapturer(clientId);
                var actualEvents = eventCapturer.Events;

                unifiedEventMatcher.AssertEventsMatch(actualEvents, eventItem["events"].AsBsonArray);
            }
        }

        private void AssertOperation(BsonDocument operationDocument, bool async, CancellationToken cancellationToken)
        {
            var operation = CreateOperation(operationDocument, _entityMap);

            switch (operation)
            {
                case IUnifiedEntityTestOperation entityOperation:
                    var result = async
                        ? entityOperation.ExecuteAsync(cancellationToken).GetAwaiter().GetResult()
                        : entityOperation.Execute(cancellationToken);
                    AssertResult(result, operationDocument, _entityMap);
                    break;
                case IUnifiedSpecialTestOperation specialOperation:
                    specialOperation.Execute();
                    break;
                case IUnifiedWithTransactionOperation withTransactionOperation:
                    if (async)
                    {
                        withTransactionOperation.ExecuteAsync(AssertOperation, cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        withTransactionOperation.Execute(AssertOperation, cancellationToken);
                    }
                    break;
                case IUnifiedFailPointOperation failPointOperation:
                    failPointOperation.Execute(out var failPoint);
                    _failPoints.Add(failPoint);
                    break;
                default:
                    throw new FormatException($"Unexpected operation type: '{operation.GetType()}'.");
            }
        }

        private void AssertOutcome(IMongoClient client, BsonArray outcome)
        {
            foreach (var outcomeItem in outcome)
            {
                var collectionName = outcomeItem["collectionName"].AsString;
                var databaseName = outcomeItem["databaseName"].AsString;
                var expectedData = outcomeItem["documents"].AsBsonArray.Cast<BsonDocument>().ToList();

                var findOptions = new FindOptions<BsonDocument> { Sort = "{ _id : 1 }" };
                var collection = client
                    .GetDatabase(databaseName)
                    .GetCollection<BsonDocument>(collectionName)
                    .WithReadPreference(ReadPreference.Primary);
                collection = Feature.ReadConcern.IsSupported(CoreTestConfiguration.ServerVersion)
                    ? collection.WithReadConcern(ReadConcern.Local)
                    : collection;

                var actualData = collection
                    .FindSync(new EmptyFilterDefinition<BsonDocument>(), findOptions)
                    .ToList();

                actualData.Should().Equal(expectedData);
            }
        }

        private void AssertResult(OperationResult actualResult, BsonDocument operation, UnifiedEntityMap entityMap)
        {
            if (operation.TryGetValue("expectResult", out var expectedResult))
            {
                actualResult.Exception.Should().BeNull();

                new UnifiedValueMatcher(entityMap).AssertValuesMatch(actualResult.Result, expectedResult);
            }
            if (operation.TryGetValue("expectError", out var expectedError))
            {
                actualResult.Exception.Should().NotBeNull();
                actualResult.Result.Should().BeNull();

                new UnifiedErrorMatcher().AssertErrorsMatch(actualResult.Exception, expectedError.AsBsonDocument);
            }
            else
            {
                actualResult.Exception.Should().BeNull();
            }
            if (operation.TryGetValue("saveResultAsEntity", out var saveResultAsEntity))
            {
                if (actualResult.Result != null)
                {
                    entityMap.AddResult(saveResultAsEntity.AsString, actualResult.Result);
                }
                else if (actualResult.ChangeStream != null)
                {
                    entityMap.AddChangeStream(saveResultAsEntity.AsString, actualResult.ChangeStream);
                }
                else
                {
                    throw new AssertionException($"Expected result to be present but none found to save with id: '{saveResultAsEntity.AsString}'.");
                }
            }
        }

        private IUnifiedTestOperation CreateOperation(BsonDocument operation, UnifiedEntityMap entityMap)
        {
            var factory = new UnifiedTestOperationFactory(entityMap);

            var operationName = operation["name"].AsString;
            var operationTarget = operation["object"].AsString;
            var operationArguments = operation.GetValue("arguments", null)?.AsBsonDocument;

            return factory.CreateOperation(operationName, operationTarget, operationArguments);
        }

        private void KillOpenTransactions(IMongoClient client)
        {
            if (CoreTestConfiguration.ServerVersion >= new SemanticVersion(3, 6, 0))
            {
                var command = new BsonDocument("killAllSessions", new BsonArray());
                var adminDatabase = client.GetDatabase("admin");

                try
                {
                    adminDatabase.RunCommand<BsonDocument>(command);
                }
                catch (MongoCommandException exception) when (
                    CoreTestConfiguration.ServerVersion < new SemanticVersion(4, 1, 9) &&
                    exception.Code == (int)ServerErrorCode.Interrupted)
                {
                    // Ignored because of SERVER-38297
                }
            }
        }

        // nested types
        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            #region static
            private static readonly string[] __ignoredTestNames =
            {
                "poc-retryable-writes.json:InsertOne fails after multiple retryable writeConcernErrors" // CSHARP-3269
            };
            #endregion

            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.unified_test_format.tests.valid_pass.";

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                var testCases = base.CreateTestCases(document).Where(test => !__ignoredTestNames.Any(ignoredName => test.Name.EndsWith(ignoredName)));
                foreach (var testCase in testCases)
                {
                    foreach (var async in new[] { false, true })
                    {
                        var name = $"{testCase.Name.Replace(PathPrefix, "")}:async={async}";
                        var test = testCase.Test.DeepClone().AsBsonDocument.Add("async", async);
                        yield return new JsonDrivenTestCase(name, testCase.Shared, test);
                    }
                }
            }
        }
    }
}
