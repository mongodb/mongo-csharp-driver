/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Tests;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver
{
    public class MongoCollectionImplTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(0), new DnsEndPoint("localhost", 27017)), 0);
        private MockOperationExecutor _operationExecutor;

        [SetUp]
        public void Setup()
        {
            _operationExecutor = new MockOperationExecutor();
        }

        private MongoCollectionImpl<TDocument> CreateSubject<TDocument>()
        {
            var settings = new MongoCollectionSettings();
            var dbSettings = new MongoDatabaseSettings();
            dbSettings.ApplyDefaultValues(new MongoClientSettings());
            settings.ApplyDefaultValues(dbSettings);

            return new MongoCollectionImpl<TDocument>(
                Substitute.For<IMongoDatabase>(),
                new CollectionNamespace("foo", "bar"),
                settings,
                Substitute.For<ICluster>(),
                _operationExecutor);
        }

        [Test]
        public void CollectionName_should_be_set()
        {
            var subject = CreateSubject<BsonDocument>();
            subject.CollectionNamespace.CollectionName.Should().Be("bar");
        }

        public void Database_should_be_set()
        {
            var subject = CreateSubject<BsonDateTime>();
            subject.Database.Should().NotBeNull();
        }

        [Test]
        public void Settings_should_be_set()
        {
            var subject = CreateSubject<BsonDocument>();
            subject.Settings.Should().NotBeNull();
        }

        [Test]
        public async Task AggregateAsync_should_execute_the_AggregateOperation_when_out_is_not_specified()
        {
            var stages = new PipelineStageDefinition<BsonDocument, BsonDocument>[] 
            { 
                BsonDocument.Parse("{$match: {x: 2}}")
            };
            var options = new AggregateOptions()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                MaxTime = TimeSpan.FromSeconds(3),
                UseCursor = false
            };

            var fakeCursor = NSubstitute.Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(fakeCursor);

            var subject = CreateSubject<BsonDocument>();
            await subject.AggregateAsync<BsonDocument>(stages, options, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<AggregateOperation<BsonDocument>>();
            var operation = (AggregateOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.UseCursor.Should().Be(options.UseCursor);
        }

        [Test]
        public async Task AggregateAsync_should_execute_the_AggregateToCollectionOperation_and_the_FindOperation_when_out_is_specified()
        {
            var stages = new PipelineStageDefinition<BsonDocument, BsonDocument>[] 
            { 
                BsonDocument.Parse("{$match: {x: 2}}"), 
                BsonDocument.Parse("{$out: \"funny\"}") 
            };
            var options = new AggregateOptions()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                MaxTime = TimeSpan.FromSeconds(3),
                UseCursor = false
            };

            var subject = CreateSubject<BsonDocument>();
            var result = await subject.AggregateAsync<BsonDocument>(stages, options, CancellationToken.None);

            _operationExecutor.QueuedCallCount.Should().Be(1);
            var writeCall = _operationExecutor.GetWriteCall<BsonDocument>();

            writeCall.Operation.Should().BeOfType<AggregateToCollectionOperation>();
            var writeOperation = (AggregateToCollectionOperation)writeCall.Operation;
            writeOperation.CollectionNamespace.FullName.Should().Be("foo.bar");
            writeOperation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            writeOperation.MaxTime.Should().Be(options.MaxTime);
            writeOperation.Pipeline.Should().HaveCount(2);

            var fakeCursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(fakeCursor);

            await result.MoveNextAsync(CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.funny");
            operation.AllowPartialResults.Should().BeFalse();
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Comment.Should().BeNull();
            operation.CursorType.Should().Be(Core.Operations.CursorType.NonTailable);
            operation.Filter.Should().BeNull();
            operation.Limit.Should().Be(null);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Modifiers.Should().BeNull();
            operation.NoCursorTimeout.Should().BeFalse();
            operation.Projection.Should().BeNull();
            operation.Skip.Should().Be(null);
            operation.Sort.Should().BeNull();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task BulkWriteAsync_should_execute_the_BulkMixedWriteOperation(bool isOrdered)
        {
            var requests = new WriteModel<BsonDocument>[] 
            { 
                new InsertOneModel<BsonDocument>(new BsonDocument("_id", 1).Add("a",1)),
                new DeleteManyModel<BsonDocument>(new BsonDocument("b", 1)),
                new DeleteOneModel<BsonDocument>(new BsonDocument("c", 1)),
                new ReplaceOneModel<BsonDocument>(new BsonDocument("d", 1), new BsonDocument("e", 1)),
                new ReplaceOneModel<BsonDocument>(new BsonDocument("f", 1), new BsonDocument("g", 1)) { IsUpsert = true },
                new UpdateManyModel<BsonDocument>(new BsonDocument("h", 1), new BsonDocument("$set", new BsonDocument("i", 1))),
                new UpdateManyModel<BsonDocument>(new BsonDocument("j", 1), new BsonDocument("$set", new BsonDocument("k", 1))) { IsUpsert = true },
                new UpdateOneModel<BsonDocument>(new BsonDocument("l", 1), new BsonDocument("$set", new BsonDocument("m", 1))),
                new UpdateOneModel<BsonDocument>(new BsonDocument("n", 1), new BsonDocument("$set", new BsonDocument("o", 1))) { IsUpsert = true },
            };
            var bulkOptions = new BulkWriteOptions
            {
                IsOrdered = isOrdered
            };

            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { new InsertRequest(new BsonDocument("b", 1)) });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            var result = await subject.BulkWriteAsync(requests, bulkOptions, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();

            call.Operation.Should().BeOfType<BulkMixedWriteOperation>();
            var operation = (BulkMixedWriteOperation)call.Operation;

            // I know, this is a lot of stuff in one test :(
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.IsOrdered.Should().Be(isOrdered);
            operation.Requests.Count().Should().Be(9);
            var convertedRequests = operation.Requests.ToList();

            // InsertOneModel
            convertedRequests[0].Should().BeOfType<InsertRequest>();
            convertedRequests[0].CorrelationId.Should().Be(0);
            var convertedRequest0 = (InsertRequest)convertedRequests[0];
            convertedRequest0.Document.Should().Be("{_id:1, a:1}");

            // RemoveManyModel
            convertedRequests[1].Should().BeOfType<DeleteRequest>();
            convertedRequests[1].CorrelationId.Should().Be(1);
            var convertedRequest1 = (DeleteRequest)convertedRequests[1];
            convertedRequest1.Filter.Should().Be("{b:1}");
            convertedRequest1.Limit.Should().Be(0);

            // RemoveOneModel
            convertedRequests[2].Should().BeOfType<DeleteRequest>();
            convertedRequests[2].CorrelationId.Should().Be(2);
            var convertedRequest2 = (DeleteRequest)convertedRequests[2];
            convertedRequest2.Filter.Should().Be("{c:1}");
            convertedRequest2.Limit.Should().Be(1);

            // ReplaceOneModel
            convertedRequests[3].Should().BeOfType<UpdateRequest>();
            convertedRequests[3].CorrelationId.Should().Be(3);
            var convertedRequest3 = (UpdateRequest)convertedRequests[3];
            convertedRequest3.Filter.Should().Be("{d:1}");
            convertedRequest3.Update.Should().Be("{e:1}");
            convertedRequest3.UpdateType.Should().Be(UpdateType.Replacement);
            convertedRequest3.IsMulti.Should().BeFalse();
            convertedRequest3.IsUpsert.Should().BeFalse();

            // ReplaceOneModel with upsert
            convertedRequests[4].Should().BeOfType<UpdateRequest>();
            convertedRequests[4].CorrelationId.Should().Be(4);
            var convertedRequest4 = (UpdateRequest)convertedRequests[4];
            convertedRequest4.Filter.Should().Be("{f:1}");
            convertedRequest4.Update.Should().Be("{g:1}");
            convertedRequest4.UpdateType.Should().Be(UpdateType.Replacement);
            convertedRequest4.IsMulti.Should().BeFalse();
            convertedRequest4.IsUpsert.Should().BeTrue();

            // UpdateManyModel
            convertedRequests[5].Should().BeOfType<UpdateRequest>();
            convertedRequests[5].CorrelationId.Should().Be(5);
            var convertedRequest5 = (UpdateRequest)convertedRequests[5];
            convertedRequest5.Filter.Should().Be("{h:1}");
            convertedRequest5.Update.Should().Be("{$set:{i:1}}");
            convertedRequest5.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest5.IsMulti.Should().BeTrue();
            convertedRequest5.IsUpsert.Should().BeFalse();

            // UpdateManyModel with upsert
            convertedRequests[6].Should().BeOfType<UpdateRequest>();
            convertedRequests[6].CorrelationId.Should().Be(6);
            var convertedRequest6 = (UpdateRequest)convertedRequests[6];
            convertedRequest6.Filter.Should().Be("{j:1}");
            convertedRequest6.Update.Should().Be("{$set:{k:1}}");
            convertedRequest6.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest6.IsMulti.Should().BeTrue();
            convertedRequest6.IsUpsert.Should().BeTrue();

            // UpdateOneModel
            convertedRequests[7].Should().BeOfType<UpdateRequest>();
            convertedRequests[7].CorrelationId.Should().Be(7);
            var convertedRequest7 = (UpdateRequest)convertedRequests[7];
            convertedRequest7.Filter.Should().Be("{l:1}");
            convertedRequest7.Update.Should().Be("{$set:{m:1}}");
            convertedRequest7.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest7.IsMulti.Should().BeFalse();
            convertedRequest7.IsUpsert.Should().BeFalse();

            // UpdateOneModel with upsert
            convertedRequests[8].Should().BeOfType<UpdateRequest>();
            convertedRequests[8].CorrelationId.Should().Be(8);
            var convertedRequest8 = (UpdateRequest)convertedRequests[8];
            convertedRequest8.Filter.Should().Be("{n:1}");
            convertedRequest8.Update.Should().Be("{$set:{o:1}}");
            convertedRequest8.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest8.IsMulti.Should().BeFalse();
            convertedRequest8.IsUpsert.Should().BeTrue();

            // Result
            result.Should().NotBeNull();
            result.IsAcknowledged.Should().BeFalse();
            result.RequestCount.Should().Be(9);
            result.ProcessedRequests.Should().BeEquivalentTo(requests);
            for (int i = 0; i < requests.Length; i++)
            {
                result.ProcessedRequests[i].Should().BeSameAs(requests[i]);
            }
        }

        [Test]
        public async Task CountAsync_should_execute_the_CountOperation()
        {
            var filter = new BsonDocument("x", 1);
            var options = new CountOptions
            {
                Hint = "funny",
                Limit = 10,
                MaxTime = TimeSpan.FromSeconds(20),
                Skip = 30
            };

            var subject = CreateSubject<BsonDocument>();
            await subject.CountAsync(filter, options, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<long>();

            call.Operation.Should().BeOfType<CountOperation>();
            var operation = (CountOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Filter.Should().Be(filter);
            operation.Hint.Should().Be((string)options.Hint);
            operation.Limit.Should().Be(options.Limit);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Skip.Should().Be(options.Skip);
        }

        [Test]
        public async Task DeleteManyAsync_should_execute_the_BulkMixedOperation()
        {
            var filter = new BsonDocument("a", 1);
            var expectedRequest = new DeleteRequest(filter) { CorrelationId = 0, Limit = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            await subject.DeleteManyAsync(
                filter,
                CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, call);
        }

        [Test]
        public void DeleteManyAsync_should_throw_a_WriteException_when_an_error_occurs()
        {
            var filter = new BsonDocument("a", 1);
            var expectedRequest = new DeleteRequest(filter) { CorrelationId = 0, Limit = 0 };

            var exception = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { expectedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(10, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());

            _operationExecutor.EnqueueException<BulkWriteOperationResult>(exception);

            var subject = CreateSubject<BsonDocument>();
            Action act = () => subject.DeleteManyAsync(
                    filter,
                    CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<MongoWriteException>();
        }

        [Test]
        public async Task DeleteOneAsync_should_execute_the_BulkMixedOperation()
        {
            var filter = new BsonDocument("a", 1);
            var expectedRequest = new DeleteRequest(filter) { CorrelationId = 0, Limit = 1 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            await subject.DeleteOneAsync(
                filter,
                CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, call);
        }

        [Test]
        public void DeleteOneAsync_should_throw_a_WriteException_when_an_error_occurs()
        {
            var filter = new BsonDocument("a", 1);
            var expectedRequest = new DeleteRequest(filter) { CorrelationId = 0, Limit = 1 };

            var exception = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { expectedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());

            _operationExecutor.EnqueueException<BulkWriteOperationResult>(exception);

            var subject = CreateSubject<BsonDocument>();
            Action act = () => subject.DeleteOneAsync(
                    filter,
                    CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<MongoWriteException>();
        }

        [Test]
        public async Task DistinctAsync_should_execute_the_DistinctOperation()
        {
            var fieldName = "a.b";
            var filter = new BsonDocument("x", 1);
            var options = new DistinctOptions
            {
                MaxTime = TimeSpan.FromSeconds(20),
            };

            var subject = CreateSubject<BsonDocument>();
            await subject.DistinctAsync<int>("a.b", filter, options, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<int>>();

            call.Operation.Should().BeOfType<DistinctOperation<int>>();
            var operation = (DistinctOperation<int>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.FieldName.Should().Be(fieldName);
            operation.Filter.Should().Be(filter);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Test]
        public async Task FindAsync_should_execute_the_FindOperation()
        {
            var filter = BsonDocument.Parse("{x:1}");
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var options = new FindOptions<BsonDocument, BsonDocument>
            {
                AllowPartialResults = true,
                BatchSize = 20,
                Comment = "funny",
                CursorType = CursorType.TailableAwait,
                Limit = 30,
                MaxTime = TimeSpan.FromSeconds(3),
                Modifiers = BsonDocument.Parse("{$snapshot: true}"),
                NoCursorTimeout = true,
                Projection = projection,
                Skip = 40,
                Sort = sort
            };

            var fakeCursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(fakeCursor);

            var subject = CreateSubject<BsonDocument>();
            await subject.FindAsync(filter, options, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AllowPartialResults.Should().Be(options.AllowPartialResults);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Comment.Should().Be("funny");
            operation.CursorType.Should().Be(MongoDB.Driver.Core.Operations.CursorType.TailableAwait);
            operation.Filter.Should().Be(filter);
            operation.Limit.Should().Be(options.Limit);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Modifiers.Should().Be(options.Modifiers);
            operation.NoCursorTimeout.Should().Be(options.NoCursorTimeout);
            operation.Projection.Should().Be(projection);
            operation.Skip.Should().Be(options.Skip);
            operation.Sort.Should().Be(sort);
        }

        [Test]
        public async Task FindAsync_with_an_expression_should_execute_correctly()
        {
            Expression<Func<BsonDocument, bool>> filter = doc => doc["x"] == 1;
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var options = new FindOptions<BsonDocument, BsonDocument>
            {
                AllowPartialResults = true,
                BatchSize = 20,
                Comment = "funny",
                CursorType = CursorType.TailableAwait,
                Limit = 30,
                MaxTime = TimeSpan.FromSeconds(3),
                Modifiers = BsonDocument.Parse("{$snapshot: true}"),
                NoCursorTimeout = true,
                Projection = projection,
                Skip = 40,
                Sort = sort
            };

            var fakeCursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(fakeCursor);

            var subject = CreateSubject<BsonDocument>();
            await subject.FindAsync(filter, options, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AllowPartialResults.Should().Be(options.AllowPartialResults);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Comment.Should().Be("funny");
            operation.CursorType.Should().Be(MongoDB.Driver.Core.Operations.CursorType.TailableAwait);
            operation.Filter.Should().Be(new BsonDocument("x", 1));
            operation.Limit.Should().Be(options.Limit);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Modifiers.Should().Be(options.Modifiers);
            operation.NoCursorTimeout.Should().Be(options.NoCursorTimeout);
            operation.Projection.Should().Be(projection);
            operation.Skip.Should().Be(options.Skip);
            operation.Sort.Should().Be(sort);
        }

        [Test]
        public async Task FindOneAndDelete_should_execute_the_FindOneAndDeleteOperation()
        {
            var filter = BsonDocument.Parse("{x: 1}");
            var projection = BsonDocument.Parse("{x: 1}");
            var sort = BsonDocument.Parse("{a: -1}");
            var options = new FindOneAndDeleteOptions<BsonDocument, BsonDocument>()
            {
                Projection = projection,
                Sort = sort,
                MaxTime = TimeSpan.FromSeconds(2)
            };

            var subject = CreateSubject<BsonDocument>();
            await subject.FindOneAndDeleteAsync<BsonDocument>(filter, options, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<FindOneAndDeleteOperation<BsonDocument>>();
            var operation = (FindOneAndDeleteOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Filter.Should().Be(filter);
            operation.Projection.Should().Be(projection);
            operation.Sort.Should().Be(sort);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Test]
        [TestCase(false, ReturnDocument.Before)]
        [TestCase(false, ReturnDocument.After)]
        [TestCase(true, ReturnDocument.Before)]
        [TestCase(true, ReturnDocument.After)]
        public async Task FindOneAndReplace_should_execute_the_FindOneAndReplaceOperation(bool isUpsert, ReturnDocument returnDocument)
        {
            var filter = BsonDocument.Parse("{x: 1}");
            var replacement = BsonDocument.Parse("{a: 2}");
            var projection = BsonDocument.Parse("{x: 1}");
            var sort = BsonDocument.Parse("{a: -1}");
            var options = new FindOneAndReplaceOptions<BsonDocument, BsonDocument>()
            {
                IsUpsert = isUpsert,
                Projection = projection,
                ReturnDocument = returnDocument,
                Sort = sort,
                MaxTime = TimeSpan.FromSeconds(2)
            };

            var subject = CreateSubject<BsonDocument>();
            await subject.FindOneAndReplaceAsync<BsonDocument>(filter, replacement, options, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<FindOneAndReplaceOperation<BsonDocument>>();
            var operation = (FindOneAndReplaceOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Filter.Should().Be(filter);
            operation.Replacement.Should().Be(replacement);
            operation.IsUpsert.Should().Be(isUpsert);
            operation.ReturnDocument.Should().Be((Core.Operations.ReturnDocument)returnDocument);
            operation.Projection.Should().Be(projection);
            operation.Sort.Should().Be(sort);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Test]
        [TestCase(false, ReturnDocument.Before)]
        [TestCase(false, ReturnDocument.After)]
        [TestCase(true, ReturnDocument.Before)]
        [TestCase(true, ReturnDocument.After)]
        public async Task FindOneAndUpdate_should_execute_the_FindOneAndReplaceOperation(bool isUpsert, ReturnDocument returnDocument)
        {
            var filter = BsonDocument.Parse("{x: 1}");
            var update = BsonDocument.Parse("{$set: {a: 2}}");
            var projection = BsonDocument.Parse("{x: 1}");
            var sort = BsonDocument.Parse("{a: -1}");
            var options = new FindOneAndUpdateOptions<BsonDocument, BsonDocument>()
            {
                IsUpsert = isUpsert,
                Projection = projection,
                ReturnDocument = returnDocument,
                Sort = sort,
                MaxTime = TimeSpan.FromSeconds(2)
            };

            var subject = CreateSubject<BsonDocument>();
            await subject.FindOneAndUpdateAsync<BsonDocument>(filter, update, options, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<FindOneAndUpdateOperation<BsonDocument>>();
            var operation = (FindOneAndUpdateOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Filter.Should().Be(filter);
            operation.Update.Should().Be(update);
            operation.IsUpsert.Should().Be(isUpsert);
            operation.ReturnDocument.Should().Be((Core.Operations.ReturnDocument)returnDocument);
            operation.Projection.Should().Be(projection);
            operation.Sort.Should().Be(sort);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Test]
        public async Task Indexes_CreateOneAsync_should_execute_the_CreateIndexesOperation()
        {
            var keys = new BsonDocument("x", 1);
            var weights = new BsonDocument("y", 1);
            var storageEngine = new BsonDocument("awesome", true);
            var options = new CreateIndexOptions
            {
                Background = true,
                Bits = 10,
                BucketSize = 20,
                DefaultLanguage = "en",
                ExpireAfter = TimeSpan.FromSeconds(20),
                LanguageOverride = "es",
                Max = 30,
                Min = 40,
                Name = "awesome",
                Sparse = false,
                SphereIndexVersion = 50,
                StorageEngine = storageEngine,
                TextIndexVersion = 60,
                Unique = true,
                Version = 70,
                Weights = weights
            };

            var subject = CreateSubject<BsonDocument>();
            await subject.Indexes.CreateOneAsync(keys, options, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<CreateIndexesOperation>();
            var operation = (CreateIndexesOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Requests.Count().Should().Be(1);
            var request = operation.Requests.Single();
            request.AdditionalOptions.Should().BeNull();
            request.Background.Should().Be(options.Background);
            request.Bits.Should().Be(options.Bits);
            request.BucketSize.Should().Be(options.BucketSize);
            request.DefaultLanguage.Should().Be(options.DefaultLanguage);
            request.ExpireAfter.Should().Be(options.ExpireAfter);
            request.Keys.Should().Be(keys);
            request.LanguageOverride.Should().Be(options.LanguageOverride);
            request.Max.Should().Be(options.Max);
            request.Min.Should().Be(options.Min);
            request.Name.Should().Be(options.Name);
            request.Sparse.Should().Be(options.Sparse);
            request.SphereIndexVersion.Should().Be(options.SphereIndexVersion);
            request.StorageEngine.Should().Be(storageEngine);
            request.TextIndexVersion.Should().Be(options.TextIndexVersion);
            request.Unique.Should().Be(options.Unique);
            request.Version.Should().Be(options.Version);
            request.Weights.Should().Be(weights);
            request.GetIndexName().Should().Be(options.Name);
        }

        [Test]
        public async Task Indexes_CreateManyAsync_should_execute_the_CreateIndexesOperation()
        {
            var keys = new BsonDocument("x", 1);
            var keys2 = new BsonDocument("z", 1);
            var weights = new BsonDocument("y", 1);
            var storageEngine = new BsonDocument("awesome", true);
            var options = new CreateIndexOptions
            {
                Background = true,
                Bits = 10,
                BucketSize = 20,
                DefaultLanguage = "en",
                ExpireAfter = TimeSpan.FromSeconds(20),
                LanguageOverride = "es",
                Max = 30,
                Min = 40,
                Name = "awesome",
                Sparse = false,
                SphereIndexVersion = 50,
                StorageEngine = storageEngine,
                TextIndexVersion = 60,
                Unique = true,
                Version = 70,
                Weights = weights
            };

            var first = new CreateIndexModel<BsonDocument>(keys, options);
            var second = new CreateIndexModel<BsonDocument>(keys2);

            var subject = CreateSubject<BsonDocument>();
            await subject.Indexes.CreateManyAsync(new[] { first, second }, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<CreateIndexesOperation>();
            var operation = (CreateIndexesOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Requests.Count().Should().Be(2);

            var request1 = operation.Requests.ElementAt(0);
            request1.AdditionalOptions.Should().BeNull();
            request1.Background.Should().Be(options.Background);
            request1.Bits.Should().Be(options.Bits);
            request1.BucketSize.Should().Be(options.BucketSize);
            request1.DefaultLanguage.Should().Be(options.DefaultLanguage);
            request1.ExpireAfter.Should().Be(options.ExpireAfter);
            request1.Keys.Should().Be(keys);
            request1.LanguageOverride.Should().Be(options.LanguageOverride);
            request1.Max.Should().Be(options.Max);
            request1.Min.Should().Be(options.Min);
            request1.Name.Should().Be(options.Name);
            request1.Sparse.Should().Be(options.Sparse);
            request1.SphereIndexVersion.Should().Be(options.SphereIndexVersion);
            request1.StorageEngine.Should().Be(storageEngine);
            request1.TextIndexVersion.Should().Be(options.TextIndexVersion);
            request1.Unique.Should().Be(options.Unique);
            request1.Version.Should().Be(options.Version);
            request1.Weights.Should().Be(weights);
            request1.GetIndexName().Should().Be(options.Name);

            var request2 = operation.Requests.ElementAt(1);
            request2.AdditionalOptions.Should().BeNull();
            request2.Background.Should().NotHaveValue();
            request2.Bits.Should().NotHaveValue();
            request2.BucketSize.Should().NotHaveValue();
            request2.DefaultLanguage.Should().BeNull();
            request2.ExpireAfter.Should().NotHaveValue();
            request2.Keys.Should().Be(keys2);
            request2.LanguageOverride.Should().BeNull();
            request2.Max.Should().NotHaveValue();
            request2.Min.Should().NotHaveValue(); ;
            request2.Name.Should().BeNull();
            request2.Sparse.Should().NotHaveValue(); ;
            request2.SphereIndexVersion.Should().NotHaveValue();
            request2.StorageEngine.Should().BeNull();
            request2.TextIndexVersion.Should().NotHaveValue();
            request2.Unique.Should().NotHaveValue();
            request2.Version.Should().NotHaveValue();
            request2.Weights.Should().BeNull();
            request2.GetIndexName().Should().Be("z_1");
        }

        [Test]
        public async Task Indexes_DropAllAsync_should_execute_the_DropIndexOperation()
        {
            var subject = CreateSubject<BsonDocument>();
            await subject.Indexes.DropAllAsync(CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropIndexOperation>();
            var operation = (DropIndexOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.IndexName.Should().Be("*");
        }

        [Test]
        public async Task Indexes_DropOneAsync_should_execute_the_DropIndexOperation()
        {
            var subject = CreateSubject<BsonDocument>();
            await subject.Indexes.DropOneAsync("name", CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropIndexOperation>();
            var operation = (DropIndexOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.IndexName.Should().Be("name");
        }

        [Test]
        public void Indexes_DropOneAsync_should_throw_an_exception_if_an_asterick_is_used()
        {
            var subject = CreateSubject<BsonDocument>();
            Func<Task> act = () => subject.Indexes.DropOneAsync("*", CancellationToken.None);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public async Task Indexes_ListAsync_should_execute_the_ListIndexesOperation()
        {
            var subject = CreateSubject<BsonDocument>();
            await subject.Indexes.ListAsync(CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<ListIndexesOperation>();
            var operation = (ListIndexesOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
        }

        [Test]
        public async Task InsertOneAsync_should_execute_the_BulkMixedOperation()
        {
            var document = BsonDocument.Parse("{_id:1,a:1}");
            var expectedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(1, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            await subject.InsertOneAsync(
                document,
                CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, call);
        }

        [Test]
        public void InsertOneAsync_should_throw_a_WriteException_when_an_error_occurs()
        {
            var document = BsonDocument.Parse("{_id:1,a:1}");
            var expectedRequest = new InsertRequest(document) { CorrelationId = 0 };

            var exception = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 0,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { expectedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());

            _operationExecutor.EnqueueException<BulkWriteOperationResult>(exception);

            var subject = CreateSubject<BsonDocument>();
            Action act = () => subject.InsertOneAsync(
                    document,
                    CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<MongoWriteException>();
        }

        [Test]
        public async Task InsertManyAsync_should_execute_the_BulkMixedOperation()
        {
            var documents = new[] 
            { 
                BsonDocument.Parse("{_id:1,a:1}"),
                BsonDocument.Parse("{_id:2,a:2}")
            };
            var expectedRequests = new[] 
            {
                new InsertRequest(documents[0]) { CorrelationId = 0 },
                new InsertRequest(documents[1]) { CorrelationId = 1 }
            };

            var operationResult = new BulkWriteOperationResult.Unacknowledged(2, expectedRequests);
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            await subject.InsertManyAsync(
                documents,
                null,
                CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifyWrites(expectedRequests, true, call);
        }

        [Test]
        public async Task MapReduceAsync_with_inline_output_mode_should_execute_the_MapReduceOperation()
        {
            var filter = new BsonDocument("filter", 1);
            var scope = new BsonDocument("test", 3);
            var sort = new BsonDocument("sort", 1);
            var options = new MapReduceOptions<BsonDocument, BsonDocument>
            {
                Filter = new BsonDocument("filter", 1),
                Finalize = "finalizer",
                JavaScriptMode = true,
                Limit = 10,
                MaxTime = TimeSpan.FromMinutes(2),
                OutputOptions = MapReduceOutputOptions.Inline,
                Scope = scope,
                Sort = sort,
                Verbose = true
            };
            var subject = CreateSubject<BsonDocument>();

            await subject.MapReduceAsync("map", "reduce", options);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            call.Operation.Should().BeOfType<MapReduceOperation<BsonDocument>>();
            var operation = (MapReduceOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Filter.Should().Be(filter);
            operation.FinalizeFunction.Should().Be(options.Finalize);
            operation.JavaScriptMode.Should().Be(options.JavaScriptMode);
            operation.Limit.Should().Be(options.Limit);
            operation.MapFunction.Should().Be(new BsonJavaScript("map"));
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.ReduceFunction.Should().Be(new BsonJavaScript("reduce"));
            operation.ResultSerializer.Should().Be(BsonDocumentSerializer.Instance);
            operation.Scope.Should().Be(scope);
            operation.Sort.Should().Be(sort);
            operation.Verbose.Should().Be(options.Verbose);
        }

        [Test]
        public async Task MapReduceAsync_with_collection_output_mode_should_execute_the_MapReduceOperation()
        {
            var filter = new BsonDocument("filter", 1);
            var scope = new BsonDocument("test", 3);
            var sort = new BsonDocument("sort", 1);
            var options = new MapReduceOptions<BsonDocument, BsonDocument>
            {
                Filter = new BsonDocument("filter", 1),
                Finalize = "finalizer",
                JavaScriptMode = true,
                Limit = 10,
                MaxTime = TimeSpan.FromMinutes(2),
                OutputOptions = MapReduceOutputOptions.Replace("awesome", "otherDB", true),
                Scope = scope,
                Sort = sort,
                Verbose = true
            };
            var subject = CreateSubject<BsonDocument>();

            await subject.MapReduceAsync("map", "reduce", options);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            call.Operation.Should().BeOfType<MapReduceOutputToCollectionOperation>();
            var operation = (MapReduceOutputToCollectionOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Filter.Should().Be(filter);
            operation.FinalizeFunction.Should().Be(options.Finalize);
            operation.JavaScriptMode.Should().Be(options.JavaScriptMode);
            operation.Limit.Should().Be(options.Limit);
            operation.MapFunction.Should().Be(new BsonJavaScript("map"));
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.NonAtomicOutput.Should().NotHaveValue();
            operation.OutputCollectionNamespace.Should().Be(CollectionNamespace.FromFullName("otherDB.awesome"));
            operation.OutputMode.Should().Be(Core.Operations.MapReduceOutputMode.Replace);
            operation.ReduceFunction.Should().Be(new BsonJavaScript("reduce"));
            operation.Scope.Should().Be(scope);
            operation.ShardedOutput.Should().Be(true);
            operation.Sort.Should().Be(sort);
            operation.Verbose.Should().Be(options.Verbose);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ReplaceOneAsync_should_execute_the_BulkMixedOperation(bool upsert)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var replacement = BsonDocument.Parse("{a:2}");
            var expectedRequest = new UpdateRequest(UpdateType.Replacement, filter, replacement) { CorrelationId = 0, IsUpsert = upsert, IsMulti = false };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            await subject.ReplaceOneAsync(
                filter,
                replacement,
                new UpdateOptions { IsUpsert = upsert },
                CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, call);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ReplaceOneAsync_should_throw_a_WriteException_when_an_error_occurs(bool upsert)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var replacement = BsonDocument.Parse("{a:2}");
            var expectedRequest = new UpdateRequest(UpdateType.Replacement, filter, replacement) { CorrelationId = 0, IsUpsert = upsert, IsMulti = false };
            var exception = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { expectedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());

            _operationExecutor.EnqueueException<BulkWriteOperationResult>(exception);

            var subject = CreateSubject<BsonDocument>();
            Action act = () => subject.ReplaceOneAsync(
                filter,
                replacement,
                new UpdateOptions { IsUpsert = upsert },
                CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<MongoWriteException>();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task UpdateManyAsync_should_execute_the_BulkMixedOperation(bool upsert)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var expectedRequest = new UpdateRequest(UpdateType.Update, filter, update) { CorrelationId = 0, IsUpsert = upsert, IsMulti = true };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            await subject.UpdateManyAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = upsert },
                CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, call);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void UpdateManyAsync_should_throw_a_WriteException_when_an_error_occurs(bool upsert)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var expectedRequest = new UpdateRequest(UpdateType.Update, filter, update) { CorrelationId = 0, IsUpsert = upsert, IsMulti = true };
            var exception = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { expectedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());

            _operationExecutor.EnqueueException<BulkWriteOperationResult>(exception);

            var subject = CreateSubject<BsonDocument>();
            Action act = () => subject.UpdateManyAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = upsert },
                CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<MongoWriteException>();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task UpdateOneAsync_should_execute_the_BulkMixedOperation(bool upsert)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var expectedRequest = new UpdateRequest(UpdateType.Update, filter, update) { CorrelationId = 0, IsUpsert = upsert, IsMulti = false };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            await subject.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = upsert },
                CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, call);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void UpdateOneAsync_should_throw_a_WriteException_when_an_error_occurs(bool upsert)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var expectedRequest = new UpdateRequest(UpdateType.Update, filter, update) { CorrelationId = 0, IsUpsert = upsert, IsMulti = false };
            var exception = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { expectedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());

            _operationExecutor.EnqueueException<BulkWriteOperationResult>(exception);

            var subject = CreateSubject<BsonDocument>();
            Action act = () => subject.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = upsert },
                CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<MongoWriteException>();
        }

        [Test]
        public void WithReadPreference_should_return_a_new_collection_with_the_read_preference_changed()
        {
            var subject = CreateSubject<BsonDocument>();
            var newSubject = subject.WithReadPreference(ReadPreference.Nearest);
            newSubject.Settings.ReadPreference.Should().Be(ReadPreference.Nearest);
        }

        [Test]
        public void WithWriteConcern_should_return_a_new_collection_with_the_write_concern_changed()
        {
            var subject = CreateSubject<BsonDocument>();
            var newSubject = subject.WithWriteConcern(WriteConcern.WMajority);
            newSubject.Settings.WriteConcern.Should().Be(WriteConcern.WMajority);
        }

        private static void VerifyWrites(WriteRequest[] expectedRequests, bool isOrdered, MockOperationExecutor.WriteCall<BulkWriteOperationResult> call)
        {
            call.Operation.Should().BeOfType<BulkMixedWriteOperation>();
            var operation = (BulkMixedWriteOperation)call.Operation;

            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.IsOrdered.Should().Be(isOrdered);

            var actualRequests = operation.Requests.ToList();
            actualRequests.Count.Should().Be(expectedRequests.Length);

            for (int i = 0; i < expectedRequests.Length; i++)
            {
                expectedRequests[i].ShouldBeEquivalentTo(actualRequests[i]);
            }
        }

        private static void VerifySingleWrite<TRequest>(TRequest expectedRequest, MockOperationExecutor.WriteCall<BulkWriteOperationResult> call)
            where TRequest : WriteRequest
        {
            VerifyWrites(new[] { expectedRequest }, true, call);
        }

        private class A
        {
            public int PropA = 0;
        }

        private class B : A
        {
            public int PropB = 0;
        }
    }
}
