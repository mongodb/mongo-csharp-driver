/* Copyright 2021-present MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.crud.prose_tests
{
    [Trait("Category", "Serverless")]
    public class CrudProseTests : LoggableTestClass
    {
        // public constructors
        public CrudProseTests(ITestOutputHelper output) :
            base(output)
        {
        }

        // public methods
        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L13
        [Fact]
        public void WriteConcernError_details_should_expose_writeConcernError_errInfo()
        {
            var failPointFeature = CoreTestConfiguration.Cluster.Description.Type == ClusterType.Sharded
                ? Feature.FailPointsFailCommandForSharded
                : Feature.FailPointsFailCommand;
            RequireServer.Check().Supports(failPointFeature);

            var failPointCommand = @"
                {
                    configureFailPoint : 'failCommand',
                    data : {
                        failCommands : ['insert'],
                        writeConcernError : {
                            code : 100,
                            codeName : 'UnsatisfiableWriteConcern',
                            errmsg : 'Not enough data-bearing nodes',
                            errInfo : {
                                writeConcern : {
                                    w : 2,
                                    wtimeout : 0,
                                    provenance : 'clientSupplied'
                                }
                            }
                        }
                    },
                    mode: { times: 1 }
                }";

            using (ConfigureFailPoint(failPointCommand))
            {
                var client = DriverTestConfiguration.Client;
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Record.Exception(() => collection.InsertOne(new BsonDocument()));

                exception.Should().NotBeNull();
                var bulkWriteException = exception.InnerException.Should().BeOfType<MongoBulkWriteException<BsonDocument>>().Subject;
                var writeConcernError = bulkWriteException.WriteConcernError;
                writeConcernError.Code.Should().Be(100);
                writeConcernError.CodeName.Should().Be("UnsatisfiableWriteConcern");
                writeConcernError.Details.Should().Be("{ writeConcern : { w : 2, wtimeout : 0, provenance : 'clientSupplied' } }");
                writeConcernError.Message.Should().Be("Not enough data-bearing nodes");
            }
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L45
        [Fact]
        public void WriteError_details_should_expose_writeErrors_errInfo()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo(new SemanticVersion(5, 0, 0, ""));

            var eventCapturer = new EventCapturer().Capture<CommandSucceededEvent>(e => e.CommandName == "insert");
            var collectionName = "WriteError_details_should_expose_writeErrors_errInfo";
            var collectionValidator = BsonDocument.Parse("{ x : { $type : 'string' } }");
            var collectionOptions = new CreateCollectionOptions<BsonDocument> { Validator = collectionValidator };

            Exception exception;
            using (var client = CreateMongoClient(eventCapturer))
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                database.CreateCollection(collectionName, collectionOptions);
                var collection = database.GetCollection<BsonDocument>(collectionName);

                exception = Record.Exception(() => collection.InsertOne(new BsonDocument("x", 1)));
            }

            // Assert MongoWriteException WriteError
            exception.Should().NotBeNull();
            var mongoWriteExcepion = exception.Should().BeOfType<MongoWriteException>().Subject;
            var writeError = mongoWriteExcepion.WriteError;
            var objectId = writeError.Details["failingDocumentId"].AsObjectId;
            var expectedWriteErrorDetails = GetExpectedWriteErrorDetails(objectId);
            writeError.Code.Should().Be(121);
            writeError.Message.Should().Be("Document failed validation");
            writeError.Details.Should().BeEquivalentTo(expectedWriteErrorDetails);

            // Assert MongoBulkWriteException WriteError
            exception.InnerException.Should().NotBeNull();
            var bulkWriteException = exception.InnerException.Should().BeOfType<MongoBulkWriteException<BsonDocument>>().Subject;
            bulkWriteException.WriteErrors.Should().HaveCount(1);
            var bulkWriteWriteError = bulkWriteException.WriteErrors.Single();
            bulkWriteWriteError.Code.Should().Be(121);
            bulkWriteWriteError.Message.Should().Be("Document failed validation");
            bulkWriteWriteError.Details.Should().BeEquivalentTo(expectedWriteErrorDetails);

            // Assert exception messages
            var expectedWriteErrorMessage = GetExpectedWriteErrorMessage(expectedWriteErrorDetails.ToJson());
            mongoWriteExcepion.Message.Should().Be($"A write operation resulted in an error. WriteError: {expectedWriteErrorMessage}.");
            bulkWriteException.Message.Should().Be($"A bulk write operation resulted in one or more errors. WriteErrors: [ {expectedWriteErrorMessage} ].");

            // Assert writeErrors[0].errInfo
            eventCapturer.Events.Should().HaveCount(1);
            var commandSucceededEvent = (CommandSucceededEvent)eventCapturer.Events.Single();
            var writeErrors = commandSucceededEvent.Reply["writeErrors"].AsBsonArray;
            writeErrors.Values.Should().HaveCount(1);
            var errorInfo = writeErrors.Values.Single()["errInfo"].AsBsonDocument;
            errorInfo.Should().BeEquivalentTo(expectedWriteErrorDetails);

            string GetExpectedWriteErrorMessage(string expectedWriteErrorDetails)
            {
                return $"{{ Category : \"Uncategorized\", Code : 121, Message : \"Document failed validation\", Details : \"{expectedWriteErrorDetails}\" }}";
            }

            BsonDocument GetExpectedWriteErrorDetails(ObjectId objectId)
            {
                return BsonDocument.Parse($"{{ failingDocumentId : {objectId.ToJson()}, details : {{ operatorName : \"$type\", specifiedAs : {{ x : {{ $type : \"string\" }} }}, reason : \"type did not match\", consideredValue : 1, consideredType : \"int\" }} }}");
            }
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L68C8-L68C104
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_splits_batches_on_maxWriteBatchSize([Values(true, false)]bool async)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
            var maxBatchCount = DriverTestConfiguration.GetConnectionDescription().MaxBatchCount;
            var models = Enumerable
                .Range(0, maxBatchCount + 1)
                .Select(_ => new BulkWriteInsertOneModel<BsonDocument>("db.coll", new BsonDocument{ { "a", "b" } }))
                .ToArray();

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "bulkWrite");
            using var client = CreateMongoClient(eventCapturer);
            var result = async ? await client.BulkWriteAsync(models) : client.BulkWrite(models);

            result.InsertedCount.Should().Be(models.Length);
            eventCapturer.Count.Should().Be(2);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>()
                .Subject.Should().Match(c => ((CommandStartedEvent)c).Command["ops"].AsBsonArray.Count == maxBatchCount);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>()
                .Subject.Should().Match(c => ((CommandStartedEvent)c).Command["ops"].AsBsonArray.Count == 1);
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L96
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_splits_batches_on_maxMessageSizeBytes([Values(true, false)]bool async)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
            var connectionDescription = DriverTestConfiguration.GetConnectionDescription();
            var maxDocumentSize = connectionDescription.MaxDocumentSize;
            var maxMessageSize = connectionDescription.MaxMessageSize;
            var numModels = maxMessageSize / maxDocumentSize + 1;

            var models = Enumerable
                .Range(0, numModels)
                .Select(_ => new BulkWriteInsertOneModel<BsonDocument>("db.coll", new BsonDocument { { "a", new string('b', maxDocumentSize - 500) } }))
                .ToArray();

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "bulkWrite");
            using var client = CreateMongoClient(eventCapturer);
            var result = async ? await client.BulkWriteAsync(models) : client.BulkWrite(models);

            result.InsertedCount.Should().Be(numModels);
            eventCapturer.Count.Should().Be(2);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>()
                .Subject.Should().Match(c => ((CommandStartedEvent)c).Command["ops"].AsBsonArray.Count == numModels - 1);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>()
                .Subject.Should().Match(c => ((CommandStartedEvent)c).Command["ops"].AsBsonArray.Count == 1);
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L136
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_collects_WriteConcernError_across_batches([Values(true, false)]bool async)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
            var maxBatchCount = DriverTestConfiguration.GetConnectionDescription().MaxBatchCount;
            const string failPointCommand = @"
            {
              configureFailPoint: 'failCommand',
              mode: { times: 2 },
              data: {
                failCommands: ['bulkWrite'],
                writeConcernError: {
                  code: 91,
                  errmsg: 'Replication is being shut down'
                }
              }
            }";
            var models = Enumerable
                .Range(0, maxBatchCount + 1)
                .Select(_ => new BulkWriteInsertOneModel<BsonDocument>("db.coll", new BsonDocument{ { "a", "b" } }))
                .ToArray();

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "bulkWrite");
            using var client = DriverTestConfiguration.CreateMongoClient(settings =>
            {
                settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5);
                settings.LoggingSettings = LoggingSettings;
                settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                settings.RetryWrites = false;
            });

            using var failPoint = ConfigureFailPoint(failPointCommand);
            var exception = async
                ? await Record.ExceptionAsync(() => client.BulkWriteAsync(models))
                : Record.Exception(() => client.BulkWrite(models));

            var bulkWriteException = exception.Should().BeOfType<ClientBulkWriteException>().Subject;
            bulkWriteException.WriteConcernErrors.Should().HaveCount(2);
            bulkWriteException.PartialResult.Should().NotBeNull();
            bulkWriteException.PartialResult.InsertedCount.Should().Be(models.Length);

            eventCapturer.Count.Should().Be(2);
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L181
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_handles_individual_WriteError_across_batches(
            [Values(true, false)] bool async,
            [Values(true, false)] bool ordered)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
            var maxBatchCount = DriverTestConfiguration.GetConnectionDescription().MaxBatchCount;
            var model = new BsonDocument { { "_id", 1 } };
            var models = Enumerable
                .Range(0, maxBatchCount + 1)
                .Select(_ => new BulkWriteInsertOneModel<BsonDocument>("db.coll", model))
                .ToArray();
            var bulkWriteOptions = new ClientBulkWriteOptions { IsOrdered = ordered };

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "bulkWrite");
            using var client = CreateMongoClient(eventCapturer);
            var db = client.GetDatabase("db");
            db.DropCollection("coll");
            db.GetCollection<BsonDocument>("coll").InsertOne(model);

            var exception = async
                ? await Record.ExceptionAsync(() => client.BulkWriteAsync(models, bulkWriteOptions))
                : Record.Exception(() => client.BulkWrite(models, bulkWriteOptions));

            var bulkWriteException = exception.Should().BeOfType<ClientBulkWriteException>().Subject;
            bulkWriteException.WriteErrors.Should().HaveCount(ordered ? 1 : models.Length);
            bulkWriteException.PartialResult.Should().BeNull();

            eventCapturer.Count.Should().Be(ordered ? 1 : 2);
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L236
        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L274
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_handles_cursor_requiring_getMore(
            [Values(true, false)] bool async,
            [Values(true, false)] bool isInTransaction)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
            if (isInTransaction)
            {
                RequireServer.Check()
                    .ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded)
                    .Supports(Feature.Transactions);
            }

            var maxDocumentSize = DriverTestConfiguration.GetConnectionDescription().MaxDocumentSize;
            var models = new[]
            {
                new BulkWriteUpdateOneModel<BsonDocument>(
                    "db.coll",
                    Builders<BsonDocument>.Filter.Eq("_id", new string('a', maxDocumentSize / 2)),
                    Builders<BsonDocument>.Update.Set("x", 1),
                    isUpsert: true
                ),
                new BulkWriteUpdateOneModel<BsonDocument>(
                    "db.coll",
                    Builders<BsonDocument>.Filter.Eq("_id", new string('b', maxDocumentSize / 2)),
                    Builders<BsonDocument>.Update.Set("x", 1),
                    isUpsert: true
                )
            };

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>();
            using var client = CreateMongoClient(eventCapturer);
            var bulkWriteOptions = new ClientBulkWriteOptions { VerboseResult = true };

            var db = client.GetDatabase("db");
            db.DropCollection("coll");

            ClientBulkWriteResult result;
            if (isInTransaction)
            {
                var session = client.StartSession();
                session.StartTransaction();

                result = async
                    ? await client.BulkWriteAsync(session, models, bulkWriteOptions)
                    : client.BulkWrite(session, models, bulkWriteOptions);

                session.CommitTransaction();
            }
            else
            {
                result = async
                    ? await client.BulkWriteAsync(models, bulkWriteOptions)
                    : client.BulkWrite(models, bulkWriteOptions);
            }

            result.UpsertedCount.Should().Be(models.Length);
            result.UpdateResults.Should().HaveCount(models.Length);

            eventCapturer.Events.Should().Contain(e => ((CommandStartedEvent)e).CommandName == "getMore");
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L318
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_handles_getMore_error([Values(true, false)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
            var maxDocumentSize = DriverTestConfiguration.GetConnectionDescription().MaxDocumentSize;

            const string failPointCommand = @"
                {
                  configureFailPoint: 'failCommand',
                  mode: { times: 1 },
                  data: {
                    failCommands: ['getMore'],
                    errorCode: 8
                  }
                }";

            var models = new[]
            {
                new BulkWriteUpdateOneModel<BsonDocument>(
                    "db.coll",
                    Builders<BsonDocument>.Filter.Eq("_id", new string('a', maxDocumentSize / 2)),
                    Builders<BsonDocument>.Update.Set("x", 1),
                    isUpsert: true
                ),
                new BulkWriteUpdateOneModel<BsonDocument>(
                    "db.coll",
                    Builders<BsonDocument>.Filter.Eq("_id", new string('b', maxDocumentSize / 2)),
                    Builders<BsonDocument>.Update.Set("x", 1),
                    isUpsert: true
                )
            };

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>();
            using var client = CreateMongoClient(eventCapturer);
            using var failPoint = ConfigureFailPoint(failPointCommand);
            var bulkWriteOptions = new ClientBulkWriteOptions { VerboseResult = true };

            var db = client.GetDatabase("db");
            db.DropCollection("coll");

            var exception = async
                ? await Record.ExceptionAsync(() => client.BulkWriteAsync(models, bulkWriteOptions))
                : Record.Exception(() => client.BulkWrite(models, bulkWriteOptions));

            var bulkWriteException = exception.Should().BeOfType<ClientBulkWriteException>().Subject;
            bulkWriteException.InnerException.Should().BeOfType<MongoCommandException>()
                .Subject.Code.Should().Be(8);
            bulkWriteException.PartialResult.Should().NotBeNull();
            bulkWriteException.PartialResult.UpsertedCount.Should().Be(2);
            bulkWriteException.PartialResult.UpdateResults.Should().HaveCount(1);

            eventCapturer.Events.Should().Contain(e => ((CommandStartedEvent)e).CommandName == "getMore");
            eventCapturer.Events.Should().Contain(e => ((CommandStartedEvent)e).CommandName == "killCursors");
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L371
        [Theory]
        [ParameterAttributeData]
        internal async Task MongoClient_bulkWrite_returns_error_for_unacknowledged_too_large_insert(
            [Values(true, false)] bool async,
            [Values(true, false)] bool isReplace)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
            var maxDocumentSize = DriverTestConfiguration.GetConnectionDescription().MaxDocumentSize;

            var document = new BsonDocument() { { "a", new string('b', maxDocumentSize) } };
            BulkWriteModel[] models = isReplace
                    ? new[] { new BulkWriteReplaceOneModel<BsonDocument>("db.coll", Builders<BsonDocument>.Filter.Empty, document) }
                    : new[] { new BulkWriteInsertOneModel<BsonDocument>("db.coll", document) };

            using var client = CreateMongoClient(null);
            var bulkWriteOptions = new ClientBulkWriteOptions
            {
                WriteConcern = WriteConcern.Unacknowledged,
                IsOrdered = false
            };

            var exception = async
                ? await Record.ExceptionAsync(() => client.BulkWriteAsync(models, bulkWriteOptions))
                : Record.Exception(() => client.BulkWrite(models, bulkWriteOptions));

            var bulkWriteException = exception.Should().BeOfType<ClientBulkWriteException>().Subject;
            bulkWriteException.InnerException.Should().BeOfType<FormatException>();
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L422
        //
        // This test is commented out because calculations in spec does not include "$db" and "lsid" fields of bulkWrite command.
        // Have to investigate deeper the way to include that fields in calculations.
        //
        // [Theory]
        // [ParameterAttributeData]
        // internal async Task MongoClient_bulkWrite_batch_splits_on_namespace_exceeds_maximum_message_size(
        //     [Values(true, false)] bool async,
        //     [Values(true, false)] bool isBatchSplit)
        // {
        //     RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
        //     var connectionDescription = DriverTestConfiguration.GetConnectionDescription();
        //     var maxDocumentSize = connectionDescription.MaxDocumentSize;
        //     var maxMessageSize = connectionDescription.MaxMessageSize;
        //     var opsBytes = maxMessageSize - 1122;
        //     var numModels = opsBytes / maxDocumentSize;
        //     var remainderBytes = opsBytes % maxDocumentSize;
        //
        //     var models = Enumerable.Range(0, numModels)
        //         .Select(_ => new BulkWriteInsertOneModel<BsonDocument>("db.coll", new BsonDocument { { "a", new string('b', maxDocumentSize - 57) } }))
        //         .ToList();
        //
        //     if (remainderBytes >= 217)
        //     {
        //         models.Add(new BulkWriteInsertOneModel<BsonDocument>("db.coll", new BsonDocument{ { "a", new string('b', remainderBytes - 57) }}));
        //     }
        //
        //     if (isBatchSplit)
        //     {
        //         models.Add(new BulkWriteInsertOneModel<BsonDocument>($"db.{new string('c', 200)}", new BsonDocument { { "a", "b" } }));
        //     }
        //     else
        //     {
        //         models.Add(new BulkWriteInsertOneModel<BsonDocument>("db.coll", new BsonDocument { { "a", "b" } }));
        //     }
        //
        //     var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "bulkWrite");
        //     using var client = CreateMongoClient(eventCapturer);
        //
        //     var result = async ? await client.BulkWriteAsync(models) : client.BulkWrite(models);
        //
        //     result.InsertedCount.Should().Be(models.Count);
        //
        //     if (isBatchSplit)
        //     {
        //         eventCapturer.Count.Should().Be(2);
        //         eventCapturer.Next().Should().BeOfType<CommandStartedEvent>()
        //         .Subject.Command.Should().Match(
        //             c => c["ops"].AsBsonArray.Count == models.Count - 1 && c["nsInfo"].AsBsonArray.Count == 1);
        //         eventCapturer.Next().Should().BeOfType<CommandStartedEvent>()
        //             .Subject.Command.Should().Match(
        //                 c => c["ops"].AsBsonArray.Count == 1 && c["nsInfo"].AsBsonArray.Count == 1);
        //     }
        //     else
        //     {
        //         eventCapturer.Count.Should().Be(1);
        //         eventCapturer.Next().Should().BeOfType<CommandStartedEvent>()
        //             .Subject.Command.Should().Match(
        //                 c => c["ops"].AsBsonArray.Count == models.Count && c["nsInfo"].AsBsonArray.Count == 1);
        //     }
        // }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L602
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_throws_if_no_operations_can_be_added_big_document([Values(true, false)]bool async)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
            var maxMessageSize = DriverTestConfiguration.GetConnectionDescription().MaxMessageSize;

            var models = new[]
            {
                new BulkWriteInsertOneModel<BsonDocument>(
                    "db.coll",
                    new BsonDocument { { "a", new string('b', maxMessageSize) } }
                )
            };

            using var client = CreateMongoClient();

            var exception = async
                ? await Record.ExceptionAsync(() => client.BulkWriteAsync(models))
                : Record.Exception(() => client.BulkWrite(models));

            var bulkWriteException = exception.Should().BeOfType<ClientBulkWriteException>().Subject;
            bulkWriteException.InnerException.Should().BeOfType<FormatException>();
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L602
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_throws_if_no_operations_can_be_added_big_namespace([Values(true, false)]bool async)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);
            var maxMessageSize = DriverTestConfiguration.GetConnectionDescription().MaxMessageSize;

            var models = new[]
            {
                new BulkWriteInsertOneModel<BsonDocument>(
                    $"db.{new string('c', maxMessageSize)}",
                    new BsonDocument { { "a", "b" } }
                )
            };

            using var client = CreateMongoClient();

            var exception = async
                ? await Record.ExceptionAsync(() => client.BulkWriteAsync(models))
                : Record.Exception(() => client.BulkWrite(models));

            var bulkWriteException = exception.Should().BeOfType<ClientBulkWriteException>().Subject;
            bulkWriteException.InnerException.Should().BeOfType<FormatException>();
        }

        // https://github.com/mongodb/specifications/blob/7517681e6a3186cb7f3114314a9fe1bc3a747b9f/source/crud/tests/README.md?plain=1#L647
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_throws_if_auto_encryption_configured([Values(true, false)]bool async)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);

            var models = new[]
            {
                new BulkWriteInsertOneModel<BsonDocument>(
                    $"db.coll",
                    new BsonDocument { { "a", "b" } }
                )
            };

            using var client = DriverTestConfiguration.CreateMongoClient((MongoClientSettings settings) =>
                {
                    settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5);
                    var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
                    var localKey = new Dictionary<string, object>
                    {
                        { "accessKeyId", "foo"},
                        { "secretAccessKey", "bar"}
                    };
                    kmsProviders.Add("aws", localKey);

                    var keyVaultNamespace = CollectionNamespace.FromFullName("db.coll");
                    settings.AutoEncryptionOptions = new AutoEncryptionOptions(keyVaultNamespace, kmsProviders);
                    settings.LoggingSettings = LoggingSettings;
                });

            var exception = async
                ? await Record.ExceptionAsync(() => client.BulkWriteAsync(models))
                : Record.Exception(() => client.BulkWrite(models));

            exception.Should().BeOfType<NotSupportedException>();
        }

        // https://github.com/mongodb/specifications/blob/d1bdb68b7b4aec9681ea56d41c8b9a6c1a97d365/source/crud/tests/README.md?plain=1#L699
        [Theory]
        [ParameterAttributeData]
        public async Task MongoClient_bulkWrite_unacknowledged_write_concern_uses_w0_all_batches([Values(true, false)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientBulkWrite).Serverless(false);

            var connectionDescription = DriverTestConfiguration.GetConnectionDescription();
            var maxDocumentSize = connectionDescription.MaxDocumentSize;
            var maxMessageSize = connectionDescription.MaxMessageSize;
            var numModels = maxMessageSize / maxDocumentSize + 1;

            var models = Enumerable
                .Range(0, numModels)
                .Select(_ => new BulkWriteInsertOneModel<BsonDocument>("db.coll", new BsonDocument { { "a", new string('b', maxDocumentSize - 500) } }))
                .ToArray();

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "bulkWrite");
            using var client = DriverTestConfiguration.CreateMongoClient(settings =>
            {
                settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5);
                settings.LoggingSettings = LoggingSettings;
                settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                if (CoreTestConfiguration.Cluster.Description.Type == ClusterType.Sharded)
                {
                    var serverAddress = settings.Servers.First();
                    settings.Servers = new[] { serverAddress };
                    settings.DirectConnection = true;
                }
            });

            var db = client.GetDatabase("db");
            db.DropCollection("coll");
            db.CreateCollection("coll");

            var bulkWriteOptions = new ClientBulkWriteOptions
            {
                WriteConcern = WriteConcern.Unacknowledged,
                IsOrdered = false
            };
            var result = async ? await client.BulkWriteAsync(models, bulkWriteOptions) : client.BulkWrite(models, bulkWriteOptions);

            result.Acknowledged.Should().BeFalse();

            eventCapturer.Count.Should().Be(2);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>()
                .Subject.Should().Match(c => ((CommandStartedEvent)c).Command["ops"].AsBsonArray.Count == numModels - 1)
                .And.Subject.Should().Match(c => ((CommandStartedEvent)c).Command["writeConcern"]["w"] == 0);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>()
                .Subject.Should().Match(c => ((CommandStartedEvent)c).Command["ops"].AsBsonArray.Count == 1)
                .And.Subject.Should().Match(c => ((CommandStartedEvent)c).Command["writeConcern"]["w"] == 0);

            var documentCount = db.GetCollection<BsonDocument>("coll").CountDocuments(Builders<BsonDocument>.Filter.Empty);
            documentCount.Should().Be(numModels);
        }

        // private methods
        private FailPoint ConfigureFailPoint(string failpointCommand)
        {
            var cluster = DriverTestConfiguration.Client.GetClusterInternal();
            var session = NoCoreSession.NewHandle();

            return FailPoint.Configure(cluster, session, BsonDocument.Parse(failpointCommand));
        }

        private IMongoClient CreateMongoClient(EventCapturer eventCapturer = null)
        {
            return DriverTestConfiguration.CreateMongoClient((MongoClientSettings settings) =>
            {
                settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5);
                settings.LoggingSettings = LoggingSettings;
                if (eventCapturer != null)
                {
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                }
            });
        }
    }
}
