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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Tests;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver
{
    public class MongoCollectionImplTests
    {
        private MockOperationExecutor _operationExecutor;
        private MongoCollectionImpl<BsonDocument> _subject;

        [SetUp]
        public void Setup()
        {
            var settings = new MongoCollectionSettings();
            var dbSettings = new MongoDatabaseSettings();
            dbSettings.ApplyDefaultValues(new MongoServerSettings());
            settings.ApplyDefaultValues(dbSettings);
            _operationExecutor = new MockOperationExecutor();
            _subject = new MongoCollectionImpl<BsonDocument>(
                new CollectionNamespace("foo", "bar"),
                settings,
                Substitute.For<ICluster>(),
                _operationExecutor);
        }

        [Test]
        public void CollectionName_should_be_set()
        {
            _subject.CollectionNamespace.CollectionName.Should().Be("bar");
        }

        [Test]
        public void Settings_should_be_set()
        {
            _subject.Settings.Should().NotBeNull();
        }

        [Test]
        public async Task Aggregate_should_execute_the_AggregateOperation_when_out_is_not_specified()
        {
            var pipeline = new object[] { BsonDocument.Parse("{$match: {x: 2}}") };

            var fluent = _subject.Aggregate(new AggregateOptions
                {
                    AllowDiskUse = true,
                    BatchSize = 10,
                    MaxTime = TimeSpan.FromSeconds(3),
                    UseCursor = false
                })
                .Match("{x: 2}");

            var options = fluent.Options;

            var fakeCursor = NSubstitute.Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(fakeCursor);

            var result = await fluent.ToCursorAsync(CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<AggregateOperation<BsonDocument>>();
            var operation = (AggregateOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.UseCursor.Should().Be(options.UseCursor);

            operation.Pipeline.Should().ContainInOrder(pipeline);
        }

        [Test]
        public async Task AggregateAsync_should_execute_the_AggregateOperation_when_out_is_not_specified()
        {
            var pipeline = new object[] { BsonDocument.Parse("{$match: {x: 2}}") };
            var options = new AggregateOptions<BsonDocument>()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                MaxTime = TimeSpan.FromSeconds(3),
                UseCursor = false
            };

            var fakeCursor = NSubstitute.Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(fakeCursor);

            var result = await _subject.AggregateAsync(pipeline, options, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<AggregateOperation<BsonDocument>>();
            var operation = (AggregateOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.UseCursor.Should().Be(options.UseCursor);

            operation.Pipeline.Should().ContainInOrder(pipeline);
        }

        [Test]
        public async Task AggregateAsync_should_execute_the_AggregateToCollectionOperation_and_the_FindOperation_when_out_is_specified()
        {
            var pipeline = new object[] { BsonDocument.Parse("{$match: {x: 2}}"), BsonDocument.Parse("{$out: \"funny\"}") };
            var options = new AggregateOptions<BsonDocument>()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                MaxTime = TimeSpan.FromSeconds(3),
                UseCursor = false
            };

            var result = await _subject.AggregateAsync(pipeline, options, CancellationToken.None);

            _operationExecutor.QueuedCallCount.Should().Be(1);
            var writeCall = _operationExecutor.GetWriteCall<BsonDocument>();

            writeCall.Operation.Should().BeOfType<AggregateToCollectionOperation>();
            var writeOperation = (AggregateToCollectionOperation)writeCall.Operation;
            writeOperation.CollectionNamespace.FullName.Should().Be("foo.bar");
            writeOperation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            writeOperation.MaxTime.Should().Be(options.MaxTime);
            writeOperation.Pipeline.Should().BeEquivalentTo(pipeline);

            var fakeCursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(fakeCursor);

            await result.MoveNextAsync();

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.funny");
            operation.AwaitData.Should().BeTrue();
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Comment.Should().BeNull();
            operation.Criteria.Should().BeNull();
            operation.Limit.Should().Be(null);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Modifiers.Should().BeNull();
            operation.NoCursorTimeout.Should().BeFalse();
            operation.Partial.Should().BeFalse();
            operation.Projection.Should().BeNull();
            operation.Skip.Should().Be(null);
            operation.Sort.Should().BeNull();
            operation.Tailable.Should().BeFalse();
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

            var result = await _subject.BulkWriteAsync(requests, bulkOptions, CancellationToken.None);

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
            convertedRequest1.Criteria.Should().Be("{b:1}");
            convertedRequest1.Limit.Should().Be(0);

            // RemoveOneModel
            convertedRequests[2].Should().BeOfType<DeleteRequest>();
            convertedRequests[2].CorrelationId.Should().Be(2);
            var convertedRequest2 = (DeleteRequest)convertedRequests[2];
            convertedRequest2.Criteria.Should().Be("{c:1}");
            convertedRequest2.Limit.Should().Be(1);

            // ReplaceOneModel
            convertedRequests[3].Should().BeOfType<UpdateRequest>();
            convertedRequests[3].CorrelationId.Should().Be(3);
            var convertedRequest3 = (UpdateRequest)convertedRequests[3];
            convertedRequest3.Criteria.Should().Be("{d:1}");
            convertedRequest3.Update.Should().Be("{e:1}");
            convertedRequest3.UpdateType.Should().Be(UpdateType.Replacement);
            convertedRequest3.IsMulti.Should().BeFalse();
            convertedRequest3.IsUpsert.Should().BeFalse();

            // ReplaceOneModel with upsert
            convertedRequests[4].Should().BeOfType<UpdateRequest>();
            convertedRequests[4].CorrelationId.Should().Be(4);
            var convertedRequest4 = (UpdateRequest)convertedRequests[4];
            convertedRequest4.Criteria.Should().Be("{f:1}");
            convertedRequest4.Update.Should().Be("{g:1}");
            convertedRequest4.UpdateType.Should().Be(UpdateType.Replacement);
            convertedRequest4.IsMulti.Should().BeFalse();
            convertedRequest4.IsUpsert.Should().BeTrue();

            // UpdateManyModel
            convertedRequests[5].Should().BeOfType<UpdateRequest>();
            convertedRequests[5].CorrelationId.Should().Be(5);
            var convertedRequest5 = (UpdateRequest)convertedRequests[5];
            convertedRequest5.Criteria.Should().Be("{h:1}");
            convertedRequest5.Update.Should().Be("{$set:{i:1}}");
            convertedRequest5.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest5.IsMulti.Should().BeTrue();
            convertedRequest5.IsUpsert.Should().BeFalse();

            // UpdateManyModel with upsert
            convertedRequests[6].Should().BeOfType<UpdateRequest>();
            convertedRequests[6].CorrelationId.Should().Be(6);
            var convertedRequest6 = (UpdateRequest)convertedRequests[6];
            convertedRequest6.Criteria.Should().Be("{j:1}");
            convertedRequest6.Update.Should().Be("{$set:{k:1}}");
            convertedRequest6.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest6.IsMulti.Should().BeTrue();
            convertedRequest6.IsUpsert.Should().BeTrue();

            // UpdateOneModel
            convertedRequests[7].Should().BeOfType<UpdateRequest>();
            convertedRequests[7].CorrelationId.Should().Be(7);
            var convertedRequest7 = (UpdateRequest)convertedRequests[7];
            convertedRequest7.Criteria.Should().Be("{l:1}");
            convertedRequest7.Update.Should().Be("{$set:{m:1}}");
            convertedRequest7.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest7.IsMulti.Should().BeFalse();
            convertedRequest7.IsUpsert.Should().BeFalse();

            // UpdateOneModel with upsert
            convertedRequests[8].Should().BeOfType<UpdateRequest>();
            convertedRequests[8].CorrelationId.Should().Be(8);
            var convertedRequest8 = (UpdateRequest)convertedRequests[8];
            convertedRequest8.Criteria.Should().Be("{n:1}");
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
            var criteria = new BsonDocument("x", 1);
            var options = new CountOptions
            {
                Hint = "funny",
                Limit = 10,
                MaxTime = TimeSpan.FromSeconds(20),
                Skip = 30
            };
            await _subject.CountAsync(criteria, options, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<long>();

            call.Operation.Should().BeOfType<CountOperation>();
            var operation = (CountOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Criteria.Should().Be(criteria);
            operation.Hint.Should().Be((string)options.Hint);
            operation.Limit.Should().Be(options.Limit);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Skip.Should().Be(options.Skip);
        }

        [Test]
        public async Task DeleteManyAsync_should_execute_the_BulkMixedOperation()
        {
            var criteria = new BsonDocument("a", 1);
            var expectedRequest = new DeleteRequest(criteria) { CorrelationId = 0, Limit = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            await _subject.DeleteManyAsync(
                criteria,
                CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, call);
        }

        [Test]
        public void DeleteManyAsync_should_throw_a_WriteException_when_an_error_occurs()
        {
            var criteria = new BsonDocument("a", 1);
            var expectedRequest = new DeleteRequest(criteria) { CorrelationId = 0, Limit = 0 };

            var exception = new BulkWriteOperationException(
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

            Action act = () => _subject.DeleteManyAsync(
                    criteria,
                    CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<WriteException>();
        }

        [Test]
        public async Task DeleteOneAsync_should_execute_the_BulkMixedOperation()
        {
            var criteria = new BsonDocument("a", 1);
            var expectedRequest = new DeleteRequest(criteria) { CorrelationId = 0, Limit = 1 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            await _subject.DeleteOneAsync(
                criteria,
                CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, call);
        }

        [Test]
        public void DeleteOneAsync_should_throw_a_WriteException_when_an_error_occurs()
        {
            var criteria = new BsonDocument("a", 1);
            var expectedRequest = new DeleteRequest(criteria) { CorrelationId = 0, Limit = 1 };

            var exception = new BulkWriteOperationException(
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

            Action act = () => _subject.DeleteOneAsync(
                    criteria,
                    CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<WriteException>();
        }

        [Test]
        public async Task DistinctAsync_should_execute_the_DistinctOperation()
        {
            var fieldName = "a.b";
            var criteria = new BsonDocument("x", 1);
            var options = new DistinctOptions<int>
            {
                MaxTime = TimeSpan.FromSeconds(20),
            };

            await _subject.DistinctAsync("a.b", criteria, options, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IReadOnlyList<int>>();

            call.Operation.Should().BeOfType<DistinctOperation<int>>();
            var operation = (DistinctOperation<int>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.FieldName.Should().Be(fieldName);
            operation.Criteria.Should().Be(criteria);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Test]
        public async Task FindAsync_should_execute_the_FindOperation()
        {
            var criteria = BsonDocument.Parse("{x:1}");
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var options = new FindOptions<BsonDocument>
            {
                AwaitData = false,
                BatchSize = 20,
                Comment = "funny",
                Limit = 30,
                MaxTime = TimeSpan.FromSeconds(3),
                Modifiers = BsonDocument.Parse("{$snapshot: true}"),
                NoCursorTimeout = true,
                Partial = true,
                Projection = projection,
                Skip = 40,
                Sort = sort,
                Tailable = true
            };

            var fakeCursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(fakeCursor);

            var result = await _subject.FindAsync(criteria, options, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AwaitData.Should().Be(options.AwaitData);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Comment.Should().Be("funny");
            operation.Criteria.Should().Be(criteria);
            operation.Limit.Should().Be(options.Limit);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Modifiers.Should().Be(options.Modifiers);
            operation.NoCursorTimeout.Should().Be(options.NoCursorTimeout);
            operation.Partial.Should().Be(options.Partial);
            operation.Projection.Should().Be(projection);
            operation.Skip.Should().Be(options.Skip);
            operation.Sort.Should().Be(sort);
            operation.Tailable.Should().Be(options.Tailable);
        }

        [Test]
        public async Task Find_should_execute_the_FindOperation()
        {
            var criteria = BsonDocument.Parse("{x:1}");
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var fluent = _subject.Find(criteria)
                .Projection<BsonDocument>(projection)
                .Sort(sort)
                .AwaitData(false)
                .BatchSize(20)
                .Comment("funny")
                .Limit(30)
                .MaxTime(TimeSpan.FromSeconds(3))
                .Modifiers(BsonDocument.Parse("{$snapshot: true}"))
                .NoCursorTimeout(true)
                .Partial(true)
                .Skip(40)
                .Tailable(true);
            var options = fluent.Options;

            var fakeCursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(fakeCursor);

            var result = await fluent.ToCursorAsync(CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AwaitData.Should().Be(options.AwaitData);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Comment.Should().Be("funny");
            operation.Criteria.Should().Be(criteria);
            operation.Limit.Should().Be(options.Limit);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Modifiers.Should().Be(options.Modifiers);
            operation.NoCursorTimeout.Should().Be(options.NoCursorTimeout);
            operation.Partial.Should().Be(options.Partial);
            operation.Projection.Should().Be(projection);
            operation.Skip.Should().Be(options.Skip);
            operation.Sort.Should().Be(sort);
            operation.Tailable.Should().Be(options.Tailable);
        }

        [Test]
        public async Task FindOneAndDelete_should_execute_the_FindOneAndDeleteOperation()
        {
            var criteria = BsonDocument.Parse("{x: 1}");
            var projection = BsonDocument.Parse("{x: 1}");
            var sort = BsonDocument.Parse("{a: -1}");
            var options = new FindOneAndDeleteOptions<BsonDocument>()
            {
                Projection = projection,
                Sort = sort,
                MaxTime = TimeSpan.FromSeconds(2)
            };

            await _subject.FindOneAndDeleteAsync<BsonDocument>(criteria, options, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<FindOneAndDeleteOperation<BsonDocument>>();
            var operation = (FindOneAndDeleteOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Criteria.Should().Be(criteria);
            operation.Projection.Should().Be(projection);
            operation.Sort.Should().Be(sort);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task FindOneAndReplace_should_execute_the_FindOneAndReplaceOperation(bool isUpsert, bool returnOriginal)
        {
            var criteria = BsonDocument.Parse("{x: 1}");
            var replacement = BsonDocument.Parse("{a: 2}");
            var projection = BsonDocument.Parse("{x: 1}");
            var sort = BsonDocument.Parse("{a: -1}");
            var options = new FindOneAndReplaceOptions<BsonDocument>()
            {
                IsUpsert = isUpsert,
                Projection = projection,
                ReturnOriginal = returnOriginal,
                Sort = sort,
                MaxTime = TimeSpan.FromSeconds(2)
            };

            await _subject.FindOneAndReplaceAsync<BsonDocument>(criteria, replacement, options, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<FindOneAndReplaceOperation<BsonDocument>>();
            var operation = (FindOneAndReplaceOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Criteria.Should().Be(criteria);
            operation.Replacement.Should().Be(replacement);
            operation.IsUpsert.Should().Be(isUpsert);
            operation.ReturnOriginal.Should().Be(returnOriginal);
            operation.Projection.Should().Be(projection);
            operation.Sort.Should().Be(sort);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task FindOneAndUpdate_should_execute_the_FindOneAndReplaceOperation(bool isUpsert, bool returnOriginal)
        {
            var criteria = BsonDocument.Parse("{x: 1}");
            var update = BsonDocument.Parse("{$set: {a: 2}}");
            var projection = BsonDocument.Parse("{x: 1}");
            var sort = BsonDocument.Parse("{a: -1}");
            var options = new FindOneAndUpdateOptions<BsonDocument>()
            {
                IsUpsert = isUpsert,
                Projection = projection,
                ReturnOriginal = returnOriginal,
                Sort = sort,
                MaxTime = TimeSpan.FromSeconds(2)
            };

            await _subject.FindOneAndUpdateAsync<BsonDocument>(criteria, update, options, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<FindOneAndUpdateOperation<BsonDocument>>();
            var operation = (FindOneAndUpdateOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Criteria.Should().Be(criteria);
            operation.Update.Should().Be(update);
            operation.IsUpsert.Should().Be(isUpsert);
            operation.ReturnOriginal.Should().Be(returnOriginal);
            operation.Projection.Should().Be(projection);
            operation.Sort.Should().Be(sort);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Test]
        public async Task InsertOneAsync_should_execute_the_BulkMixedOperation()
        {
            var document = BsonDocument.Parse("{_id:1,a:1}");
            var expectedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(1, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            await _subject.InsertOneAsync(
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

            var exception = new BulkWriteOperationException(
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

            Action act = () => _subject.InsertOneAsync(
                    document,
                    CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<WriteException>();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ReplaceOneAsync_should_execute_the_BulkMixedOperation(bool upsert)
        {
            var criteria = BsonDocument.Parse("{a:1}");
            var replacement = BsonDocument.Parse("{a:2}");
            var expectedRequest = new UpdateRequest(UpdateType.Replacement, criteria, replacement) { CorrelationId = 0, IsUpsert = upsert, IsMulti = false };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            await _subject.ReplaceOneAsync(
                criteria,
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
            var criteria = BsonDocument.Parse("{a:1}");
            var replacement = BsonDocument.Parse("{a:2}");
            var expectedRequest = new UpdateRequest(UpdateType.Replacement, criteria, replacement) { CorrelationId = 0, IsUpsert = upsert, IsMulti = false };
            var exception = new BulkWriteOperationException(
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

            Action act = () => _subject.ReplaceOneAsync(
                criteria,
                replacement,
                new UpdateOptions { IsUpsert = upsert },
                CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<WriteException>();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task UpdateManyAsync_should_execute_the_BulkMixedOperation(bool upsert)
        {
            var criteria = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var expectedRequest = new UpdateRequest(UpdateType.Update, criteria, update) { CorrelationId = 0, IsUpsert = upsert, IsMulti = true };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            await _subject.UpdateManyAsync(
                criteria,
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
            var criteria = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var expectedRequest = new UpdateRequest(UpdateType.Update, criteria, update) { CorrelationId = 0, IsUpsert = upsert, IsMulti = true };
            var exception = new BulkWriteOperationException(
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

            Action act = () => _subject.UpdateManyAsync(
                criteria,
                update,
                new UpdateOptions { IsUpsert = upsert },
                CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<WriteException>();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task UpdateOneAsync_should_execute_the_BulkMixedOperation(bool upsert)
        {
            var criteria = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var expectedRequest = new UpdateRequest(UpdateType.Update, criteria, update) { CorrelationId = 0, IsUpsert = upsert, IsMulti = false };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            await _subject.UpdateOneAsync(
                criteria,
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
            var criteria = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var expectedRequest = new UpdateRequest(UpdateType.Update, criteria, update) { CorrelationId = 0, IsUpsert = upsert, IsMulti = false };
            var exception = new BulkWriteOperationException(
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

            Action act = () => _subject.UpdateOneAsync(
                criteria,
                update,
                new UpdateOptions { IsUpsert = upsert },
                CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<WriteException>();
        }

        private static void VerifySingleWrite<TRequest>(TRequest expectedRequest, MockOperationExecutor.WriteCall<BulkWriteOperationResult> call)
        {
            call.Operation.Should().BeOfType<BulkMixedWriteOperation>();
            var operation = (BulkMixedWriteOperation)call.Operation;

            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.IsOrdered.Should().BeTrue();
            operation.Requests.Count().Should().Be(1);
            operation.Requests.Single().Should().BeOfType<TRequest>();

            operation.Requests.Single().ShouldBeEquivalentTo(expectedRequest);
        }
    }
}
