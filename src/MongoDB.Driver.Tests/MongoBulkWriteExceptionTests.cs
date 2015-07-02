/* Copyright 2010-2015 MongoDB Inc.
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
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.EqualityComparers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class MongoBulkWriteExceptionTests
    {
        private BulkWriteResult<BsonDocument> _bulkWriteResult;
        private ConnectionId _connectionId;
        private WriteConcernError _writeConcernError;
        private BulkWriteError[] _writeErrors;
        private WriteModel<BsonDocument>[] _unprocessedRequests;


        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2);
            var processedRequests = new[] { new InsertOneModel<BsonDocument>(new BsonDocument("b", 1)) };
            var upserts = new BulkWriteUpsert[0];
            _bulkWriteResult = new BulkWriteResult<BsonDocument>.Acknowledged(1, 1, 0, 0, 0, processedRequests, upserts);
            _writeConcernError = new WriteConcernError(11, "funny", new BsonDocument("c", 1));
            _writeErrors = new[] { new BulkWriteError(10, ServerErrorCategory.Uncategorized, 1, "blah", new BsonDocument("a", 1)) };
            _unprocessedRequests = new[] { new InsertOneModel<BsonDocument>(new BsonDocument("a", 1)) };
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoBulkWriteException<BsonDocument>(_connectionId, _bulkWriteResult, _writeErrors, _writeConcernError, _unprocessedRequests);

            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.Message.Should().Contain("bulk write operation");
            subject.Result.Should().BeSameAs(_bulkWriteResult);
            subject.UnprocessedRequests.Should().Equal(_unprocessedRequests);
            subject.WriteConcernError.Should().BeSameAs(_writeConcernError);
            subject.WriteErrors.Should().Equal(_writeErrors);
        }

        [Test]
        public void FromCore_should_convert_from_core_exception_with_a_write_concern_error_when_original_models_exists()
        {
            var exception = new MongoBulkWriteOperationException(
                _connectionId,
                result: new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { new InsertRequest(new BsonDocument("b", 1)) { CorrelationId = 1 } },
                    upserts: new List<BulkWriteOperationUpsert>()),
                writeErrors: new[] { new BulkWriteOperationError(10, 1, "blah", new BsonDocument("a", 1)) },
                writeConcernError: new BulkWriteConcernError(11, "funny", new BsonDocument("c", 1)),
                unprocessedRequests: new[] { new InsertRequest(new BsonDocument("a", 1)) { CorrelationId = 0 } });

            var models = new [] 
            {
                new InsertOneModel<BsonDocument>(new BsonDocument("a", 1)),
                new InsertOneModel<BsonDocument>(new BsonDocument("b", 1))
            }; 
            var mapped = MongoBulkWriteException<BsonDocument>.FromCore(exception, models);

            mapped.Result.ProcessedRequests.Count.Should().Be(1);
            mapped.Result.ProcessedRequests[0].Should().BeSameAs(models[1]);
            mapped.WriteConcernError.Should().NotBeNull();
            mapped.WriteErrors.Count.Should().Be(1);
            mapped.WriteErrors[0].Should().NotBeNull();
            mapped.UnprocessedRequests.Count.Should().Be(1);
            mapped.UnprocessedRequests[0].Should().BeSameAs(models[0]);
        }

        [Test]
        public void FromCore_should_convert_from_core_exception_with_a_write_concern_error_when_original_models_do_not_exist()
        {
            var exception = new MongoBulkWriteOperationException(
                _connectionId,
                result: new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { new InsertRequest(new BsonDocumentWrapper(new BsonDocument("b", 1))) { CorrelationId = 1 } },
                    upserts: new List<BulkWriteOperationUpsert>()),
                writeErrors: new[] { new BulkWriteOperationError(10, 1, "blah", new BsonDocument("a", 1)) },
                writeConcernError: new BulkWriteConcernError(11, "funny", new BsonDocument("c", 1)),
                unprocessedRequests: new[] { new InsertRequest(new BsonDocumentWrapper(new BsonDocument("a", 1))) { CorrelationId = 0 } });

            var mapped = MongoBulkWriteException<BsonDocument>.FromCore(exception);

            mapped.Result.ProcessedRequests.Count.Should().Be(1);
            mapped.Result.ProcessedRequests[0].Should().BeOfType<InsertOneModel<BsonDocument>>();
            ((InsertOneModel<BsonDocument>)mapped.Result.ProcessedRequests[0]).Document.Should().Be("{b:1}");
            mapped.WriteConcernError.Should().NotBeNull();
            mapped.WriteErrors.Count.Should().Be(1);
            mapped.WriteErrors[0].Should().NotBeNull();
            mapped.UnprocessedRequests.Count.Should().Be(1);
            mapped.UnprocessedRequests[0].Should().BeOfType<InsertOneModel<BsonDocument>>();
            ((InsertOneModel<BsonDocument>)mapped.UnprocessedRequests[0]).Document.Should().Be("{a:1}");
        }

        [Test]
        public void Serialization_should_work()
        {
            var subject = new MongoBulkWriteException<BsonDocument>(_connectionId, _bulkWriteResult, _writeErrors, _writeConcernError, _unprocessedRequests);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoBulkWriteException<BsonDocument>)formatter.Deserialize(stream);

                rehydrated.ConnectionId.Should().Be(subject.ConnectionId);
                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.Result.Should().BeUsing(subject.Result, EqualityComparerRegistry.Default);
                rehydrated.UnprocessedRequests.Should().EqualUsing(subject.UnprocessedRequests, EqualityComparerRegistry.Default);
                rehydrated.WriteConcernError.Should().BeUsing(subject.WriteConcernError, EqualityComparerRegistry.Default);
                rehydrated.WriteErrors.Should().EqualUsing(subject.WriteErrors, EqualityComparerRegistry.Default);
            }
        }
    }
}
