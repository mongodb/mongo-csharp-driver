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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes.prose_tests
{
    public class CommandConstructionTests
    {
        private readonly string _collectionName = CoreTestConfiguration.GetCollectionNamespaceForTestClass(typeof(CommandConstructionTests)).CollectionName;
        private readonly string _databaseName = CoreTestConfiguration.DatabaseNamespace.DatabaseName;

        [SkippableTheory]
        [ParameterAttributeData]
        public void Unacknowledged_writes_should_not_have_transaction_id(
            [Values("delete", "insert", "update")] string operation,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            DropCollection();
            var eventCapturer = CreateEventCapturer();
            using (var client = CreateDisposableClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName).WithWriteConcern(WriteConcern.Unacknowledged);

                switch (operation)
                {
                    case "delete":
                        var deleteFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        if (async)
                        {
                            collection.DeleteOneAsync(deleteFilter).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.DeleteOne(deleteFilter);
                        }
                        break;

                    case "insert":
                        var document = new BsonDocument("_id", 1);
                        if (async)
                        {
                            collection.InsertOneAsync(document).GetAwaiter().GetResult(); ;
                        }
                        else
                        {
                            collection.InsertOne(document);
                        }
                        SpinUntilCollectionIsNotEmpty(); // wait for unacknowledged insert to complete so it won't execute later while another test is running
                        break;

                    case "update":
                        var updateFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        var update = Builders<BsonDocument>.Update.Set("x", 1);
                        if (async)
                        {
                            collection.UpdateOneAsync(updateFilter, update).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.UpdateOne(updateFilter, update);
                        }
                        break;

                    default:
                        throw new Exception($"Unexpected operation: {operation}.");
                }

                AssertCommandDoesNotHaveTransactionId(eventCapturer);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Unsupported_single_statement_writes_should_not_have_transaction_id(
            [Values("deleteMany", "updateMany")] string operation,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            DropCollection();
            var eventCapturer = CreateEventCapturer();
            using (var client = CreateDisposableClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName);

                switch (operation)
                {
                    case "deleteMany":
                        var deleteManyFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        if (async)
                        {
                            collection.DeleteManyAsync(deleteManyFilter).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.DeleteMany(deleteManyFilter);
                        }
                        break;

                    case "updateMany":
                        var updateManyFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        var update = Builders<BsonDocument>.Update.Set("x", 1);
                        if (async)
                        {
                            collection.UpdateManyAsync(updateManyFilter, update).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.UpdateMany(updateManyFilter, update);
                        }
                        break;

                    default:
                        throw new Exception($"Unexpected operation: {operation}.");
                }

                AssertCommandDoesNotHaveTransactionId(eventCapturer);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Unsupported_multi_statement_writes_should_not_have_transaction_id(
            [Values("deleteMany", "updateMany")] string operation,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            DropCollection();
            var eventCapturer = CreateEventCapturer();
            using (var client = CreateDisposableClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName);

                WriteModel<BsonDocument>[] requests;
                switch (operation)
                {
                    case "deleteMany":
                        var deleteManyFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        requests = new[] { new DeleteManyModel<BsonDocument>(deleteManyFilter) };
                        break;

                    case "updateMany":
                        var updateManyFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        var update = Builders<BsonDocument>.Update.Set("x", 1);
                        requests = new[] { new UpdateManyModel<BsonDocument>(updateManyFilter, update) };
                        break;

                    default:
                        throw new Exception($"Unexpected operation: {operation}.");
                }
                if (async)
                {
                    collection.BulkWriteAsync(requests).GetAwaiter().GetResult();
                }
                else
                {
                    collection.BulkWrite(requests);
                }

                AssertCommandDoesNotHaveTransactionId(eventCapturer);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Aggregate_with_write_stage_should_not_have_transaction_id(
            [Values("$out", "$merge")] string outStage,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);
            if (outStage == "$merge")
            {
                RequireServer.Check().Supports(Feature.AggregateMerge);
            }

            DropAndCreateCollection();

            var eventCapturer = CreateEventCapturer();
            using (var client = CreateDisposableClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName);

                PipelineDefinition<BsonDocument, BsonDocument> pipeline = new EmptyPipelineDefinition<BsonDocument>();
                var outputCollection = database.GetCollection<BsonDocument>(_collectionName + "-outputCollection");
                switch (outStage)
                {
                    case "$out":
                        pipeline = pipeline.Out(outputCollection);
                        break;

                    case "$merge":
                        var mergeOptions = new MergeStageOptions<BsonDocument>();
                        pipeline = pipeline.Merge(outputCollection, mergeOptions);
                        break;

                    default:
                        throw new Exception($"Unexpected outStage: {outStage}.");
                }
                if (async)
                {
                    collection.AggregateAsync(pipeline).GetAwaiter().GetResult();
                }
                else
                {
                    collection.Aggregate(pipeline);
                }

                AssertCommandDoesNotHaveTransactionId(eventCapturer);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Supported_single_statement_writes_should_have_transaction_id(
            [Values("insertOne", "updateOne", "replaceOne", "deleteOne", "findOneAndDelete", "findOneAndReplace", "findOneAndUpdate")] string operation,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.6.0").ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            DropCollection();
            var eventCapturer = CreateEventCapturer();
            using (var client = CreateDisposableClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName);

                switch (operation)
                {
                    case "deleteOne":
                        var deleteOneFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        if (async)
                        {
                            collection.DeleteOneAsync(deleteOneFilter).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.DeleteOne(deleteOneFilter);
                        }
                        break;

                    case "findOneAndDelete":
                        var findOneAndDeleteFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        if (async)
                        {
                            collection.FindOneAndDeleteAsync(findOneAndDeleteFilter).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.FindOneAndDelete(findOneAndDeleteFilter);
                        }
                        break;

                    case "findOneAndReplace":
                        var findOneAndReplaceFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        var findOneAndReplaceReplacement = new BsonDocument("_id", 1);
                        if (async)
                        {
                            collection.FindOneAndReplaceAsync(findOneAndReplaceFilter, findOneAndReplaceReplacement).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.FindOneAndReplace(findOneAndReplaceFilter, findOneAndReplaceReplacement);
                        }
                        break;

                    case "findOneAndUpdate":
                        var findOneAndUpdateFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        var findOneAndUpdateUpdate = Builders<BsonDocument>.Update.Set("x", 2);
                        if (async)
                        {
                            collection.FindOneAndUpdateAsync(findOneAndUpdateFilter, findOneAndUpdateUpdate).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.FindOneAndUpdate(findOneAndUpdateFilter, findOneAndUpdateUpdate);
                        }
                        break;

                    case "insertOne":
                        var document = new BsonDocument("_id", 1);
                        if (async)
                        {
                            collection.InsertOneAsync(document).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.InsertOne(document);
                        }
                        break;

                    case "replaceOne":
                        var replaceOneFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        var replacement = new BsonDocument("_id", 1);
                        if (async)
                        {
                            collection.ReplaceOneAsync(replaceOneFilter, replacement).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.ReplaceOne(replaceOneFilter, replacement);
                        }
                        break;

                    case "updateOne":
                        var updateOneFilter = Builders<BsonDocument>.Filter.Eq("_id", 1);
                        var updateOne = Builders<BsonDocument>.Update.Set("x", 2);
                        if (async)
                        {
                            collection.UpdateOneAsync(updateOneFilter, updateOne).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.UpdateOne(updateOneFilter, updateOne);
                        }
                        break;

                    default:
                        throw new Exception($"Unexpected operation: {operation}.");
                }

                AssertCommandHasTransactionId(eventCapturer);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Supported_multi_statement_writes_should_have_transaction_id(
            [Values("insertMany", "bulkWrite")] string operation,
            [Values(false, true)] bool ordered,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.6.0").ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            DropCollection();
            var eventCapturer = CreateEventCapturer();
            using (var client = CreateDisposableClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName);

                switch (operation)
                {
                    case "bulkWrite":
                        var requests = new[] { new InsertOneModel<BsonDocument>(new BsonDocument("_id", 1)) };
                        var bulkWriteOptions = new BulkWriteOptions { IsOrdered = ordered };
                        if (async)
                        {
                            collection.BulkWriteAsync(requests, bulkWriteOptions).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.BulkWrite(requests, bulkWriteOptions);
                        }
                        break;

                    case "insertMany":
                        var documents = new[] { new BsonDocument("_id", 1) };
                        var insertManyOptions = new InsertManyOptions { IsOrdered = ordered };
                        if (async)
                        {
                            collection.InsertManyAsync(documents, insertManyOptions).GetAwaiter().GetResult();
                        }
                        else
                        {
                            collection.InsertMany(documents, insertManyOptions);
                        }
                        break;

                    default:
                        throw new Exception($"Unexpected operation: {operation}.");
                }

                AssertCommandHasTransactionId(eventCapturer);
            }
        }

        // private methods
        private void AssertCommandDoesNotHaveTransactionId(EventCapturer eventCapturer)
        {
            var commandStartedEvent = eventCapturer.Events.OfType<CommandStartedEvent>().Single();
            var command = commandStartedEvent.Command;
            command.Should().NotContain("txnNumber");
        }

        private void AssertCommandHasTransactionId(EventCapturer eventCapturer)
        {
            var commandStartedEvent = eventCapturer.Events.OfType<CommandStartedEvent>().Single();
            var command = commandStartedEvent.Command;
            command.Should().Contain("txnNumber");
        }

        private DisposableMongoClient CreateDisposableClient(EventCapturer eventCapturer)
        {
            return DriverTestConfiguration.CreateDisposableClient((MongoClientSettings settings) =>
            {
                settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                settings.RetryWrites = true;
            });
        }

        private EventCapturer CreateEventCapturer()
        {
            var commandsToNotCapture = new HashSet<string>
            {
                "isMaster",
                "buildInfo",
                "getLastError",
                "authenticate",
                "saslStart",
                "saslContinue",
                "getnonce"
            };

            return
                new EventCapturer()
                .Capture<CommandStartedEvent>(e => !commandsToNotCapture.Contains(e.CommandName));
        }

        private void DropAndCreateCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName);
            database.DropCollection(_collectionName);
            database.CreateCollection(_collectionName);
        }

        private void DropCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName);
            database.DropCollection(_collectionName);
        }

        private void SpinUntilCollectionIsNotEmpty()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName);
            var collection = database.GetCollection<BsonDocument>(_collectionName);
            SpinWait.SpinUntil(() => collection.CountDocuments("{}") > 0, TimeSpan.FromSeconds(10)).Should().BeTrue();
        }
    }
}
