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
using System.Linq;
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
using MongoDB.Driver.TestHelpers;
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

        [Fact]
        public void WriteError_details_should_expose_writeErrors_errInfo()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo(new SemanticVersion(5, 0, 0, ""));

            var eventCapturer = new EventCapturer().Capture<CommandSucceededEvent>(e => e.CommandName == "insert");
            var collectionName = "WriteError_details_should_expose_writeErrors_errInfo";
            var collectionValidator = BsonDocument.Parse("{ x : { $type : 'string' } }");
            var collectionOptions = new CreateCollectionOptions<BsonDocument> { Validator = collectionValidator };

            Exception exception;
            using (var client = CreateDisposableClient(eventCapturer))
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

        // private methods
        private FailPoint ConfigureFailPoint(string failpointCommand)
        {
            var cluster = DriverTestConfiguration.Client.Cluster;
            var session = NoCoreSession.NewHandle();

            return FailPoint.Configure(cluster, session, BsonDocument.Parse(failpointCommand));
        }

        private DisposableMongoClient CreateDisposableClient(EventCapturer eventCapturer)
        {
            return DriverTestConfiguration.CreateDisposableClient((MongoClientSettings settings) =>
            {
                settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5);
                settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
            },
            LoggingSettings);
        }
    }
}
