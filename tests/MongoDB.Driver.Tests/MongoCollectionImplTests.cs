/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Tests;
using Moq;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoCollectionImplTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(0), new DnsEndPoint("localhost", 27017)), 0);
        private readonly ReadConcern _readConcern = ReadConcern.Majority;
        private MockOperationExecutor _operationExecutor;

        public MongoCollectionImplTests()
        {
            _operationExecutor = new MockOperationExecutor();
        }

        private MongoCollectionImpl<TDocument> CreateSubject<TDocument>(MongoCollectionSettings settings = null)
        {
            settings = settings ?? new MongoCollectionSettings();
            settings.ReadConcern = _readConcern;
            var dbSettings = new MongoDatabaseSettings();
            dbSettings.ApplyDefaultValues(new MongoClientSettings());
            settings.ApplyDefaultValues(dbSettings);

            return new MongoCollectionImpl<TDocument>(
                new Mock<IMongoDatabase>().Object,
                new CollectionNamespace("foo", "bar"),
                settings,
                new Mock<ICluster>().Object,
                _operationExecutor);
        }

        [Fact]
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

        [Fact]
        public void Settings_should_be_set()
        {
            var subject = CreateSubject<BsonDocument>();
            subject.Settings.Should().NotBeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_should_execute_the_AggregateOperation_when_out_is_not_specified(
            [Values(false, true)] bool async)
        {
            var stages = new PipelineStageDefinition<BsonDocument, BsonDocument>[]
            {
                BsonDocument.Parse("{$match: {x: 2}}")
            };
            var options = new AggregateOptions()
            {
                AllowDiskUse = true,
                Collation = new Collation("en_US"),
                BatchSize = 10,
                MaxTime = TimeSpan.FromSeconds(3),
                UseCursor = false
            };

            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(mockCursor.Object);

            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.AggregateAsync<BsonDocument>(stages, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Aggregate<BsonDocument>(stages, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<AggregateOperation<BsonDocument>>();
            var operation = (AggregateOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.ReadConcern.Should().Be(_readConcern);
            operation.UseCursor.Should().Be(options.UseCursor);
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_should_execute_the_AggregateToCollectionOperation_and_the_FindOperation_when_out_is_specified(
            [Values(false, true)] bool async)
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
                BypassDocumentValidation = true,
                Collation = new Collation("en_US"),
                MaxTime = TimeSpan.FromSeconds(3),
                UseCursor = false,
            };
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);

            IAsyncCursor<BsonDocument> result;
            if (async)
            {
                result = subject.AggregateAsync<BsonDocument>(stages, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.Aggregate<BsonDocument>(stages, options, CancellationToken.None);
            }

            _operationExecutor.QueuedCallCount.Should().Be(1);
            var writeCall = _operationExecutor.GetWriteCall<BsonDocument>();

            writeCall.Operation.Should().BeOfType<AggregateToCollectionOperation>();
            var writeOperation = (AggregateToCollectionOperation)writeCall.Operation;
            writeOperation.Collation.Should().BeSameAs(options.Collation);
            writeOperation.CollectionNamespace.FullName.Should().Be("foo.bar");
            writeOperation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            writeOperation.BypassDocumentValidation.Should().Be(options.BypassDocumentValidation);
            writeOperation.MaxTime.Should().Be(options.MaxTime);
            writeOperation.Pipeline.Should().HaveCount(2);
            writeOperation.WriteConcern.Should().BeSameAs(writeConcern);

            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(mockCursor.Object);

            if (async)
            {
                result.MoveNextAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result.MoveNext(CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.funny");
            operation.AllowPartialResults.Should().NotHaveValue();
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.Comment.Should().BeNull();
            operation.CursorType.Should().Be(Core.Operations.CursorType.NonTailable);
            operation.Filter.Should().BeNull();
            operation.Limit.Should().Be(null);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Modifiers.Should().BeNull();
            operation.NoCursorTimeout.Should().NotHaveValue();
            operation.OplogReplay.Should().NotHaveValue();
            operation.Projection.Should().BeNull();
            operation.Skip.Should().Be(null);
            operation.Sort.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void BulkWrite_should_execute_the_BulkMixedWriteOperation(
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isOrdered,
            [Values(false, true)] bool async)
        {
            var collation = new Collation("en_US");
            var requests = new WriteModel<BsonDocument>[]
            {
                new InsertOneModel<BsonDocument>(new BsonDocument("_id", 1).Add("a",1)),
                new DeleteManyModel<BsonDocument>(new BsonDocument("b", 1)) { Collation = collation },
                new DeleteOneModel<BsonDocument>(new BsonDocument("c", 1)) { Collation = collation },
                new ReplaceOneModel<BsonDocument>(new BsonDocument("d", 1), new BsonDocument("e", 1)) { Collation = collation },
                new ReplaceOneModel<BsonDocument>(new BsonDocument("f", 1), new BsonDocument("g", 1)) { Collation = collation, IsUpsert = true },
                new UpdateManyModel<BsonDocument>(new BsonDocument("h", 1), new BsonDocument("$set", new BsonDocument("i", 1))) { Collation = collation },
                new UpdateManyModel<BsonDocument>(new BsonDocument("j", 1), new BsonDocument("$set", new BsonDocument("k", 1))) { Collation = collation, IsUpsert = true },
                new UpdateOneModel<BsonDocument>(new BsonDocument("l", 1), new BsonDocument("$set", new BsonDocument("m", 1))) { Collation = collation },
                new UpdateOneModel<BsonDocument>(new BsonDocument("n", 1), new BsonDocument("$set", new BsonDocument("o", 1))) { Collation = collation, IsUpsert = true },
            };
            var bulkOptions = new BulkWriteOptions
            {
                BypassDocumentValidation = bypassDocumentValidation,
                IsOrdered = isOrdered
            };

            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { new InsertRequest(new BsonDocument("b", 1)) });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();

            BulkWriteResult<BsonDocument> result;
            if (async)
            {
                result = subject.BulkWriteAsync(requests, bulkOptions, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.BulkWrite(requests, bulkOptions, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();

            call.Operation.Should().BeOfType<BulkMixedWriteOperation>();
            var operation = (BulkMixedWriteOperation)call.Operation;

            // I know, this is a lot of stuff in one test :(
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.BypassDocumentValidation.Should().Be(bypassDocumentValidation);
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
            convertedRequest1.Collation.Should().BeSameAs(collation);
            convertedRequest1.Filter.Should().Be("{b:1}");
            convertedRequest1.Limit.Should().Be(0);

            // RemoveOneModel
            convertedRequests[2].Should().BeOfType<DeleteRequest>();
            convertedRequests[2].CorrelationId.Should().Be(2);
            var convertedRequest2 = (DeleteRequest)convertedRequests[2];
            convertedRequest2.Collation.Should().BeSameAs(collation);
            convertedRequest2.Filter.Should().Be("{c:1}");
            convertedRequest2.Limit.Should().Be(1);

            // ReplaceOneModel
            convertedRequests[3].Should().BeOfType<UpdateRequest>();
            convertedRequests[3].CorrelationId.Should().Be(3);
            var convertedRequest3 = (UpdateRequest)convertedRequests[3];
            convertedRequest3.Collation.Should().BeSameAs(collation);
            convertedRequest3.Filter.Should().Be("{d:1}");
            convertedRequest3.Update.Should().Be("{e:1}");
            convertedRequest3.UpdateType.Should().Be(UpdateType.Replacement);
            convertedRequest3.IsMulti.Should().BeFalse();
            convertedRequest3.IsUpsert.Should().BeFalse();

            // ReplaceOneModel with upsert
            convertedRequests[4].Should().BeOfType<UpdateRequest>();
            convertedRequests[4].CorrelationId.Should().Be(4);
            var convertedRequest4 = (UpdateRequest)convertedRequests[4];
            convertedRequest4.Collation.Should().BeSameAs(collation);
            convertedRequest4.Filter.Should().Be("{f:1}");
            convertedRequest4.Update.Should().Be("{g:1}");
            convertedRequest4.UpdateType.Should().Be(UpdateType.Replacement);
            convertedRequest4.IsMulti.Should().BeFalse();
            convertedRequest4.IsUpsert.Should().BeTrue();

            // UpdateManyModel
            convertedRequests[5].Should().BeOfType<UpdateRequest>();
            convertedRequests[5].CorrelationId.Should().Be(5);
            var convertedRequest5 = (UpdateRequest)convertedRequests[5];
            convertedRequest5.Collation.Should().BeSameAs(collation);
            convertedRequest5.Filter.Should().Be("{h:1}");
            convertedRequest5.Update.Should().Be("{$set:{i:1}}");
            convertedRequest5.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest5.IsMulti.Should().BeTrue();
            convertedRequest5.IsUpsert.Should().BeFalse();

            // UpdateManyModel with upsert
            convertedRequests[6].Should().BeOfType<UpdateRequest>();
            convertedRequests[6].CorrelationId.Should().Be(6);
            var convertedRequest6 = (UpdateRequest)convertedRequests[6];
            convertedRequest6.Collation.Should().BeSameAs(collation);
            convertedRequest6.Filter.Should().Be("{j:1}");
            convertedRequest6.Update.Should().Be("{$set:{k:1}}");
            convertedRequest6.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest6.IsMulti.Should().BeTrue();
            convertedRequest6.IsUpsert.Should().BeTrue();

            // UpdateOneModel
            convertedRequests[7].Should().BeOfType<UpdateRequest>();
            convertedRequests[7].CorrelationId.Should().Be(7);
            var convertedRequest7 = (UpdateRequest)convertedRequests[7];
            convertedRequest7.Collation.Should().BeSameAs(collation);
            convertedRequest7.Filter.Should().Be("{l:1}");
            convertedRequest7.Update.Should().Be("{$set:{m:1}}");
            convertedRequest7.UpdateType.Should().Be(UpdateType.Update);
            convertedRequest7.IsMulti.Should().BeFalse();
            convertedRequest7.IsUpsert.Should().BeFalse();

            // UpdateOneModel with upsert
            convertedRequests[8].Should().BeOfType<UpdateRequest>();
            convertedRequests[8].CorrelationId.Should().Be(8);
            var convertedRequest8 = (UpdateRequest)convertedRequests[8];
            convertedRequest8.Collation.Should().BeSameAs(collation);
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

        [Theory]
        [ParameterAttributeData]
        public void Count_should_execute_the_CountOperation(
            [Values(false, true)] bool async)
        {
            var filter = new BsonDocument("x", 1);
            var options = new CountOptions
            {
                Collation = new Collation("en_US"),
                Hint = "funny",
                Limit = 10,
                MaxTime = TimeSpan.FromSeconds(20),
                Skip = 30
            };

            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.CountAsync(filter, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Count(filter, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<long>();
            
            call.Operation.Should().BeOfType<CountOperation>();
            var operation = (CountOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.Filter.Should().Be(filter);
            operation.Hint.Should().Be(options.Hint);
            operation.Limit.Should().Be(options.Limit);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.ReadConcern.Should().Be(_readConcern);
            operation.Skip.Should().Be(options.Skip);
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteMany_should_execute_the_BulkMixedOperation(
            [Values(false, true)] bool async)
        {
            var filter = new BsonDocument("a", 1);
            var collation = new Collation("en_US");
            var expectedRequest = new DeleteRequest(filter) { Collation = collation, CorrelationId = 0, Limit = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();

            var options = new DeleteOptions { Collation = collation };
            if (async)
            {
                subject.DeleteManyAsync(filter, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.DeleteMany(filter, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, null, true, call);
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteMany_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool async)
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

            Action action;
            if (async)
            {
                action = () => subject.DeleteManyAsync(filter, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.DeleteMany(filter, CancellationToken.None);
            }

            action.ShouldThrow<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteOne_should_execute_the_BulkMixedOperation(
            [Values(false, true)] bool async)
        {
            var filter = new BsonDocument("a", 1);
            var collation = new Collation("en_US");
            var expectedRequest = new DeleteRequest(filter) { Collation = collation, CorrelationId = 0, Limit = 1 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();

            var options = new DeleteOptions { Collation = collation };
            if (async)
            {
                subject.DeleteOneAsync(filter, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.DeleteOne(filter, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, null, true, call);
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteOne_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool async)
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

            Action action;
            if (async)
            {
                action = () => subject.DeleteOneAsync(filter, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.DeleteOne(filter, CancellationToken.None);
            }

            action.ShouldThrow<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Distinct_should_execute_the_DistinctOperation(
            [Values(false, true)] bool async)
        {
            var fieldName = "a.b";
            var filter = new BsonDocument("x", 1);
            var options = new DistinctOptions
            {
                Collation = new Collation("en_US"),
                MaxTime = TimeSpan.FromSeconds(20)
            };

            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.DistinctAsync<int>("a.b", filter, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Distinct<int>("a.b", filter, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<int>>();

            call.Operation.Should().BeOfType<DistinctOperation<int>>();
            var operation = (DistinctOperation<int>)call.Operation;
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.FieldName.Should().Be(fieldName);
            operation.Filter.Should().Be(filter);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.ReadConcern.Should().Be(_readConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_should_execute_the_FindOperation(
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{x:1}");
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var options = new FindOptions<BsonDocument, BsonDocument>
            {
                AllowPartialResults = true,
                BatchSize = 20,
                Collation = new Collation("en_US"),
                Comment = "funny",
                CursorType = CursorType.TailableAwait,
                Limit = 30,
                MaxAwaitTime = TimeSpan.FromSeconds(4),
                MaxTime = TimeSpan.FromSeconds(3),
                Modifiers = BsonDocument.Parse("{$snapshot: true}"),
                NoCursorTimeout = true,
                OplogReplay = true,
                Projection = projection,
                Skip = 40,
                Sort = sort
            };

            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(mockCursor.Object);

            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.FindAsync(filter, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindSync(filter, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AllowPartialResults.Should().Be(options.AllowPartialResults);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.Comment.Should().Be("funny");
            operation.CursorType.Should().Be(MongoDB.Driver.Core.Operations.CursorType.TailableAwait);
            operation.Filter.Should().Be(filter);
            operation.Limit.Should().Be(options.Limit);
            operation.MaxAwaitTime.Should().Be(options.MaxAwaitTime);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Modifiers.Should().Be(options.Modifiers);
            operation.NoCursorTimeout.Should().Be(options.NoCursorTimeout);
            operation.OplogReplay.Should().Be(options.OplogReplay);
            operation.Projection.Should().Be(projection);
            operation.ReadConcern.Should().Be(_readConcern);
            operation.Skip.Should().Be(options.Skip);
            operation.Sort.Should().Be(sort);
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_with_an_expression_should_execute_correctly(
            [Values(false, true)] bool async)
        {
            Expression<Func<BsonDocument, bool>> filter = doc => doc["x"] == 1;
            var projection = BsonDocument.Parse("{y:1}");
            var sort = BsonDocument.Parse("{a:1}");
            var options = new FindOptions<BsonDocument, BsonDocument>
            {
                AllowPartialResults = true,
                BatchSize = 20,
                Collation = new Collation("en_US"),
                Comment = "funny",
                CursorType = CursorType.TailableAwait,
                Limit = 30,
                MaxAwaitTime = TimeSpan.FromSeconds(4),
                MaxTime = TimeSpan.FromSeconds(3),
                Modifiers = BsonDocument.Parse("{$snapshot: true}"),
                NoCursorTimeout = true,
                OplogReplay = true,
                Projection = projection,
                Skip = 40,
                Sort = sort
            };

            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(mockCursor.Object);

            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.FindAsync(filter, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindSync(filter, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.AllowPartialResults.Should().Be(options.AllowPartialResults);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Comment.Should().Be("funny");
            operation.CursorType.Should().Be(MongoDB.Driver.Core.Operations.CursorType.TailableAwait);
            operation.Filter.Should().Be(new BsonDocument("x", 1));
            operation.Limit.Should().Be(options.Limit);
            operation.MaxAwaitTime.Should().Be(options.MaxAwaitTime);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Modifiers.Should().Be(options.Modifiers);
            operation.NoCursorTimeout.Should().Be(options.NoCursorTimeout);
            operation.OplogReplay.Should().Be(options.OplogReplay);
            operation.Projection.Should().Be(projection);
            operation.ReadConcern.Should().Be(_readConcern);
            operation.Skip.Should().Be(options.Skip);
            operation.Sort.Should().Be(sort);
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_with_Projection_As_should_execute_correctly(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<A>();
            var filter = new BsonDocument();
            var options = new FindOptions<A, BsonDocument>
            {
                Projection = Builders<A>.Projection.As<BsonDocument>()
            };

            if (async)
            {
                subject.FindAsync(filter, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindSync(filter, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            call.Operation.Should().BeOfType<FindOperation<BsonDocument>>();

            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.Projection.Should().BeNull();
            operation.ResultSerializer.Should().BeOfType<BsonDocumentSerializer>();
            operation.ReadConcern.Should().Be(_readConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndDelete_should_execute_the_FindOneAndDeleteOperation(
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{x: 1}");
            var projection = BsonDocument.Parse("{x: 1}");
            var sort = BsonDocument.Parse("{a: -1}");
            var options = new FindOneAndDeleteOptions<BsonDocument, BsonDocument>()
            {
                Collation = new Collation("en_US"),
                Projection = projection,
                Sort = sort,
                MaxTime = TimeSpan.FromSeconds(2)
            };

            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.FindOneAndDeleteAsync<BsonDocument>(filter, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindOneAndDelete<BsonDocument>(filter, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<FindOneAndDeleteOperation<BsonDocument>>();
            var operation = (FindOneAndDeleteOperation<BsonDocument>)call.Operation;
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Filter.Should().Be(filter);
            operation.Projection.Should().Be(projection);
            operation.Sort.Should().Be(sort);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndDelete_with_Projection_As_should_execute_correctly(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<A>();
            var filter = new BsonDocument();
            var options = new FindOneAndDeleteOptions<A, BsonDocument>
            {
                Collation = new Collation("en_US"),
                Projection = Builders<A>.Projection.As<BsonDocument>()
            };

            if (async)
            {
                subject.FindOneAndDeleteAsync(filter, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindOneAndDelete(filter, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            call.Operation.Should().BeOfType<FindOneAndDeleteOperation<BsonDocument>>();

            var operation = (FindOneAndDeleteOperation<BsonDocument>)call.Operation;
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.Projection.Should().BeNull();
            operation.ResultSerializer.Should().BeOfType<FindAndModifyValueDeserializer<BsonDocument>>();
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndReplace_should_execute_the_FindOneAndReplaceOperation(
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(ReturnDocument.After, ReturnDocument.Before)] ReturnDocument returnDocument,
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{x: 1}");
            var replacement = BsonDocument.Parse("{a: 2}");
            var projection = BsonDocument.Parse("{x: 1}");
            var sort = BsonDocument.Parse("{a: -1}");
            var options = new FindOneAndReplaceOptions<BsonDocument, BsonDocument>()
            {
                BypassDocumentValidation = bypassDocumentValidation,
                Collation = new Collation("en_US"),
                IsUpsert = isUpsert,
                Projection = projection,
                ReturnDocument = returnDocument,
                Sort = sort,
                MaxTime = TimeSpan.FromSeconds(2)
            };

            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.FindOneAndReplaceAsync<BsonDocument>(filter, replacement, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindOneAndReplace<BsonDocument>(filter, replacement, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<FindOneAndReplaceOperation<BsonDocument>>();
            var operation = (FindOneAndReplaceOperation<BsonDocument>)call.Operation;
            operation.BypassDocumentValidation.Should().Be(bypassDocumentValidation);
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Filter.Should().Be(filter);
            operation.Replacement.Should().Be(replacement);
            operation.IsUpsert.Should().Be(isUpsert);
            operation.ReturnDocument.Should().Be((Core.Operations.ReturnDocument)returnDocument);
            operation.Projection.Should().Be(projection);
            operation.Sort.Should().Be(sort);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndReplace_with_Projection_As_should_execute_correctly(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<A>();
            var filter = new BsonDocument();
            var replacement = new A();
            var options = new FindOneAndReplaceOptions<A, BsonDocument>
            {
                Collation = new Collation("en_US"),
                Projection = Builders<A>.Projection.As<BsonDocument>()
            };

            if (async)
            {
                subject.FindOneAndReplaceAsync(filter, replacement, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindOneAndReplace(filter, replacement, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            call.Operation.Should().BeOfType<FindOneAndReplaceOperation<BsonDocument>>();

            var operation = (FindOneAndReplaceOperation<BsonDocument>)call.Operation;
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.Projection.Should().BeNull();
            operation.ResultSerializer.Should().BeOfType<FindAndModifyValueDeserializer<BsonDocument>>();
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndUpdate_should_execute_the_FindOneAndUpdateOperation(
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(ReturnDocument.After, ReturnDocument.Before)] ReturnDocument returnDocument,
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{x: 1}");
            var update = BsonDocument.Parse("{$set: {a: 2}}");
            var projection = BsonDocument.Parse("{x: 1}");
            var sort = BsonDocument.Parse("{a: -1}");
            var options = new FindOneAndUpdateOptions<BsonDocument, BsonDocument>()
            {
                BypassDocumentValidation = bypassDocumentValidation,
                Collation = new Collation("en_US"),
                IsUpsert = isUpsert,
                Projection = projection,
                ReturnDocument = returnDocument,
                Sort = sort,
                MaxTime = TimeSpan.FromSeconds(2)
            };

            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.FindOneAndUpdateAsync<BsonDocument>(filter, update, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindOneAndUpdate<BsonDocument>(filter, update, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<FindOneAndUpdateOperation<BsonDocument>>();
            var operation = (FindOneAndUpdateOperation<BsonDocument>)call.Operation;
            operation.BypassDocumentValidation.Should().Be(bypassDocumentValidation);
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Filter.Should().Be(filter);
            operation.Update.Should().Be(update);
            operation.IsUpsert.Should().Be(isUpsert);
            operation.ReturnDocument.Should().Be((Core.Operations.ReturnDocument)returnDocument);
            operation.Projection.Should().Be(projection);
            operation.Sort.Should().Be(sort);
            operation.MaxTime.Should().Be(options.MaxTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndUpdate_with_Projection_As_should_execute_correctly(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<A>();
            var filter = new BsonDocument();
            var update = Builders<A>.Update.Inc(x => x.PropA, 1);
            var options = new FindOneAndUpdateOptions<A, BsonDocument>
            {
                Projection = Builders<A>.Projection.As<BsonDocument>()
            };

            if (async)
            {
                subject.FindOneAndUpdateAsync(filter, update, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.FindOneAndUpdate(filter, update, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            call.Operation.Should().BeOfType<FindOneAndUpdateOperation<BsonDocument>>();

            var operation = (FindOneAndUpdateOperation<BsonDocument>)call.Operation;
            operation.Projection.Should().BeNull();
            operation.ResultSerializer.Should().BeOfType<FindAndModifyValueDeserializer<BsonDocument>>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_CreateOne_should_execute_the_CreateIndexesOperation(
            [Values(false, true)] bool async)
        {
            var keys = new BsonDocument("x", 1);
            var partialFilterExpression = Builders<BsonDocument>.Filter.Gt("x", 0);
            var renderedPartialFilterExpression = new BsonDocument("x", new BsonDocument("$gt", 0));
            var weights = new BsonDocument("y", 1);
            var storageEngine = new BsonDocument("awesome", true);
            var options = new CreateIndexOptions<BsonDocument>
            {
                Background = true,
                Bits = 10,
                BucketSize = 20,
                Collation = new Collation("en_US"),
                DefaultLanguage = "en",
                ExpireAfter = TimeSpan.FromSeconds(20),
                LanguageOverride = "es",
                Max = 30,
                Min = 40,
                Name = "awesome",
                PartialFilterExpression = partialFilterExpression,
                Sparse = false,
                SphereIndexVersion = 50,
                StorageEngine = storageEngine,
                TextIndexVersion = 60,
                Unique = true,
                Version = 70,
                Weights = weights
            };
            var writeConcern = new WriteConcern(1);

            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);

            if (async)
            {
                subject.Indexes.CreateOneAsync(keys, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Indexes.CreateOne(keys, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<CreateIndexesOperation>();
            var operation = (CreateIndexesOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Requests.Count().Should().Be(1);
            operation.WriteConcern.Should().BeSameAs(writeConcern);
            var request = operation.Requests.Single();
            request.AdditionalOptions.Should().BeNull();
            request.Background.Should().Be(options.Background);
            request.Bits.Should().Be(options.Bits);
            request.BucketSize.Should().Be(options.BucketSize);
            request.Collation.Should().BeSameAs(options.Collation);
            request.DefaultLanguage.Should().Be(options.DefaultLanguage);
            request.ExpireAfter.Should().Be(options.ExpireAfter);
            request.Keys.Should().Be(keys);
            request.LanguageOverride.Should().Be(options.LanguageOverride);
            request.Max.Should().Be(options.Max);
            request.Min.Should().Be(options.Min);
            request.Name.Should().Be(options.Name);
            request.PartialFilterExpression.Should().Be(renderedPartialFilterExpression);
            request.Sparse.Should().Be(options.Sparse);
            request.SphereIndexVersion.Should().Be(options.SphereIndexVersion);
            request.StorageEngine.Should().Be(storageEngine);
            request.TextIndexVersion.Should().Be(options.TextIndexVersion);
            request.Unique.Should().Be(options.Unique);
            request.Version.Should().Be(options.Version);
            request.Weights.Should().Be(weights);
            request.GetIndexName().Should().Be(options.Name);
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_CreateMany_should_execute_the_CreateIndexesOperation(
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);
            var keys = new BsonDocument("x", 1);
            var keys2 = new BsonDocument("z", 1);
            var partialFilterExpression = Builders<BsonDocument>.Filter.Gt("x", 0);
            var renderedPartialFilterExpression = new BsonDocument("x", new BsonDocument("$gt", 0));
            var weights = new BsonDocument("y", 1);
            var storageEngine = new BsonDocument("awesome", true);
            var options = new CreateIndexOptions<BsonDocument>
            {
                Background = true,
                Bits = 10,
                BucketSize = 20,
                Collation = new Collation("en_US"),
                DefaultLanguage = "en",
                ExpireAfter = TimeSpan.FromSeconds(20),
                LanguageOverride = "es",
                Max = 30,
                Min = 40,
                Name = "awesome",
                PartialFilterExpression = partialFilterExpression,
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

            if (async)
            {
                subject.Indexes.CreateManyAsync(new[] { first, second }, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Indexes.CreateMany(new[] { first, second }, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<CreateIndexesOperation>();
            var operation = (CreateIndexesOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.Requests.Count().Should().Be(2);
            operation.WriteConcern.Should().BeSameAs(writeConcern);

            var request1 = operation.Requests.ElementAt(0);
            request1.AdditionalOptions.Should().BeNull();
            request1.Background.Should().Be(options.Background);
            request1.Bits.Should().Be(options.Bits);
            request1.BucketSize.Should().Be(options.BucketSize);
            request1.Collation.Should().BeSameAs(options.Collation);
            request1.DefaultLanguage.Should().Be(options.DefaultLanguage);
            request1.ExpireAfter.Should().Be(options.ExpireAfter);
            request1.Keys.Should().Be(keys);
            request1.LanguageOverride.Should().Be(options.LanguageOverride);
            request1.Max.Should().Be(options.Max);
            request1.Min.Should().Be(options.Min);
            request1.Name.Should().Be(options.Name);
            request1.PartialFilterExpression.Should().Be(renderedPartialFilterExpression);
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
            request2.Collation.Should().BeNull();
            request2.DefaultLanguage.Should().BeNull();
            request2.ExpireAfter.Should().NotHaveValue();
            request2.Keys.Should().Be(keys2);
            request2.LanguageOverride.Should().BeNull();
            request2.Max.Should().NotHaveValue();
            request2.Min.Should().NotHaveValue(); ;
            request2.Name.Should().BeNull();
            request2.PartialFilterExpression.Should().BeNull();
            request2.Sparse.Should().NotHaveValue(); ;
            request2.SphereIndexVersion.Should().NotHaveValue();
            request2.StorageEngine.Should().BeNull();
            request2.TextIndexVersion.Should().NotHaveValue();
            request2.Unique.Should().NotHaveValue();
            request2.Version.Should().NotHaveValue();
            request2.Weights.Should().BeNull();
            request2.GetIndexName().Should().Be("z_1");
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_DropAll_should_execute_the_DropIndexOperation(
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);

            if (async)
            {
                subject.Indexes.DropAllAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Indexes.DropAll(CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropIndexOperation>();
            var operation = (DropIndexOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.IndexName.Should().Be("*");
            operation.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_DropOne_should_execute_the_DropIndexOperation(
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);

            if (async)
            {
                subject.Indexes.DropOneAsync("name", CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Indexes.DropOne("name", CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropIndexOperation>();
            var operation = (DropIndexOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.IndexName.Should().Be("name");
            operation.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_DropOne_should_throw_an_exception_if_an_asterisk_is_used(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();

            Action action;
            if (async)
            {
                action = () => subject.Indexes.DropOneAsync("*", CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Indexes.DropOne("*", CancellationToken.None);
            }

            action.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_List_should_execute_the_ListIndexesOperation(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.Indexes.ListAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Indexes.List(CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<ListIndexesOperation>();
            var operation = (ListIndexesOperation)call.Operation;
            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertOne_should_execute_the_BulkMixedOperation(
            [Values(false, true)] bool async)
        {
            var document = BsonDocument.Parse("{_id:1,a:1}");
            var expectedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(1, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();

            if (async)
            {
                subject.InsertOneAsync(document, cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.InsertOne(document, cancellationToken: CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, null, true, call);
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertOne_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool async)
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

            Action action;
            if (async)
            {
                action = () => subject.InsertOneAsync(document, cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.InsertOne(document, cancellationToken: CancellationToken.None);
            }

            action.ShouldThrow<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertOne_should_respect_AssignIdOnInsert(
            [Values(false, true)] bool assignIdOnInsert,
            [Values(false, true)] bool async)
        {
            var document = BsonDocument.Parse("{ a : 1 }");
            var expectedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(1, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var settings = new MongoCollectionSettings { AssignIdOnInsert = assignIdOnInsert };
            var subject = CreateSubject<BsonDocument>(settings);

            if (async)
            {
                subject.InsertOneAsync(document, cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.InsertOne(document, cancellationToken: CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            var operation = (BulkMixedWriteOperation)call.Operation;
            var requests = operation.Requests.ToList(); // call ToList to force evaluation
            document.Contains("_id").Should().Be(assignIdOnInsert);
            VerifySingleWrite(expectedRequest, null, true, call);
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertMany_should_execute_the_BulkMixedOperation(
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isOrdered,
            [Values(false, true)] bool async)
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
            var options = new InsertManyOptions { BypassDocumentValidation = bypassDocumentValidation, IsOrdered = isOrdered };

            if (async)
            {
                subject.InsertManyAsync(documents, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.InsertMany(documents, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifyWrites(expectedRequests, bypassDocumentValidation, isOrdered, call);
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertMany_should_respect_AssignIdOnInsert(
            [Values(false, true)] bool assignIdOnInsert,
            [Values(false, true)] bool async)
        {
            var document = BsonDocument.Parse("{ a : 1 }");
            var expectedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(1, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var settings = new MongoCollectionSettings { AssignIdOnInsert = assignIdOnInsert };
            var subject = CreateSubject<BsonDocument>(settings);

            if (async)
            {
                subject.InsertManyAsync(new[] { document }, cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.InsertMany(new[] { document }, cancellationToken: CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            var operation = (BulkMixedWriteOperation)call.Operation;
            var requests = operation.Requests.ToList(); // call ToList to force evaluation
            document.Contains("_id").Should().Be(assignIdOnInsert);
            VerifySingleWrite(expectedRequest, null, true, call);
        }

        [Theory]
        [ParameterAttributeData]
        public void MapReduce_with_inline_output_mode_should_execute_the_MapReduceOperation(
            [Values(false, true)] bool async)
        {
            var filter = new BsonDocument("filter", 1);
            var scope = new BsonDocument("test", 3);
            var sort = new BsonDocument("sort", 1);
            var options = new MapReduceOptions<BsonDocument, BsonDocument>
            {
                Collation = new Collation("en_US"),
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

            if (async)
            {
                subject.MapReduceAsync("map", "reduce", options).GetAwaiter().GetResult();
            }
            else
            {
                subject.MapReduce("map", "reduce", options);
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            call.Operation.Should().BeOfType<MapReduceOperation<BsonDocument>>();
            var operation = (MapReduceOperation<BsonDocument>)call.Operation;
            operation.Collation.Should().BeSameAs(options.Collation);
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

        [Theory]
        [ParameterAttributeData]
        public void MapReduce_with_collection_output_mode_should_execute_the_MapReduceOperation(
            [Values(false, true)] bool async)
        {
            var filter = new BsonDocument("filter", 1);
            var scope = new BsonDocument("test", 3);
            var sort = new BsonDocument("sort", 1);
            var options = new MapReduceOptions<BsonDocument, BsonDocument>
            {
                BypassDocumentValidation = true,
                Collation = new Collation("en_US"),
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
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);

            if (async)
            {
                subject.MapReduceAsync("map", "reduce", options).GetAwaiter().GetResult();
            }
            else
            {
                subject.MapReduce("map", "reduce", options);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            call.Operation.Should().BeOfType<MapReduceOutputToCollectionOperation>();
            var operation = (MapReduceOutputToCollectionOperation)call.Operation;
            operation.BypassDocumentValidation.Should().BeTrue();
            operation.Collation.Should().BeSameAs(options.Collation);
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
            operation.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReplaceOne_should_execute_the_BulkMixedOperation(
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var replacement = BsonDocument.Parse("{a:2}");
            var collation = new Collation("en_US");
            var expectedRequest = new UpdateRequest(UpdateType.Replacement, filter, replacement) { Collation = collation, CorrelationId = 0, IsUpsert = isUpsert, IsMulti = false };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            var updateOptions = new UpdateOptions { Collation = collation, BypassDocumentValidation = bypassDocumentValidation, IsUpsert = isUpsert };

            if (async)
            {
                subject.ReplaceOneAsync(filter, replacement, updateOptions, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.ReplaceOne(filter, replacement, updateOptions, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, bypassDocumentValidation, true, call);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReplaceOne_should_throw_a_WriteException_when_an_error_occurs(
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var replacement = BsonDocument.Parse("{a:2}");
            var collation = new Collation("en_US");
            var expectedRequest = new UpdateRequest(UpdateType.Replacement, filter, replacement) { Collation = collation, CorrelationId = 0, IsUpsert = isUpsert, IsMulti = false };
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
            var updateOptions = new UpdateOptions { Collation = collation, BypassDocumentValidation = bypassDocumentValidation, IsUpsert = isUpsert };

            Action action;
            if (async)
            {
                action = () => subject.ReplaceOneAsync(filter, replacement, updateOptions, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.ReplaceOne(filter, replacement, updateOptions, CancellationToken.None);
            }

            action.ShouldThrow<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateMany_should_execute_the_BulkMixedOperation(
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var collation = new Collation("en_US");
            var expectedRequest = new UpdateRequest(UpdateType.Update, filter, update) { Collation = collation, CorrelationId = 0, IsUpsert = isUpsert, IsMulti = true };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            var updateOptions = new UpdateOptions { BypassDocumentValidation = bypassDocumentValidation, Collation = collation, IsUpsert = isUpsert };

            if (async)
            {
                subject.UpdateManyAsync(filter, update, updateOptions, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.UpdateMany(filter, update, updateOptions, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, bypassDocumentValidation, true, call);
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateMany_should_throw_a_WriteException_when_an_error_occurs(
            [Values(null, false,true)] bool? bypassDocumentValidation,
            [Values(false,true)] bool isUpsert,
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var collation = new Collation("en_US");
            var expectedRequest = new UpdateRequest(UpdateType.Update, filter, update) { Collation = collation, CorrelationId = 0, IsUpsert = isUpsert, IsMulti = true };
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
            var updateOptions = new UpdateOptions { BypassDocumentValidation = bypassDocumentValidation, Collation = collation, IsUpsert = isUpsert };

            Action action;
            if (async)
            {
                action = () => subject.UpdateManyAsync(filter, update, updateOptions, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.UpdateMany(filter, update, updateOptions, CancellationToken.None);
            }

            action.ShouldThrow<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateOne_should_execute_the_BulkMixedOperation(
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var collation = new Collation("en_US");
            var expectedRequest = new UpdateRequest(UpdateType.Update, filter, update) { Collation = collation, CorrelationId = 0, IsUpsert = isUpsert, IsMulti = false };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { expectedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            var subject = CreateSubject<BsonDocument>();
            var updateOptions = new UpdateOptions { BypassDocumentValidation = bypassDocumentValidation, Collation = collation, IsUpsert = isUpsert };

            if (async)
            {
                subject.UpdateOneAsync(filter, update, updateOptions, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.UpdateOne(filter, update, updateOptions, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySingleWrite(expectedRequest, bypassDocumentValidation, true, call);
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateOne_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(false, true)] bool async)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set:{a:1}}");
            var collation = new Collation("en_US");
            var expectedRequest = new UpdateRequest(UpdateType.Update, filter, update) { Collation = collation, CorrelationId = 0, IsUpsert = isUpsert, IsMulti = false };
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
            var updateOptions = new UpdateOptions { BypassDocumentValidation = bypassDocumentValidation, Collation = collation, IsUpsert = isUpsert };

            Action action;
            if (async)
            {
                action = () => subject.UpdateOneAsync(filter, update, updateOptions, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.UpdateOne(filter, update, updateOptions, CancellationToken.None);
            }

            action.ShouldThrow<MongoWriteException>();
        }

        [Fact]
        public void WithReadPreference_should_return_a_new_collection_with_the_read_preference_changed()
        {
            var subject = CreateSubject<BsonDocument>();
            var newSubject = subject.WithReadPreference(ReadPreference.Nearest);
            newSubject.Settings.ReadPreference.Should().Be(ReadPreference.Nearest);
        }

        [Fact]
        public void WithWriteConcern_should_return_a_new_collection_with_the_write_concern_changed()
        {
            var subject = CreateSubject<BsonDocument>();
            var newSubject = subject.WithWriteConcern(WriteConcern.WMajority);
            newSubject.Settings.WriteConcern.Should().Be(WriteConcern.WMajority);
        }

        private static void VerifyWrites(WriteRequest[] expectedRequests, bool? bypassDocumentValidation, bool isOrdered, MockOperationExecutor.WriteCall<BulkWriteOperationResult> call)
        {
            call.Operation.Should().BeOfType<BulkMixedWriteOperation>();
            var operation = (BulkMixedWriteOperation)call.Operation;

            operation.CollectionNamespace.FullName.Should().Be("foo.bar");
            operation.BypassDocumentValidation.Should().Be(bypassDocumentValidation);
            operation.IsOrdered.Should().Be(isOrdered);

            var actualRequests = operation.Requests.ToList();
            actualRequests.Count.Should().Be(expectedRequests.Length);

            for (int i = 0; i < expectedRequests.Length; i++)
            {
                expectedRequests[i].ShouldBeEquivalentTo(actualRequests[i]);
            }
        }

        private static void VerifySingleWrite<TRequest>(TRequest expectedRequest, bool? bypassDocumentValidation, bool isOrdered, MockOperationExecutor.WriteCall<BulkWriteOperationResult> call)
            where TRequest : WriteRequest
        {
            VerifyWrites(new[] { expectedRequest }, bypassDocumentValidation, isOrdered, call);
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
