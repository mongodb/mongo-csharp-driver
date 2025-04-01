/* Copyright 2010-present MongoDB Inc.
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
using Shouldly;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Bson.TestHelpers;
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
            var mockClient = new Mock<IMongoClient>();
            var mockCluster = new Mock<IClusterInternal>();
            mockClient.SetupGet(m => m.Cluster).Returns(mockCluster.Object);
            _operationExecutor = new MockOperationExecutor();
            _operationExecutor.Client = mockClient.Object;
        }

        [Fact]
        public void CollectionName_should_be_set()
        {
            var subject = CreateSubject<BsonDocument>();
            subject.CollectionNamespace.CollectionName.ShouldBe("bar");
        }

        public void Database_should_be_set()
        {
            var subject = CreateSubject<BsonDateTime>();
            subject.Database.ShouldNotBeNull();
        }

        [Fact]
        public void Settings_should_be_set()
        {
            var subject = CreateSubject<BsonDocument>();
            subject.Settings.ShouldNotBeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_should_execute_an_AggregateOperation_when_out_is_not_specified(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var pipeline = new EmptyPipelineDefinition<BsonDocument>().Match("{ x : 2 }");
            var options = new AggregateOptions()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                Collation = new Collation("en_US"),
                Comment = "test",
                Hint = new BsonDocument("x", 1),
                Let = new BsonDocument("y", "z"),
                MaxAwaitTime = TimeSpan.FromSeconds(4),
                MaxTime = TimeSpan.FromSeconds(3),
#pragma warning disable 618
                UseCursor = false
#pragma warning restore 618
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var renderedPipeline = RenderPipeline(subject, pipeline);

            if (usingSession)
            {
                if (async)
                {
                    subject.AggregateAsync(session, pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Aggregate(session, pipeline, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.AggregateAsync(pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Aggregate(pipeline, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<AggregateOperation<BsonDocument>>();
            operation.AllowDiskUse.ShouldBe(options.AllowDiskUse);
            operation.BatchSize.ShouldBe(options.BatchSize);
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Comment.ShouldBe(options.Comment);
            operation.Hint.ShouldBe(options.Hint);
            operation.Let.ShouldBe(options.Let);
            operation.MaxAwaitTime.ShouldBe(options.MaxAwaitTime);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.Pipeline.ShouldBe(renderedPipeline.Documents);
            operation.ReadConcern.ShouldBe(_readConcern);
            operation.RetryRequested.ShouldBeTrue();
            operation.ResultSerializer.ShouldBeSameAs(renderedPipeline.OutputSerializer);
#pragma warning disable 618
            operation.UseCursor.ShouldBe(options.UseCursor);
#pragma warning restore 618
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_should_execute_an_AggregateToCollectionOperation_and_a_FindOperation_when_out_is_specified(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingDifferentOutputDatabase,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var readConcern = new ReadConcern(ReadConcernLevel.Majority);
            var inputDatabase = CreateDatabase(databaseName: "inputDatabaseName");
            var subject = CreateSubject<BsonDocument>(database: inputDatabase).WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var outputDatabase = usingDifferentOutputDatabase ? subject.Database.Client.GetDatabase("outputDatabaseName") : inputDatabase;
            var outputCollection = outputDatabase.GetCollection<BsonDocument>("outputCollectionName");
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Match("{ x : 2 }")
                .Out(outputCollection);
            var options = new AggregateOptions()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                BypassDocumentValidation = true,
                Collation = new Collation("en_US"),
                Comment = "test",
                Hint = new BsonDocument("x", 1),
                Let = new BsonDocument("y", "z"),
                MaxTime = TimeSpan.FromSeconds(3),
#pragma warning disable 618
                UseCursor = false
#pragma warning restore 618
            };

            using var cancellationTokenSource1 = new CancellationTokenSource();
            using var cancellationTokenSource2 = new CancellationTokenSource();

            var cancellationToken1 = cancellationTokenSource1.Token;
            var cancellationToken2 = cancellationTokenSource2.Token;
            var expectedPipeline = new List<BsonDocument>(RenderPipeline(subject, pipeline).Documents);
            if (!usingDifferentOutputDatabase)
            {
                expectedPipeline[1] = new BsonDocument("$out", outputCollection.CollectionNamespace.CollectionName);
            }

            IAsyncCursor<BsonDocument> result;
            if (usingSession)
            {
                if (async)
                {
                    result = subject.AggregateAsync(session, pipeline, options, cancellationToken1).GetAwaiter().GetResult();
                }
                else
                {
                    result = subject.Aggregate(session, pipeline, options, cancellationToken1);
                }
            }
            else
            {
                if (async)
                {
                    result = subject.AggregateAsync(pipeline, options, cancellationToken1).GetAwaiter().GetResult();
                }
                else
                {
                    result = subject.Aggregate(pipeline, options, cancellationToken1);
                }
            }

            var aggregateCall = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(aggregateCall, session, cancellationToken1);

            var aggregateOperation = aggregateCall.Operation.ShouldBeOfType<AggregateToCollectionOperation>();
            aggregateOperation.AllowDiskUse.ShouldBe(options.AllowDiskUse);
            aggregateOperation.BypassDocumentValidation.ShouldBe(options.BypassDocumentValidation);
            aggregateOperation.Collation.ShouldBeSameAs(options.Collation);
            aggregateOperation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            aggregateOperation.Comment.ShouldBe(options.Comment);
            aggregateOperation.Hint.ShouldBe(options.Hint);
            aggregateOperation.Let.ShouldBe(options.Let);
            aggregateOperation.MaxTime.ShouldBe(options.MaxTime);
            aggregateOperation.Pipeline.ShouldBe(expectedPipeline);
            aggregateOperation.ReadConcern.ShouldBe(readConcern);
            aggregateOperation.WriteConcern.ShouldBeSameAs(writeConcern);

            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(mockCursor.Object);

            if (async)
            {
                result.MoveNextAsync(cancellationToken2).GetAwaiter().GetResult();
            }
            else
            {
                result.MoveNext(cancellationToken2);
            }

            var findCall = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            VerifySessionAndCancellationToken(findCall, session, cancellationToken2);

            var findOperation = findCall.Operation.ShouldBeOfType<FindOperation<BsonDocument>>();
            findOperation.AllowDiskUse.ShouldNotHaveValue();
            findOperation.AllowPartialResults.ShouldNotHaveValue();
            findOperation.BatchSize.ShouldBe(options.BatchSize);
            findOperation.Collation.ShouldBeSameAs(options.Collation);
            findOperation.CollectionNamespace.FullName.ShouldBe(outputCollection.CollectionNamespace.FullName);
            findOperation.Comment.ShouldBeNull();
            findOperation.CursorType.ShouldBe(CursorType.NonTailable);
            findOperation.Filter.ShouldBeNull();
            findOperation.Limit.ShouldBe(null);
            findOperation.MaxTime.ShouldBe(options.MaxTime);
            findOperation.NoCursorTimeout.ShouldNotHaveValue();
#pragma warning disable 618
            findOperation.OplogReplay.ShouldNotHaveValue();
#pragma warning restore 618
            findOperation.Projection.ShouldBeNull();
            findOperation.RetryRequested.ShouldBeTrue();
            findOperation.Skip.ShouldBe(null);
            findOperation.Sort.ShouldBeNull();
        }

        [Theory]
        [InlineData("{ $merge : \"outputcollection\" }", null, "outputcollection", false)]
        [InlineData("{ $merge : \"outputcollection\" }", null, "outputcollection", true)]
        [InlineData("{ $merge : { into : \"outputcollection\" } }", null, "outputcollection", false)]
        [InlineData("{ $merge : { into : \"outputcollection\" } }", null, "outputcollection", true)]
        [InlineData("{ $merge : { into : { coll : \"outputcollection\" } } }", null, "outputcollection", false)]
        [InlineData("{ $merge : { into : { coll : \"outputcollection\" } } }", null, "outputcollection", true)]
        [InlineData("{ $merge : { into : { db: \"outputdatabase\", coll : \"outputcollection\" } } }", "outputdatabase", "outputcollection", false)]
        [InlineData("{ $merge : { into : { db: \"outputdatabase\", coll : \"outputcollection\" } } }", "outputdatabase", "outputcollection", true)]
        public void Aggregate_should_recognize_merge_collection_argument(
            string stageDefinitionString,
            string expectedDatabaseName,
            string expectedCollectionName,
            bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var stageDefinition = BsonDocument.Parse(stageDefinitionString);
            var expectedCollectionNamespace = new CollectionNamespace(
                expectedDatabaseName ?? subject.CollectionNamespace.DatabaseNamespace.DatabaseName,
                expectedCollectionName);

            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .AppendStage<BsonDocument, BsonDocument, BsonDocument>(stageDefinition);
            var expectedPipeline = new List<BsonDocument>(RenderPipeline(subject, pipeline).Documents);

            IAsyncCursor<BsonDocument> result;
            if (async)
            {
                result = subject.AggregateAsync(pipeline).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.Aggregate(pipeline);
            }
            var aggregateCall = _operationExecutor.GetWriteCall<BsonDocument>();

            var aggregateOperation = aggregateCall.Operation.ShouldBeOfType<AggregateToCollectionOperation>();
            aggregateOperation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            aggregateOperation.Pipeline.ShouldBe(expectedPipeline);

            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult(mockCursor.Object);
            if (async)
            {
                result.MoveNextAsync().GetAwaiter().GetResult();
            }
            else
            {
                result.MoveNext();
            }
            var findCall = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            var findOperation = findCall.Operation.ShouldBeOfType<FindOperation<BsonDocument>>();
            findOperation.CollectionNamespace.ShouldBe(expectedCollectionNamespace);
        }

        [Theory]
        [ParameterAttributeData]
        public void AggregateToCollection_should_execute_an_AggregateToCollectionOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingDifferentOutputDatabase,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var readConcern = new ReadConcern(ReadConcernLevel.Majority);
            var inputDatabase = CreateDatabase(databaseName: "inputDatabaseName");
            var subject = CreateSubject<BsonDocument>(database: inputDatabase).WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var outputDatabase = usingDifferentOutputDatabase ? subject.Database.Client.GetDatabase("outputDatabaseName") : inputDatabase;
            var outputCollection = outputDatabase.GetCollection<BsonDocument>("outputCollectionName");
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Match("{ x : 2 }")
                .Out(outputCollection);
            var options = new AggregateOptions()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                BypassDocumentValidation = true,
                Collation = new Collation("en_US"),
                Comment = "test",
                Hint = new BsonDocument("x", 1),
                Let = new BsonDocument("y", "z"),
                MaxTime = TimeSpan.FromSeconds(3),
#pragma warning disable 618
                UseCursor = false
#pragma warning restore 618
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var expectedPipeline = new List<BsonDocument>(RenderPipeline(subject, pipeline).Documents);
            if (!usingDifferentOutputDatabase)
            {
                expectedPipeline[1] = new BsonDocument("$out", outputCollection.CollectionNamespace.CollectionName);
            }

            if (async)
            {
                if (usingSession)
                {
                    subject.AggregateToCollectionAsync(session, pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.AggregateToCollectionAsync(pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
            }
            else
            {
                if (usingSession)
                {
                    subject.AggregateToCollection(session, pipeline, options, cancellationToken);
                }
                else
                {
                    subject.AggregateToCollection(pipeline, options, cancellationToken);
                }
            }

            var aggregateCall = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(aggregateCall, session, cancellationToken);

            var aggregateOperation = aggregateCall.Operation.ShouldBeOfType<AggregateToCollectionOperation>();
            aggregateOperation.AllowDiskUse.ShouldBe(options.AllowDiskUse);
            aggregateOperation.BypassDocumentValidation.ShouldBe(options.BypassDocumentValidation);
            aggregateOperation.Collation.ShouldBeSameAs(options.Collation);
            aggregateOperation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            aggregateOperation.Comment.ShouldBe(options.Comment);
            aggregateOperation.Hint.ShouldBe(options.Hint);
            aggregateOperation.Let.ShouldBe(options.Let);
            aggregateOperation.MaxTime.ShouldBe(options.MaxTime);
            aggregateOperation.Pipeline.ShouldBe(expectedPipeline);
            aggregateOperation.ReadConcern.ShouldBe(readConcern);
            aggregateOperation.WriteConcern.ShouldBeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void AggregateToCollection_should_throw_when_last_stage_is_not_an_output_stage(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Match("{ x : 2 }");
            var options = new AggregateOptions();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Exception exception;
            if (async)
            {
                if (usingSession)
                {
                    exception = Record.Exception(() => subject.AggregateToCollectionAsync(session, pipeline, options, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.AggregateToCollectionAsync(pipeline, options, cancellationToken).GetAwaiter().GetResult());
                }
            }
            else
            {
                if (usingSession)
                {
                    exception = Record.Exception(() => subject.AggregateToCollection(session, pipeline, options, cancellationToken));
                }
                else
                {
                    exception = Record.Exception(() => subject.AggregateToCollection(pipeline, options, cancellationToken));
                }
            }

            exception.ShouldBeOfType<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task BulkWrite_should_enumerate_requests_once([Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var document = new BsonDocument("_id", 1).Add("a", 1);
            var requests = new WriteModel<BsonDocument>[]
            {
                new InsertOneModel<BsonDocument>(document)
            };
            var processedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationResult = new BulkWriteOperationResult.Acknowledged(
                requestCount: 1,
                matchedCount: 0,
                deletedCount: 0,
                insertedCount: 1,
                modifiedCount: 0,
                processedRequests: new[] { processedRequest },
                upserts: new List<BulkWriteOperationUpsert>());
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);
            var wrappedRequests = new Mock<IEnumerable<WriteModel<BsonDocument>>>();
            wrappedRequests.Setup(e => e.GetEnumerator()).Returns(((IEnumerable<WriteModel<BsonDocument>>)requests).GetEnumerator());

            var result = async ? await subject.BulkWriteAsync(wrappedRequests.Object) : subject.BulkWrite(wrappedRequests.Object);

            wrappedRequests.Verify(e => e.GetEnumerator(), Times.Once);
            result.ShouldNotBeNull();
            result.RequestCount.ShouldBe(1);
            result.ProcessedRequests.ShouldBeEquivalentTo(requests);
        }

        [Theory]
        [ParameterAttributeData]
        public void BulkWrite_should_execute_a_BulkMixedWriteOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool isOrdered,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var collation = new Collation("en_US");
            var hint = new BsonDocument("x", 1);
            var requests = new WriteModel<BsonDocument>[]
            {
                new InsertOneModel<BsonDocument>(new BsonDocument("_id", 1).Add("a",1)),
                new DeleteManyModel<BsonDocument>(new BsonDocument("b", 1)) { Collation = collation },
                new DeleteManyModel<BsonDocument>(new BsonDocument("c", 1)) { Collation = collation, Hint = hint },
                new DeleteOneModel<BsonDocument>(new BsonDocument("d", 1)) { Collation = collation },
                new DeleteOneModel<BsonDocument>(new BsonDocument("e", 1)) { Collation = collation, Hint = hint },
                new ReplaceOneModel<BsonDocument>(new BsonDocument("f", 1), new BsonDocument("g", 1)) { Collation = collation },
                new ReplaceOneModel<BsonDocument>(new BsonDocument("h", 1), new BsonDocument("i", 1)) { Collation = collation, Hint = hint },
                new ReplaceOneModel<BsonDocument>(new BsonDocument("j", 1), new BsonDocument("k", 1)) { Collation = collation, IsUpsert = true },
                new UpdateManyModel<BsonDocument>(new BsonDocument("l", 1), new BsonDocument("$set", new BsonDocument("m", 1))) { Collation = collation },
                new UpdateManyModel<BsonDocument>(new BsonDocument("n", 1), new BsonDocument("$set", new BsonDocument("o", 1))) { Collation = collation, Hint = hint },
                new UpdateManyModel<BsonDocument>(new BsonDocument("p", 1), new BsonDocument("$set", new BsonDocument("q", 1))) { Collation = collation, IsUpsert = true },
                new UpdateOneModel<BsonDocument>(new BsonDocument("r", 1), new BsonDocument("$set", new BsonDocument("s", 1))) { Collation = collation },
                new UpdateOneModel<BsonDocument>(new BsonDocument("t", 1), new BsonDocument("$set", new BsonDocument("u", 1))) { Collation = collation, Hint = hint },
                new UpdateOneModel<BsonDocument>(new BsonDocument("v", 1), new BsonDocument("$set", new BsonDocument("w", 1))) { Collation = collation, IsUpsert = true },
            };
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            var options = new BulkWriteOptions
            {
                BypassDocumentValidation = bypassDocumentValidation,
                IsOrdered = isOrdered,
                Let = letDocument
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var operationResult = new BulkWriteOperationResult.Unacknowledged(14, new[] { new InsertRequest(new BsonDocument("b", 1)) });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            BulkWriteResult<BsonDocument> result;
            if (usingSession)
            {
                if (async)
                {
                    result = subject.BulkWriteAsync(session, requests, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    result = subject.BulkWrite(session, requests, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    result = subject.BulkWriteAsync(requests, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    result = subject.BulkWrite(requests, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            // I know, this is a lot of stuff in one test :(
            var operation = call.Operation.ShouldBeOfType<BulkMixedWriteOperation>();
            operation.BypassDocumentValidation.ShouldBe(bypassDocumentValidation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.IsOrdered.ShouldBe(isOrdered);
            operation.Let.ShouldBe(letDocument);
            operation.Requests.Count().ShouldBe(14);

            var convertedRequests = operation.Requests.ToList();

            // InsertOneModel
            convertedRequests[0].ShouldBeOfType<InsertRequest>();
            convertedRequests[0].CorrelationId.ShouldBe(0);
            var convertedRequest0 = (InsertRequest)convertedRequests[0];
            convertedRequest0.Document.ShouldBe("{_id:1, a:1}");

            // RemoveManyModel
            convertedRequests[1].ShouldBeOfType<DeleteRequest>();
            convertedRequests[1].CorrelationId.ShouldBe(1);
            var convertedRequest1 = (DeleteRequest)convertedRequests[1];
            convertedRequest1.Collation.ShouldBeSameAs(collation);
            convertedRequest1.Filter.ShouldBe("{b:1}");
            convertedRequest1.Hint.ShouldBeNull();
            convertedRequest1.Limit.ShouldBe(0);

            // RemoveManyModel with hint
            convertedRequests[2].ShouldBeOfType<DeleteRequest>();
            convertedRequests[2].CorrelationId.ShouldBe(2);
            var convertedRequest2 = (DeleteRequest)convertedRequests[2];
            convertedRequest2.Collation.ShouldBeSameAs(collation);
            convertedRequest2.Filter.ShouldBe("{c:1}");
            convertedRequest2.Hint.ShouldBe(hint);
            convertedRequest2.Limit.ShouldBe(0);

            // RemoveOneModel
            convertedRequests[3].ShouldBeOfType<DeleteRequest>();
            convertedRequests[3].CorrelationId.ShouldBe(3);
            var convertedRequest3 = (DeleteRequest)convertedRequests[3];
            convertedRequest3.Collation.ShouldBeSameAs(collation);
            convertedRequest3.Filter.ShouldBe("{d:1}");
            convertedRequest3.Hint.ShouldBeNull();
            convertedRequest3.Limit.ShouldBe(1);

            // RemoveOneModel with hint
            convertedRequests[4].ShouldBeOfType<DeleteRequest>();
            convertedRequests[4].CorrelationId.ShouldBe(4);
            var convertedRequest4 = (DeleteRequest)convertedRequests[4];
            convertedRequest4.Collation.ShouldBeSameAs(collation);
            convertedRequest4.Filter.ShouldBe("{e:1}");
            convertedRequest4.Hint.ShouldBe(hint);
            convertedRequest4.Limit.ShouldBe(1);

            // ReplaceOneModel
            convertedRequests[5].ShouldBeOfType<UpdateRequest>();
            convertedRequests[5].CorrelationId.ShouldBe(5);
            var convertedRequest5 = (UpdateRequest)convertedRequests[5];
            convertedRequest5.Collation.ShouldBeSameAs(collation);
            convertedRequest5.Filter.ShouldBe("{f:1}");
            convertedRequest5.Hint.ShouldBeNull();
            convertedRequest5.Update.ShouldBe("{g:1}");
            convertedRequest5.UpdateType.ShouldBe(UpdateType.Replacement);
            convertedRequest5.IsMulti.ShouldBeFalse();
            convertedRequest5.IsUpsert.ShouldBeFalse();

            // ReplaceOneModel with hint
            convertedRequests[6].ShouldBeOfType<UpdateRequest>();
            convertedRequests[6].CorrelationId.ShouldBe(6);
            var convertedRequest6 = (UpdateRequest)convertedRequests[6];
            convertedRequest6.Collation.ShouldBeSameAs(collation);
            convertedRequest6.Filter.ShouldBe("{h:1}");
            convertedRequest6.Hint.ShouldBe(hint);
            convertedRequest6.Update.ShouldBe("{i:1}");
            convertedRequest6.UpdateType.ShouldBe(UpdateType.Replacement);
            convertedRequest6.IsMulti.ShouldBeFalse();
            convertedRequest6.IsUpsert.ShouldBeFalse();

            // ReplaceOneModel with upsert
            convertedRequests[7].ShouldBeOfType<UpdateRequest>();
            convertedRequests[7].CorrelationId.ShouldBe(7);
            var convertedRequest7 = (UpdateRequest)convertedRequests[7];
            convertedRequest7.Collation.ShouldBeSameAs(collation);
            convertedRequest7.Filter.ShouldBe("{j:1}");
            convertedRequest7.Hint.ShouldBeNull();
            convertedRequest7.Update.ShouldBe("{k:1}");
            convertedRequest7.UpdateType.ShouldBe(UpdateType.Replacement);
            convertedRequest7.IsMulti.ShouldBeFalse();
            convertedRequest7.IsUpsert.ShouldBeTrue();

            // UpdateManyModel
            convertedRequests[8].ShouldBeOfType<UpdateRequest>();
            convertedRequests[8].CorrelationId.ShouldBe(8);
            var convertedRequest8 = (UpdateRequest)convertedRequests[8];
            convertedRequest8.Collation.ShouldBeSameAs(collation);
            convertedRequest8.Filter.ShouldBe("{l:1}");
            convertedRequest8.Hint.ShouldBeNull();
            convertedRequest8.Update.ShouldBe("{$set:{m:1}}");
            convertedRequest8.UpdateType.ShouldBe(UpdateType.Update);
            convertedRequest8.IsMulti.ShouldBeTrue();
            convertedRequest8.IsUpsert.ShouldBeFalse();

            // UpdateManyModel with hint
            convertedRequests[9].ShouldBeOfType<UpdateRequest>();
            convertedRequests[9].CorrelationId.ShouldBe(9);
            var convertedRequest9 = (UpdateRequest)convertedRequests[9];
            convertedRequest9.Collation.ShouldBeSameAs(collation);
            convertedRequest9.Filter.ShouldBe("{n:1}");
            convertedRequest9.Hint.ShouldBe(hint);
            convertedRequest9.Update.ShouldBe("{$set:{o:1}}");
            convertedRequest9.UpdateType.ShouldBe(UpdateType.Update);
            convertedRequest9.IsMulti.ShouldBeTrue();
            convertedRequest9.IsUpsert.ShouldBeFalse();

            // UpdateManyModel with upsert
            convertedRequests[10].ShouldBeOfType<UpdateRequest>();
            convertedRequests[10].CorrelationId.ShouldBe(10);
            var convertedRequest10 = (UpdateRequest)convertedRequests[10];
            convertedRequest10.Collation.ShouldBeSameAs(collation);
            convertedRequest10.Filter.ShouldBe("{p:1}");
            convertedRequest10.Hint.ShouldBeNull();
            convertedRequest10.Update.ShouldBe("{$set:{q:1}}");
            convertedRequest10.UpdateType.ShouldBe(UpdateType.Update);
            convertedRequest10.IsMulti.ShouldBeTrue();
            convertedRequest10.IsUpsert.ShouldBeTrue();

            // UpdateOneModel
            convertedRequests[11].ShouldBeOfType<UpdateRequest>();
            convertedRequests[11].CorrelationId.ShouldBe(11);
            var convertedRequest11 = (UpdateRequest)convertedRequests[11];
            convertedRequest11.Collation.ShouldBeSameAs(collation);
            convertedRequest11.Filter.ShouldBe("{r:1}");
            convertedRequest11.Hint.ShouldBeNull();
            convertedRequest11.Update.ShouldBe("{$set:{s:1}}");
            convertedRequest11.UpdateType.ShouldBe(UpdateType.Update);
            convertedRequest11.IsMulti.ShouldBeFalse();
            convertedRequest11.IsUpsert.ShouldBeFalse();

            // UpdateOneModel with hint
            convertedRequests[12].ShouldBeOfType<UpdateRequest>();
            convertedRequests[12].CorrelationId.ShouldBe(12);
            var convertedRequest12 = (UpdateRequest)convertedRequests[12];
            convertedRequest12.Collation.ShouldBeSameAs(collation);
            convertedRequest12.Filter.ShouldBe("{t:1}");
            convertedRequest12.Hint.ShouldBe(hint);
            convertedRequest12.Update.ShouldBe("{$set:{u:1}}");
            convertedRequest12.UpdateType.ShouldBe(UpdateType.Update);
            convertedRequest12.IsMulti.ShouldBeFalse();
            convertedRequest12.IsUpsert.ShouldBeFalse();

            // UpdateOneModel with upsert
            convertedRequests[13].ShouldBeOfType<UpdateRequest>();
            convertedRequests[13].CorrelationId.ShouldBe(13);
            var convertedRequest13 = (UpdateRequest)convertedRequests[13];
            convertedRequest13.Collation.ShouldBeSameAs(collation);
            convertedRequest13.Filter.ShouldBe("{v:1}");
            convertedRequest13.Hint.ShouldBeNull();
            convertedRequest13.Update.ShouldBe("{$set:{w:1}}");
            convertedRequest13.UpdateType.ShouldBe(UpdateType.Update);
            convertedRequest13.IsMulti.ShouldBeFalse();
            convertedRequest13.IsUpsert.ShouldBeTrue();

            // Result
            result.ShouldNotBeNull();
            result.IsAcknowledged.ShouldBeFalse();
            result.RequestCount.ShouldBe(14);
            result.ProcessedRequests.ShouldBeEquivalentTo(requests);
            for (int i = 0; i < requests.Length; i++)
            {
                result.ProcessedRequests[i].ShouldBeSameAs(requests[i]);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BulkWrite_should_throw_if_model_is_invalid([Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();

            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new[] { new BsonDocument("$project", "{ value : 1 }") });
            var update = new PipelineUpdateDefinition<BsonDocument>(pipeline);
            var arrayFilters = new List<ArrayFilterDefinition>()
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("x", 1))
            };

            var models = new WriteModel<BsonDocument>[]
            {
                new UpdateOneModel<BsonDocument>(new BsonDocument("n", 1), update)
                {
                    ArrayFilters = arrayFilters
                },
                new UpdateManyModel<BsonDocument>(new BsonDocument("n", 2), update)
                {
                    ArrayFilters = arrayFilters
                }
            };

            foreach (var model in models)
            {
                Exception exception;
                if (async)
                {
                    exception = Record.ExceptionAsync(async () => { await subject.BulkWriteAsync(new[] { model }); })
                        .GetAwaiter()
                        .GetResult();
                }
                else
                {
                    exception = Record.Exception(() => { subject.BulkWrite(new[] { model }); });
                }

                exception.ShouldBeOfType<NotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_should_execute_a_CountOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filter = new BsonDocument("x", 1);
            var options = new CountOptions
            {
                Collation = new Collation("en_US"),
                Hint = "funny",
                Limit = 10,
                MaxTime = TimeSpan.FromSeconds(20),
                Skip = 30
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
#pragma warning disable 618
                    subject.CountAsync(session, filter, options, cancellationToken).GetAwaiter().GetResult();
#pragma warning restore
                }
                else
                {
#pragma warning disable 618
                    subject.Count(session, filter, options, cancellationToken);
#pragma warning restore
                }
            }
            else
            {
                if (async)
                {
#pragma warning disable 618
                    subject.CountAsync(filter, options, cancellationToken).GetAwaiter().GetResult();
#pragma warning restore
                }
                else
                {
#pragma warning disable 618
                    subject.Count(filter, options, cancellationToken);
#pragma warning restore
                }
            }

            var call = _operationExecutor.GetReadCall<long>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<CountOperation>();
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Filter.ShouldBe(filter);
            operation.Hint.ShouldBe(options.Hint);
            operation.Limit.ShouldBe(options.Limit);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.ReadConcern.ShouldBe(_readConcern);
            operation.RetryRequested.ShouldBeTrue();
            operation.Skip.ShouldBe(options.Skip);
        }

        [Theory]
        [ParameterAttributeData]
        public void CountDocuments_should_execute_a_CountDocumentsOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filter = new BsonDocument("x", 1);
            var options = new CountOptions
            {
                Collation = new Collation("en_US"),
                Hint = "funny",
                Limit = 10,
                MaxTime = TimeSpan.FromSeconds(20),
                Skip = 30
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.CountDocumentsAsync(session, filter, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CountDocuments(session, filter, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.CountDocumentsAsync(filter, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CountDocuments(filter, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<long>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<CountDocumentsOperation>();
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Filter.ShouldBe(filter);
            operation.Hint.ShouldBe(options.Hint);
            operation.Limit.ShouldBe(options.Limit);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.ReadConcern.ShouldBe(_readConcern);
            operation.RetryRequested.ShouldBeTrue();
            operation.Skip.ShouldBe(options.Skip);
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteMany_should_execute_a_BulkMixedOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filter = new BsonDocument("a", 1);
            var collation = new Collation("en_US");
            var hint = new BsonDocument("_id", 1);
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            var options = new DeleteOptions { Collation = collation, Let = letDocument };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new DeleteRequest(filter) { Collation = collation, CorrelationId = 0, Hint = hint, Limit = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { processedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            if (usingSession)
            {
                if (async)
                {
                    subject.DeleteManyAsync(session, filter, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DeleteMany(session, filter, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.DeleteManyAsync(filter, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DeleteMany(filter, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            VerifySingleWrite(call, bypassDocumentValidation: null, isOrdered: true, let: letDocument, processedRequest);
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteMany_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filter = new BsonDocument("a", 1);
            var options = new DeleteOptions();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new DeleteRequest(filter) { CorrelationId = 0, Limit = 0 };
            var operationException = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { processedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(10, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());
            _operationExecutor.EnqueueException<BulkWriteOperationResult>(operationException);

            Exception exception;
            if (usingSession)
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.DeleteManyAsync(session, filter, options, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.DeleteMany(session, filter, options, cancellationToken));
                }
            }
            else
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.DeleteManyAsync(filter, options, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.DeleteMany(filter, options, cancellationToken));
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            exception.ShouldBeOfType<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteOne_should_execute_a_BulkMixedOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filter = new BsonDocument("a", 1);
            var collation = new Collation("en_US");
            var hint = new BsonDocument("_id", 1);
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            var options = new DeleteOptions { Collation = collation, Let = letDocument };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new DeleteRequest(filter)
            {
                Collation = collation,
                CorrelationId = 0,
                Hint = hint,
                Limit = 1
            };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { processedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            if (usingSession)
            {
                if (async)
                {
                    subject.DeleteOneAsync(session, filter, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DeleteOne(session, filter, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.DeleteOneAsync(filter, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DeleteOne(filter, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            VerifySingleWrite(call, bypassDocumentValidation: null, isOrdered: true, let: letDocument, processedRequest);
        }

        [Theory]
        [ParameterAttributeData]
        public void DeleteOne_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filter = new BsonDocument("a", 1);
            var options = new DeleteOptions();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new DeleteRequest(filter) { CorrelationId = 0, Limit = 1 };
            var operationException = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { processedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());
            _operationExecutor.EnqueueException<BulkWriteOperationResult>(operationException);

            Exception exception;
            if (usingSession)
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.DeleteOneAsync(session, filter, options, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.DeleteOne(session, filter, options, cancellationToken));
                }
            }
            else
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.DeleteOneAsync(filter, options, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.DeleteOne(filter, options, cancellationToken));
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            exception.ShouldBeOfType<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Distinct_should_execute_a_DistinctOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var fieldName = "a.b";
            var fieldDefinition = (FieldDefinition<BsonDocument, int>)fieldName;
            var filterDocument = new BsonDocument("x", 1);
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var options = new DistinctOptions
            {
                Collation = new Collation("en_US"),
                MaxTime = TimeSpan.FromSeconds(20)
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.DistinctAsync(session, fieldDefinition, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Distinct(session, fieldDefinition, filterDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.DistinctAsync(fieldDefinition, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Distinct(fieldDefinition, filterDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<int>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<DistinctOperation<int>>();
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.FieldName.ShouldBe(fieldName);
            operation.Filter.ShouldBe(filterDocument);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.ReadConcern.ShouldBe(_readConcern);
            operation.RetryRequested.ShouldBeTrue();
            operation.ValueSerializer.ValueType.ShouldBe(typeof(int));
        }

        private enum EnumForDistinctWithArrayField { A, B }

        private class ClassForDistinctWithArrayField
        {
            public int Id { get; set; }
            [BsonRepresentation(BsonType.String)]
            public EnumForDistinctWithArrayField[] A { get; set; }
        }

        [Theory]
        [ParameterAttributeData]
        public void Distinct_should_execute_a_DistinctOperation_when_type_parameter_is_array_field_item_type(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<ClassForDistinctWithArrayField>();
            var session = CreateSession(usingSession);
            var fieldName = "A";
            var fieldDefinition = (FieldDefinition<ClassForDistinctWithArrayField, EnumForDistinctWithArrayField>)fieldName;
            var filterDocument = new BsonDocument("x", 1);
            var filterDefinition = (FilterDefinition<ClassForDistinctWithArrayField>)filterDocument;
            var options = new DistinctOptions
            {
                Collation = new Collation("en_US"),
                MaxTime = TimeSpan.FromSeconds(20)
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.DistinctAsync(session, fieldDefinition, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Distinct(session, fieldDefinition, filterDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.DistinctAsync(fieldDefinition, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Distinct(fieldDefinition, filterDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<EnumForDistinctWithArrayField>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<DistinctOperation<EnumForDistinctWithArrayField>>();
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.FieldName.ShouldBe(fieldName);
            operation.Filter.ShouldBe(filterDocument);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.ReadConcern.ShouldBe(_readConcern);
            operation.RetryRequested.ShouldBeTrue();

            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<ClassForDistinctWithArrayField>();
            BsonSerializationInfo fieldSerializationInfo;
            ((IBsonDocumentSerializer)documentSerializer).TryGetMemberSerializationInfo(fieldName, out fieldSerializationInfo).ShouldBeTrue();
            var fieldSerializer = (ArraySerializer<EnumForDistinctWithArrayField>)fieldSerializationInfo.Serializer;
            operation.ValueSerializer.ShouldBeSameAs(fieldSerializer.ItemSerializer);
        }

        [Theory]
        [ParameterAttributeData]
        public void Distinct_should_execute_a_DistinctOperation_when_type_parameter_is_string_instead_of_enum(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<ClassForDistinctWithArrayField>();
            var session = CreateSession(usingSession);
            var fieldName = "A";
            var fieldDefinition = (FieldDefinition<ClassForDistinctWithArrayField, string>)fieldName;
            var filterDocument = new BsonDocument("x", 1);
            var filterDefinition = (FilterDefinition<ClassForDistinctWithArrayField>)filterDocument;
            var options = new DistinctOptions
            {
                Collation = new Collation("en_US"),
                MaxTime = TimeSpan.FromSeconds(20)
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.DistinctAsync(session, fieldDefinition, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Distinct(session, fieldDefinition, filterDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.DistinctAsync(fieldDefinition, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Distinct(fieldDefinition, filterDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<string>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<DistinctOperation<string>>();
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.FieldName.ShouldBe(fieldName);
            operation.Filter.ShouldBe(filterDocument);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.ReadConcern.ShouldBe(_readConcern);
            operation.RetryRequested.ShouldBeTrue();

            var stringSerializer = BsonSerializer.SerializerRegistry.GetSerializer<string>();
            operation.ValueSerializer.ShouldBeSameAs(stringSerializer);
        }

        [Theory]
        [ParameterAttributeData]
        public void DistinctMany_should_execute_a_DistinctOperation_when_type_parameter_is_array_field(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool lambda,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<ClassForDistinctWithArrayField>();
            var session = CreateSession(usingSession);
            var fieldName = "A";

            FieldDefinition<ClassForDistinctWithArrayField, IEnumerable<EnumForDistinctWithArrayField>> fieldDefinition =
                lambda ?
                    new ExpressionFieldDefinition<ClassForDistinctWithArrayField, IEnumerable<EnumForDistinctWithArrayField>>(x => x.A) :
                    new StringFieldDefinition<ClassForDistinctWithArrayField, IEnumerable<EnumForDistinctWithArrayField>>(fieldName);
            var filterDocument = new BsonDocument("x", 1);
            var filterDefinition = (FilterDefinition<ClassForDistinctWithArrayField>)filterDocument;
            var options = new DistinctOptions
            {
                Collation = new Collation("en_US"),
                MaxTime = TimeSpan.FromSeconds(20)
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.DistinctManyAsync(session, fieldDefinition, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DistinctMany(session, fieldDefinition, filterDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.DistinctManyAsync(fieldDefinition, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DistinctMany(fieldDefinition, filterDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<EnumForDistinctWithArrayField>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<DistinctOperation<EnumForDistinctWithArrayField>>();
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.FieldName.ShouldBe(fieldName);
            operation.Filter.ShouldBe(filterDocument);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.ReadConcern.ShouldBe(_readConcern);
            operation.RetryRequested.ShouldBeTrue();

            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<ClassForDistinctWithArrayField>();
            ((IBsonDocumentSerializer)documentSerializer).TryGetMemberSerializationInfo(fieldName, out var fieldSerializationInfo).ShouldBeTrue();
            var fieldSerializer = (ArraySerializer<EnumForDistinctWithArrayField>)fieldSerializationInfo.Serializer;
            operation.ValueSerializer.ShouldBeSameAs(fieldSerializer.ItemSerializer);
        }

        [Theory]
        [ParameterAttributeData]
        public void EstimatedDocumentCount_should_execute_an_EstimatedDocumentCount_operation(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var options = new EstimatedDocumentCountOptions
            {
                MaxTime = TimeSpan.FromSeconds(20)
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (async)
            {
                subject.EstimatedDocumentCountAsync(options, cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                subject.EstimatedDocumentCount(options, cancellationToken);
            }

            var call = _operationExecutor.GetReadCall<long>();
            VerifySessionAndCancellationToken(call, null, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<EstimatedDocumentCountOperation>();
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.ReadConcern.ShouldBe(ReadConcern.Default);
            operation.RetryRequested.ShouldBeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_should_execute_a_FindOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ x : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var projectionDocument = BsonDocument.Parse("{ y : 1 }");
            var projectionDefinition = (ProjectionDefinition<BsonDocument, BsonDocument>)projectionDocument;
            var sortDocument = BsonDocument.Parse("{ a : 1 }");
            var sortDefinition = (SortDefinition<BsonDocument>)sortDocument;
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            var options = new FindOptions<BsonDocument, BsonDocument>
            {
                AllowDiskUse = true,
                AllowPartialResults = true,
                BatchSize = 20,
                Collation = new Collation("en_US"),
                Comment = "funny",
                CursorType = CursorType.TailableAwait,
                Let = letDocument,
                Limit = 30,
                MaxAwaitTime = TimeSpan.FromSeconds(4),
                MaxTime = TimeSpan.FromSeconds(3),
                NoCursorTimeout = true,
#pragma warning disable 618
                OplogReplay = true,
#pragma warning restore 618
                Projection = projectionDefinition,
                Skip = 40,
                Sort = sortDefinition
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.FindAsync(session, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindSync(session, filterDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.FindAsync(filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindSync(filterDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            call.Operation.ShouldBeOfType<FindOperation<BsonDocument>>();
            var operation = (FindOperation<BsonDocument>)call.Operation;
            operation.AllowDiskUse.ShouldBe(options.AllowDiskUse);
            operation.AllowPartialResults.ShouldBe(options.AllowPartialResults);
            operation.BatchSize.ShouldBe(options.BatchSize);
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Comment.ShouldBe((BsonValue)"funny");
            operation.CursorType.ShouldBe(CursorType.TailableAwait);
            operation.Filter.ShouldBe(filterDocument);
            operation.Let.ShouldBe(options.Let);
            operation.Limit.ShouldBe(options.Limit);
            operation.MaxAwaitTime.ShouldBe(options.MaxAwaitTime);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.NoCursorTimeout.ShouldBe(options.NoCursorTimeout);
#pragma warning disable 618
            operation.OplogReplay.ShouldBe(options.OplogReplay);
#pragma warning restore 618
            operation.Projection.ShouldBe(projectionDocument);
            operation.ReadConcern.ShouldBe(_readConcern);
            operation.ResultSerializer.ValueType.ShouldBe(typeof(BsonDocument));
            operation.RetryRequested.ShouldBeTrue();
            operation.Skip.ShouldBe(options.Skip);
            operation.Sort.ShouldBe(sortDocument);
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_with_an_expression_execute_a_FindOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterExpression = (Expression<Func<BsonDocument, bool>>)(doc => doc["x"] == 1);
            var projectionDocument = BsonDocument.Parse("{ y : 1 }");
            var projectionDefinition = (ProjectionDefinition<BsonDocument, BsonDocument>)projectionDocument;
            var sortDocument = BsonDocument.Parse("{ a : 1 }");
            var sortDefinition = (SortDefinition<BsonDocument>)sortDocument;
            var letDocument = BsonDocument.Parse("{ name : 'name' }");
            var options = new FindOptions<BsonDocument, BsonDocument>
            {
                AllowDiskUse = true,
                AllowPartialResults = true,
                BatchSize = 20,
                Collation = new Collation("en_US"),
                Comment = "funny",
                CursorType = CursorType.TailableAwait,
                Let = letDocument,
                Limit = 30,
                MaxAwaitTime = TimeSpan.FromSeconds(4),
                MaxTime = TimeSpan.FromSeconds(3),
                NoCursorTimeout = true,
#pragma warning disable 618
                OplogReplay = true,
#pragma warning restore 618
                Projection = projectionDefinition,
                Skip = 40,
                Sort = sortDefinition
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.FindAsync(session, filterExpression, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindSync(session, filterExpression, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.FindAsync(filterExpression, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindSync(filterExpression, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<FindOperation<BsonDocument>>();
            operation.AllowDiskUse.ShouldBe(options.AllowDiskUse);
            operation.AllowPartialResults.ShouldBe(options.AllowPartialResults);
            operation.BatchSize.ShouldBe(options.BatchSize);
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Comment.ShouldBe((BsonValue)"funny");
            operation.CursorType.ShouldBe(CursorType.TailableAwait);
            operation.Filter.ShouldBe(new BsonDocument("x", 1));
            operation.Let.ShouldBe(options.Let);
            operation.Limit.ShouldBe(options.Limit);
            operation.MaxAwaitTime.ShouldBe(options.MaxAwaitTime);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.NoCursorTimeout.ShouldBe(options.NoCursorTimeout);
#pragma warning disable 618
            operation.OplogReplay.ShouldBe(options.OplogReplay);
#pragma warning restore 618
            operation.Projection.ShouldBe(projectionDocument);
            operation.ReadConcern.ShouldBe(_readConcern);
            operation.ResultSerializer.ValueType.ShouldBe(typeof(BsonDocument));
            operation.RetryRequested.ShouldBeTrue();
            operation.Skip.ShouldBe(options.Skip);
            operation.Sort.ShouldBe(sortDocument);
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_with_Projection_As_should_execute_correctly(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<A>();
            var session = CreateSession(usingSession);
            var filterDefinition = Builders<A>.Filter.Empty;
            var letDocument = BsonDocument.Parse("{ name : 'name' }");
            var options = new FindOptions<A, BsonDocument>
            {
                Projection = Builders<A>.Projection.As<BsonDocument>(),
                Let = letDocument
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.FindAsync(session, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindSync(session, filterDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.FindAsync(filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindSync(filterDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<FindOperation<BsonDocument>>();
            operation.Projection.ShouldBeNull();
            operation.Let.ShouldBe(letDocument);
            operation.ResultSerializer.ShouldBeOfType<BsonDocumentSerializer>();
            operation.ReadConcern.ShouldBe(_readConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndDelete_should_execute_a_FindOneAndDeleteOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ x : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var projectionDocument = BsonDocument.Parse("{ x : 1 }");
            var projectionDefinition = (ProjectionDefinition<BsonDocument, BsonDocument>)projectionDocument;
            var sortDocument = BsonDocument.Parse("{ a : -1 } ");
            var sortDefinition = (SortDefinition<BsonDocument>)sortDocument;
            var letDocument = let != null ? BsonDocument.Parse("{ name : 'name' }") : null;
            var options = new FindOneAndDeleteOptions<BsonDocument, BsonDocument>
            {
                Collation = new Collation("en_US"),
                Let = letDocument,
                Projection = projectionDefinition,
                Sort = sortDefinition,
                MaxTime = TimeSpan.FromSeconds(2)
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.FindOneAndDeleteAsync(session, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndDelete(session, filterDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.FindOneAndDeleteAsync(filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndDelete(filterDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<FindOneAndDeleteOperation<BsonDocument>>();
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Filter.ShouldBe(filterDocument);
            operation.Let.ShouldBe(letDocument);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.Projection.ShouldBe(projectionDocument);
            operation.ResultSerializer.ShouldBeOfType<FindAndModifyValueDeserializer<BsonDocument>>();
            operation.Sort.ShouldBe(sortDocument);
            operation.WriteConcern.ShouldBeSameAs(subject.Settings.WriteConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndDelete_with_Projection_As_should_execute_correctly(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<A>();
            var session = CreateSession(usingSession);
            var filterDefinition = Builders<A>.Filter.Empty;
            var letDocument = BsonDocument.Parse("{ name : 'name' }");
            var options = new FindOneAndDeleteOptions<A, BsonDocument>
            {
                Collation = new Collation("en_US"),
                Hint = new BsonDocument("_id", 1),
                Let = letDocument,
                Projection = Builders<A>.Projection.As<BsonDocument>()
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.FindOneAndDeleteAsync(session, filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndDelete(session, filterDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.FindOneAndDeleteAsync(filterDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndDelete(filterDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<FindOneAndDeleteOperation<BsonDocument>>();
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.Hint.ShouldBe(options.Hint);
            operation.Let.ShouldBe(options.Let);
            operation.Projection.ShouldBeNull();
            operation.ResultSerializer.ShouldBeOfType<FindAndModifyValueDeserializer<BsonDocument>>();
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndReplace_should_execute_a_FindOneAndReplaceOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(ReturnDocument.After, ReturnDocument.Before)] ReturnDocument returnDocument,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ x : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var replacement = BsonDocument.Parse("{ a : 2 }");
            var projectionDocument = BsonDocument.Parse("{ x : 1 }");
            var projectionDefinition = (ProjectionDefinition<BsonDocument, BsonDocument>)projectionDocument;
            var sortDocument = BsonDocument.Parse("{ a : -1 }");
            var sortDefinition = (SortDefinition<BsonDocument>)sortDocument;
            var letDocument = BsonDocument.Parse("{ name : 'name' }");
            var options = new FindOneAndReplaceOptions<BsonDocument, BsonDocument>()
            {
                BypassDocumentValidation = bypassDocumentValidation,
                Collation = new Collation("en_US"),
                Hint = new BsonDocument("_id", 1),
                IsUpsert = isUpsert,
                Let = letDocument,
                MaxTime = TimeSpan.FromSeconds(2),
                Projection = projectionDefinition,
                ReturnDocument = returnDocument,
                Sort = sortDefinition
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.FindOneAndReplaceAsync(session, filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndReplace(session, filterDefinition, replacement, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.FindOneAndReplaceAsync(filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndReplace(filterDefinition, replacement, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<FindOneAndReplaceOperation<BsonDocument>>();
            operation.BypassDocumentValidation.ShouldBe(bypassDocumentValidation);
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Hint.ShouldBe(options.Hint);
            operation.Filter.ShouldBe(filterDocument);
            operation.IsUpsert.ShouldBe(isUpsert);
            operation.Let.ShouldBe(options.Let);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.Projection.ShouldBe(projectionDocument);
            operation.Replacement.ShouldBe(replacement);
            operation.ResultSerializer.ShouldBeOfType<FindAndModifyValueDeserializer<BsonDocument>>();
            operation.ReturnDocument.ShouldBe(returnDocument);
            operation.Sort.ShouldBe(sortDocument);
            operation.WriteConcern.ShouldBeSameAs(subject.Settings.WriteConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndReplace_with_Projection_As_should_execute_correctly(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<A>();
            var session = CreateSession(usingSession);
            var filterDefinition = Builders<A>.Filter.Empty;
            var replacement = new A();
            var letDocument = BsonDocument.Parse("{ name : 'name' }");
            var options = new FindOneAndReplaceOptions<A, BsonDocument>
            {
                Collation = new Collation("en_US"),
                Let = letDocument,
                Projection = Builders<A>.Projection.As<BsonDocument>()
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.FindOneAndReplaceAsync(session, filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndReplace(session, filterDefinition, replacement, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.FindOneAndReplaceAsync(filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndReplace(filterDefinition, replacement, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<FindOneAndReplaceOperation<BsonDocument>>();
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.Let.ShouldBeSameAs(options.Let);
            operation.Projection.ShouldBeNull();
            operation.ResultSerializer.ShouldBeOfType<FindAndModifyValueDeserializer<BsonDocument>>();
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndUpdate_should_execute_a_FindOneAndUpdateOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(ReturnDocument.After, ReturnDocument.Before)] ReturnDocument returnDocument,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ x : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var updateDocument = BsonDocument.Parse("{ $set : { a : 2 } }");
            var updateDefinition = (UpdateDefinition<BsonDocument>)updateDocument;
            var arrayFilterDocument = BsonDocument.Parse("{ b : 1 }");
            var arrayFilterDefinition = (ArrayFilterDefinition<BsonDocument>)arrayFilterDocument;
            var projectionDocument = BsonDocument.Parse("{ x : 1 }");
            var projectionDefinition = (ProjectionDefinition<BsonDocument, BsonDocument>)projectionDocument;
            var sortDocument = BsonDocument.Parse("{ a : -1 }");
            var sortDefinition = (SortDefinition<BsonDocument>)sortDocument;
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            var options = new FindOneAndUpdateOptions<BsonDocument, BsonDocument>()
            {
                ArrayFilters = new[] { arrayFilterDefinition },
                BypassDocumentValidation = bypassDocumentValidation,
                Collation = new Collation("en_US"),
                Hint = new BsonDocument("_id", 1),
                IsUpsert = isUpsert,
                Let = letDocument,
                MaxTime = TimeSpan.FromSeconds(2),
                Projection = projectionDefinition,
                ReturnDocument = returnDocument,
                Sort = sortDefinition,
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.FindOneAndUpdateAsync(session, filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndUpdate(session, filterDefinition, updateDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.FindOneAndUpdateAsync(filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndUpdate(filterDefinition, updateDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<FindOneAndUpdateOperation<BsonDocument>>();
            operation.ArrayFilters.ShouldBe(new[] { arrayFilterDocument });
            operation.BypassDocumentValidation.ShouldBe(bypassDocumentValidation);
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Hint.ShouldBe(options.Hint);
            operation.Filter.ShouldBe(filterDocument);
            operation.IsUpsert.ShouldBe(isUpsert);
            operation.Let.ShouldBe(options.Let);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.Projection.ShouldBe(projectionDocument);
            operation.ResultSerializer.ShouldBeOfType<FindAndModifyValueDeserializer<BsonDocument>>();
            operation.ReturnDocument.ShouldBe(returnDocument);
            operation.Sort.ShouldBe(sortDocument);
            operation.Update.ShouldBe(updateDocument);
            operation.WriteConcern.ShouldBeSameAs(subject.Settings.WriteConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndUpdate_should_throw_if_parameters_are_invalid(
            [Values(false)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new[] { new BsonDocument("$project", "{ value : 1 }") });
            var update = new PipelineUpdateDefinition<BsonDocument>(pipeline);
            var filterDocument = BsonDocument.Parse("{ x : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var arrayFilterDocument = BsonDocument.Parse("{ b : 1 }");
            var arrayFilterDefinition = (ArrayFilterDefinition<BsonDocument>)arrayFilterDocument;
            var options = new FindOneAndUpdateOptions<BsonDocument, BsonDocument>()
            {
                ArrayFilters = new[] { arrayFilterDefinition },
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Exception exception;
            if (async)
            {
                exception = Record.ExceptionAsync(async () => { await subject.FindOneAndUpdateAsync(filterDefinition, update, options, cancellationToken); })
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                exception = Record.Exception(() => { subject.FindOneAndUpdate(filterDefinition, update, options, cancellationToken); });
            }

            exception.ShouldBeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndUpdate_with_Projection_As_should_execute_correctly(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<A>();
            var session = CreateSession(usingSession);
            var filterDefinition = Builders<A>.Filter.Empty;
            var updateDefinition = Builders<A>.Update.Inc(x => x.PropA, 1);
            var letDocument = BsonDocument.Parse("{ name : 'name' }");
            var options = new FindOneAndUpdateOptions<A, BsonDocument>
            {
                Projection = Builders<A>.Projection.As<BsonDocument>(),
                Let = letDocument
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.FindOneAndUpdateAsync(session, filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndUpdate(session, filterDefinition, updateDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.FindOneAndUpdateAsync(filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.FindOneAndUpdate(filterDefinition, updateDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<FindOneAndUpdateOperation<BsonDocument>>();
            operation.Let.ShouldBe(options.Let);
            operation.Projection.ShouldBeNull();
            operation.ResultSerializer.ShouldBeOfType<FindAndModifyValueDeserializer<BsonDocument>>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_CreateOne_should_execute_a_CreateIndexesOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingWildcardIndex,
            [Values(null, 1, 2)] int? commitQuorumW,
            [Values(null, -1, 0, 42, 9000)] int? milliseconds,
            [Values(false, true)] bool usingCreateOneIndexOptions,
            [Values(null, false, true)] bool? hidden,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var keysDocument = new BsonDocument("x", 1);
            var keysDefinition =
                usingWildcardIndex
                    ? Builders<BsonDocument>.IndexKeys.Wildcard()
                    : keysDocument;
            var partialFilterDocument = BsonDocument.Parse("{ x : { $gt : 0 } }");
            var partialFilterDefinition = (FilterDefinition<BsonDocument>)partialFilterDocument;
            var weights = new BsonDocument("y", 1);
            var storageEngine = new BsonDocument("awesome", true);
            var commitQuorum = commitQuorumW.HasValue ? CreateIndexCommitQuorum.Create(commitQuorumW.Value) : null;
            var maxTime = milliseconds != null ? TimeSpan.FromMilliseconds(milliseconds.Value) : (TimeSpan?)null;
            var createOneIndexOptions = usingCreateOneIndexOptions ? new CreateOneIndexOptions { CommitQuorum = commitQuorum, MaxTime = maxTime } : null;
            var wildcardProjectionDefinition = Builders<BsonDocument>.Projection.Include("w");
            var options = new CreateIndexOptions<BsonDocument>
            {
                Background = true,
                Bits = 10,
#pragma warning disable 618
                BucketSize = 20,
#pragma warning restore 618
                Collation = new Collation("en_US"),
                DefaultLanguage = "en",
                ExpireAfter = TimeSpan.FromSeconds(20),
                Hidden = hidden,
                LanguageOverride = "es",
                Max = 30,
                Min = 40,
                Name = "awesome",
                PartialFilterExpression = partialFilterDefinition,
                Sparse = false,
                SphereIndexVersion = 50,
                StorageEngine = storageEngine,
                TextIndexVersion = 60,
                Unique = true,
                Version = 70,
                Weights = weights
            };
            if (usingWildcardIndex)
            {
                options.WildcardProjection = wildcardProjectionDefinition;
            }

            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var model = new CreateIndexModel<BsonDocument>(keysDefinition, options);

            if (usingSession)
            {
                if (async)
                {
                    subject.Indexes.CreateOneAsync(session, model, createOneIndexOptions, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Indexes.CreateOne(session, model, createOneIndexOptions, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.Indexes.CreateOneAsync(model, createOneIndexOptions, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Indexes.CreateOne(model, createOneIndexOptions, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<CreateIndexesOperation>();
            operation.CollectionNamespace.FullName.ShouldBe("foo.bar");
            operation.CommitQuorum.ShouldBeSameAs(createOneIndexOptions?.CommitQuorum);
            operation.MaxTime.ShouldBe(createOneIndexOptions?.MaxTime);
            operation.Requests.Count().ShouldBe(1);
            operation.WriteConcern.ShouldBeSameAs(writeConcern);

            var request = operation.Requests.Single();
            request.AdditionalOptions.ShouldBeNull();
            request.Background.ShouldBe(options.Background);
            request.Bits.ShouldBe(options.Bits);
#pragma warning disable 618
            request.BucketSize.ShouldBe(options.BucketSize);
#pragma warning restore 618
            request.Collation.ShouldBeSameAs(options.Collation);
            request.DefaultLanguage.ShouldBe(options.DefaultLanguage);
            request.ExpireAfter.ShouldBe(options.ExpireAfter);
            request.Hidden.ShouldBe(options.Hidden);
            var expectedKeysResult =
                usingWildcardIndex
                    ? new BsonDocument("$**", 1)
                    : keysDocument;
            request.Keys.ShouldBe(expectedKeysResult);
            request.LanguageOverride.ShouldBe(options.LanguageOverride);
            request.Max.ShouldBe(options.Max);
            request.Min.ShouldBe(options.Min);
            request.Name.ShouldBe(options.Name);
            request.PartialFilterExpression.ShouldBe(partialFilterDocument);
            request.Sparse.ShouldBe(options.Sparse);
            request.SphereIndexVersion.ShouldBe(options.SphereIndexVersion);
            request.StorageEngine.ShouldBe(options.StorageEngine);
            request.TextIndexVersion.ShouldBe(options.TextIndexVersion);
            request.Unique.ShouldBe(options.Unique);
            request.Version.ShouldBe(options.Version);
            request.Weights.ShouldBe(options.Weights);
            if (usingWildcardIndex)
            {
                var wildcardProjection = wildcardProjectionDefinition.Render(new(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));
                request.WildcardProjection.ShouldBe(wildcardProjection);
            }
            else
            {
                request.WildcardProjection.ShouldBeNull();
            }
            request.GetIndexName().ShouldBe(options.Name);
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_CreateMany_should_execute_a_CreateIndexesOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingWildcardIndex,
            [Values(null, 1, 2)] int? commitQuorumW,
            [Values(null, -1, 0, 42, 9000)] int? milliseconds,
            [Values(false, true)] bool usingCreateManyIndexesOptions,
            [Values(null, false, true)] bool? hidden,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var keysDocument1 = new BsonDocument("x", 1);
            var keysDocument2 = new BsonDocument("z", 1);
            var keysDefinition1 =
                usingWildcardIndex
                    ? Builders<BsonDocument>.IndexKeys.Wildcard()
                    : keysDocument1;
            var keysDefinition2 = (IndexKeysDefinition<BsonDocument>)keysDocument2;
            var partialFilterDocument = BsonDocument.Parse("{ x : { $gt : 0 } }");
            var partialFilterDefinition = (FilterDefinition<BsonDocument>)partialFilterDocument;
            var weights = new BsonDocument("y", 1);
            var wildcardProjectionDefinition = Builders<BsonDocument>.Projection.Include("w");
            var storageEngine = new BsonDocument("awesome", true);
            var commitQuorum = commitQuorumW.HasValue ? CreateIndexCommitQuorum.Create(commitQuorumW.Value) : null;
            var maxTime = milliseconds != null ? TimeSpan.FromMilliseconds(milliseconds.Value) : (TimeSpan?)null;
            var createManyIndexesOptions = usingCreateManyIndexesOptions ? new CreateManyIndexesOptions { CommitQuorum = commitQuorum, MaxTime = maxTime } : null;

            var options = new CreateIndexOptions<BsonDocument>
            {
                Background = true,
                Bits = 10,
#pragma warning disable 618
                BucketSize = 20,
#pragma warning restore 618
                Collation = new Collation("en_US"),
                DefaultLanguage = "en",
                ExpireAfter = TimeSpan.FromSeconds(20),
                Hidden = hidden,
                LanguageOverride = "es",
                Max = 30,
                Min = 40,
                Name = "awesome",
                PartialFilterExpression = partialFilterDefinition,
                Sparse = false,
                SphereIndexVersion = 50,
                StorageEngine = storageEngine,
                TextIndexVersion = 60,
                Unique = true,
                Version = 70,
                Weights = weights
            };
            if (usingWildcardIndex)
            {
                options.WildcardProjection = wildcardProjectionDefinition;
            }

            var model1 = new CreateIndexModel<BsonDocument>(keysDefinition1, options);
            var model2 = new CreateIndexModel<BsonDocument>(keysDefinition2);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.Indexes.CreateManyAsync(session, new[] { model1, model2 }, createManyIndexesOptions, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Indexes.CreateMany(session, new[] { model1, model2 }, createManyIndexesOptions, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.Indexes.CreateManyAsync(new[] { model1, model2 }, createManyIndexesOptions, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Indexes.CreateMany(new[] { model1, model2 }, createManyIndexesOptions, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<CreateIndexesOperation>();
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.CommitQuorum.ShouldBeSameAs(createManyIndexesOptions?.CommitQuorum);
            operation.MaxTime.ShouldBe(createManyIndexesOptions?.MaxTime);
            operation.Requests.Count().ShouldBe(2);
            operation.WriteConcern.ShouldBeSameAs(writeConcern);

            var request1 = operation.Requests.ElementAt(0);
            request1.AdditionalOptions.ShouldBeNull();
            request1.Background.ShouldBe(options.Background);
            request1.Bits.ShouldBe(options.Bits);
#pragma warning disable 618
            request1.BucketSize.ShouldBe(options.BucketSize);
#pragma warning restore 618
            request1.Collation.ShouldBeSameAs(options.Collation);
            request1.DefaultLanguage.ShouldBe(options.DefaultLanguage);
            request1.ExpireAfter.ShouldBe(options.ExpireAfter);
            request1.Hidden.ShouldBe(options.Hidden);
            var expectedKeysResult =
                usingWildcardIndex
                    ? new BsonDocument("$**", 1)
                    : keysDocument1;
            request1.Keys.ShouldBe(expectedKeysResult);
            request1.LanguageOverride.ShouldBe(options.LanguageOverride);
            request1.Max.ShouldBe(options.Max);
            request1.Min.ShouldBe(options.Min);
            request1.Name.ShouldBe(options.Name);
            request1.PartialFilterExpression.ShouldBe(partialFilterDocument);
            request1.Sparse.ShouldBe(options.Sparse);
            request1.SphereIndexVersion.ShouldBe(options.SphereIndexVersion);
            request1.StorageEngine.ShouldBe(storageEngine);
            request1.TextIndexVersion.ShouldBe(options.TextIndexVersion);
            request1.Unique.ShouldBe(options.Unique);
            request1.Version.ShouldBe(options.Version);
            request1.Weights.ShouldBe(weights);
            if (usingWildcardIndex)
            {
                var wildcardProjection = wildcardProjectionDefinition.Render(new(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));
                request1.WildcardProjection.ShouldBe(wildcardProjection);
            }
            else
            {
                request1.WildcardProjection.ShouldBeNull();
            }

            request1.GetIndexName().ShouldBe(options.Name);

            var request2 = operation.Requests.ElementAt(1);
            request2.AdditionalOptions.ShouldBeNull();
            request2.Background.ShouldNotHaveValue();
            request2.Bits.ShouldNotHaveValue();
#pragma warning disable 618
            request2.BucketSize.ShouldNotHaveValue();
#pragma warning restore 618
            request2.Collation.ShouldBeNull();
            request2.DefaultLanguage.ShouldBeNull();
            request2.ExpireAfter.ShouldNotHaveValue();
            request2.Hidden.ShouldNotHaveValue();
            request2.Keys.ShouldBe(keysDocument2);
            request2.LanguageOverride.ShouldBeNull();
            request2.Max.ShouldNotHaveValue();
            request2.Min.ShouldNotHaveValue(); ;
            request2.Name.ShouldBeNull();
            request2.PartialFilterExpression.ShouldBeNull();
            request2.Sparse.ShouldNotHaveValue(); ;
            request2.SphereIndexVersion.ShouldNotHaveValue();
            request2.StorageEngine.ShouldBeNull();
            request2.TextIndexVersion.ShouldNotHaveValue();
            request2.Unique.ShouldNotHaveValue();
            request2.Version.ShouldNotHaveValue();
            request2.Weights.ShouldBeNull();
            request2.WildcardProjection.ShouldBeNull();
            request2.GetIndexName().ShouldBe("z_1");
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_DropAll_should_execute_a_DropIndexOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var maxTime = TimeSpan.FromMilliseconds(42);
            var options = new DropIndexOptions { MaxTime = maxTime };

            if (usingSession)
            {
                if (async)
                {
                    subject.Indexes.DropAllAsync(session, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Indexes.DropAll(session, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.Indexes.DropAllAsync(options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Indexes.DropAll(options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<DropIndexOperation>();
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.IndexName.ShouldBe("*");
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.WriteConcern.ShouldBeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_DropOne_should_execute_a_DropIndexOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var maxTime = TimeSpan.FromMilliseconds(42);
            var options = new DropIndexOptions { MaxTime = maxTime };

            if (usingSession)
            {
                if (async)
                {
                    subject.Indexes.DropOneAsync(session, "name", options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Indexes.DropOne(session, "name", options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.Indexes.DropOneAsync("name", options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Indexes.DropOne("name", options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<DropIndexOperation>();
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.IndexName.ShouldBe("name");
            operation.WriteConcern.ShouldBeSameAs(writeConcern);
            operation.MaxTime.ShouldBe(maxTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_DropOne_should_throw_an_exception_if_an_asterisk_is_used(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.Indexes.DropOneAsync("*", cancellationToken).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Indexes.DropOne("*", cancellationToken));
            }

            var e = exception.ShouldBeOfType<ArgumentException>();
            e.ParamName.ShouldBe("name");
        }

        [Theory]
        [ParameterAttributeData]
        public void Indexes_List_should_execute_a_ListIndexesOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, 3)] int? batchSize,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (batchSize.HasValue)
            {
                var listIndexOptions = batchSize.HasValue ? new ListIndexesOptions() { BatchSize = batchSize.Value } : null;

                if (usingSession)
                {
                    if (async)
                    {
                        subject.Indexes.ListAsync(session, options: listIndexOptions, cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        subject.Indexes.List(session, options: listIndexOptions, cancellationToken);
                    }
                }
                else
                {
                    if (async)
                    {
                        subject.Indexes.ListAsync(options: listIndexOptions, cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        subject.Indexes.List(options: listIndexOptions, cancellationToken);
                    }
                }
            }
            else
            {
                if (usingSession)
                {
                    if (async)
                    {
                        subject.Indexes.ListAsync(session, cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        subject.Indexes.List(session, cancellationToken);
                    }
                }
                else
                {
                    if (async)
                    {
                        subject.Indexes.ListAsync(cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        subject.Indexes.List(cancellationToken);
                    }
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<ListIndexesOperation>();
            operation.BatchSize.ShouldBe(batchSize);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.RetryRequested.ShouldBeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertOne_should_execute_a_BulkMixedOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var document = BsonDocument.Parse("{ _id : 1, a : 1 }");
            var options = new InsertOneOptions();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(1, new[] { processedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            if (usingSession)
            {
                if (async)
                {
                    subject.InsertOneAsync(session, document, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.InsertOne(session, document, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.InsertOneAsync(document, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.InsertOne(document, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            VerifySingleWrite(call, bypassDocumentValidation: null, isOrdered: true, let: null, processedRequest);
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertOne_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var document = BsonDocument.Parse("{ _id : 1, a : 1 }");
            var options = new InsertOneOptions();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationException = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 0,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { processedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());
            _operationExecutor.EnqueueException<BulkWriteOperationResult>(operationException);

            Exception exception;
            if (usingSession)
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.InsertOneAsync(session, document, options, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.InsertOne(session, document, options, cancellationToken));
                }
            }
            else
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.InsertOneAsync(document, options, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.InsertOne(document, options, cancellationToken));
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            exception.ShouldBeOfType<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertOne_should_respect_AssignIdOnInsert(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool assignIdOnInsert,
            [Values(false, true)] bool async)
        {
            var settings = new MongoCollectionSettings { AssignIdOnInsert = assignIdOnInsert };
            var subject = CreateSubject<BsonDocument>(settings: settings);
            var session = CreateSession(usingSession);
            var document = BsonDocument.Parse("{ a : 1 }");
            var options = new InsertOneOptions();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(1, new[] { processedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            if (usingSession)
            {
                if (async)
                {
                    subject.InsertOneAsync(session, document, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.InsertOne(session, document, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.InsertOneAsync(document, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.InsertOne(document, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            VerifySingleWrite(call, bypassDocumentValidation: null, isOrdered: true, let: null, processedRequest);

            var operation = call.Operation.ShouldBeOfType<BulkMixedWriteOperation>();
            var requests = operation.Requests.ToList(); // call ToList to force evaluation
            document.Contains("_id").ShouldBe(assignIdOnInsert);
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertMany_should_execute_a_BulkMixedOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isOrdered,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var documents = new[]
            {
                BsonDocument.Parse("{ _id : 1, a : 1 }"),
                BsonDocument.Parse("{ _id : 2, a : 2 }")
            };
            var options = new InsertManyOptions
            {
                BypassDocumentValidation = bypassDocumentValidation,
                IsOrdered = isOrdered
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequests = new[]
            {
                new InsertRequest(documents[0]) { CorrelationId = 0 },
                new InsertRequest(documents[1]) { CorrelationId = 1 }
            };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(2, processedRequests);
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            if (usingSession)
            {
                if (async)
                {
                    subject.InsertManyAsync(session, documents, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.InsertMany(session, documents, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.InsertManyAsync(documents, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.InsertMany(documents, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            VerifyBulkWrite(call, bypassDocumentValidation: bypassDocumentValidation, isOrdered: isOrdered, let: null, processedRequests);
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertMany_should_respect_AssignIdOnInsert(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool assignIdOnInsert,
            [Values(false, true)] bool async)
        {
            var settings = new MongoCollectionSettings { AssignIdOnInsert = assignIdOnInsert };
            var subject = CreateSubject<BsonDocument>(settings: settings);
            var session = CreateSession(usingSession);
            var document = BsonDocument.Parse("{ a : 1 }");
            var options = new InsertManyOptions();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new InsertRequest(document) { CorrelationId = 0 };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(1, new[] { processedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            if (usingSession)
            {
                if (async)
                {
                    subject.InsertManyAsync(session, new[] { document }, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.InsertMany(session, new[] { document }, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.InsertManyAsync(new[] { document }, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.InsertMany(new[] { document }, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            VerifySingleWrite(call, bypassDocumentValidation: null, isOrdered: true, let: null, processedRequest);

            var operation = (BulkMixedWriteOperation)call.Operation;
            var requests = operation.Requests.ToList(); // call ToList to force evaluation
            document.Contains("_id").ShouldBe(assignIdOnInsert);
        }

        [Theory]
        [ParameterAttributeData]
        public void MapReduce_with_inline_output_mode_should_execute_a_MapReduceOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var map = new BsonJavaScript("map");
            var reduce = new BsonJavaScript("reduce");
            var filterDocument = new BsonDocument("filter", 1);
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var sortDocument = new BsonDocument("sort", 1);
            var sortDefinition = (SortDefinition<BsonDocument>)sortDocument;
#pragma warning disable CS0618 // Type or member is obsolete
            var options = new MapReduceOptions<BsonDocument, BsonDocument>
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Collation = new Collation("en_US"),
                Filter = filterDefinition,
                Finalize = new BsonJavaScript("finalizer"),
#pragma warning disable 618
                JavaScriptMode = true,
#pragma warning restore 618
                Limit = 10,
                MaxTime = TimeSpan.FromMinutes(2),
#pragma warning disable CS0618 // Type or member is obsolete
                OutputOptions = MapReduceOutputOptions.Inline,
#pragma warning restore CS0618 // Type or member is obsolete
                Scope = new BsonDocument("test", 3),
                Sort = sortDefinition,
                Verbose = true
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    subject.MapReduceAsync(session, map, reduce, options, cancellationToken).GetAwaiter().GetResult();
#pragma warning restore CS0618 // Type or member is obsolete
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    subject.MapReduce(session, map, reduce, options, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
            else
            {
                if (async)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    subject.MapReduceAsync(map, reduce, options, cancellationToken).GetAwaiter().GetResult();
#pragma warning restore CS0618 // Type or member is obsolete
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    subject.MapReduce(map, reduce, options, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
            var operation = call.Operation.ShouldBeOfType<MapReduceOperation<BsonDocument>>();
#pragma warning restore CS0618 // Type or member is obsolete
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Filter.ShouldBe(filterDocument);
            operation.FinalizeFunction.ShouldBe(options.Finalize);
#pragma warning disable 618
            operation.JavaScriptMode.ShouldBe(options.JavaScriptMode);
#pragma warning restore 618
            operation.Limit.ShouldBe(options.Limit);
            operation.MapFunction.ShouldBe(map);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.ReadConcern.ShouldBe(subject.Settings.ReadConcern);
            operation.ReduceFunction.ShouldBe(reduce);
            operation.ResultSerializer.ShouldBe(BsonDocumentSerializer.Instance);
            operation.Scope.ShouldBe(options.Scope);
            operation.Sort.ShouldBe(sortDocument);
            operation.Verbose.ShouldBe(options.Verbose);
        }

        [Theory]
        [ParameterAttributeData]
        public void MapReduce_with_collection_output_mode_should_execute_a_MapReduceOutputToCollectionOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject<BsonDocument>().WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var map = new BsonJavaScript("map");
            var reduce = new BsonJavaScript("reduce");
            var filterDocument = new BsonDocument("filter", 1);
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var sortDocument = new BsonDocument("sort", 1);
            var sortDefinition = (SortDefinition<BsonDocument>)sortDocument;
#pragma warning disable CS0618 // Type or member is obsolete
            var options = new MapReduceOptions<BsonDocument, BsonDocument>
#pragma warning restore CS0618 // Type or member is obsolete
            {
                BypassDocumentValidation = true,
                Collation = new Collation("en_US"),
                Filter = filterDefinition,
                Finalize = new BsonJavaScript("finalizer"),
#pragma warning disable 618
                JavaScriptMode = true,
#pragma warning restore 618
                Limit = 10,
                MaxTime = TimeSpan.FromMinutes(2),
#pragma warning disable 618
                OutputOptions = MapReduceOutputOptions.Replace("awesome", "otherDB", true),
#pragma warning restore 618
                Scope = new BsonDocument("test", 3),
                Sort = sortDefinition,
                Verbose = true
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    subject.MapReduceAsync(session, map, reduce, options, cancellationToken).GetAwaiter().GetResult();
#pragma warning restore CS0618 // Type or member is obsolete
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    subject.MapReduce(session, map, reduce, options, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
            else
            {
                if (async)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    subject.MapReduceAsync(map, reduce, options, cancellationToken).GetAwaiter().GetResult();
#pragma warning restore CS0618 // Type or member is obsolete
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    subject.MapReduce(map, reduce, options, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
            var operation = call.Operation.ShouldBeOfType<MapReduceOutputToCollectionOperation>();
#pragma warning restore CS0618 // Type or member is obsolete
            operation.BypassDocumentValidation.ShouldBe(options.BypassDocumentValidation);
            operation.Collation.ShouldBeSameAs(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.Filter.ShouldBe(filterDocument);
            operation.FinalizeFunction.ShouldBe(options.Finalize);
#pragma warning disable 618
            operation.JavaScriptMode.ShouldBe(options.JavaScriptMode);
#pragma warning restore 618
            operation.Limit.ShouldBe(options.Limit);
            operation.MapFunction.ShouldBe(map);
            operation.MaxTime.ShouldBe(options.MaxTime);
#pragma warning disable 618
            operation.NonAtomicOutput.ShouldNotHaveValue();
#pragma warning restore 618
            operation.OutputCollectionNamespace.ShouldBe(CollectionNamespace.FromFullName("otherDB.awesome"));
#pragma warning disable CS0618 // Type or member is obsolete
            operation.OutputMode.ShouldBe(Core.Operations.MapReduceOutputMode.Replace);
#pragma warning restore CS0618 // Type or member is obsolete
            operation.ReduceFunction.ShouldBe(reduce);
            operation.Scope.ShouldBe(options.Scope);
#pragma warning disable 618
            operation.ShardedOutput.ShouldBe(true);
#pragma warning restore 618
            operation.Sort.ShouldBe(sortDocument);
            operation.Verbose.ShouldBe(options.Verbose);
            operation.WriteConcern.ShouldBeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReplaceOne_should_execute_a_BulkMixedOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ a : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var replacement = BsonDocument.Parse("{ a : 2 }");
            var collation = new Collation("en_US");
            var hint = new BsonDocument("x", 1);
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new UpdateRequest(UpdateType.Replacement, filterDocument, replacement)
            {
                Collation = collation,
                Hint = hint,
                CorrelationId = 0,
                IsUpsert = isUpsert,
                IsMulti = false
            };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { processedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            assertReplaceOne();

            var replaceOptions = new ReplaceOptions()
            {
                BypassDocumentValidation = bypassDocumentValidation,
                Collation = collation,
                Hint = hint,
                IsUpsert = isUpsert,
                Let = letDocument
            };
            assertReplaceOneWithReplaceOptions(replaceOptions);

            var updateOptions = new UpdateOptions
            {
                BypassDocumentValidation = bypassDocumentValidation,
                Hint = hint,
                Collation = collation,
                IsUpsert = isUpsert,
                Let = letDocument
            };
            assertReplaceOneWithUpdateOptions(updateOptions);

            void assertReplaceOne()
            {
                if (usingSession)
                {
                    if (async)
                    {
                        subject.ReplaceOneAsync(session, filterDefinition, replacement, cancellationToken: cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        subject.ReplaceOne(session, filterDefinition, replacement, cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    if (async)
                    {
                        subject.ReplaceOneAsync(filterDefinition, replacement, cancellationToken: cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        subject.ReplaceOne(filterDefinition, replacement, cancellationToken: cancellationToken);
                    }
                }

                assertOperationResult(expectedBypassDocumentValidation: null, expectedLet: null);
            }

            void assertReplaceOneWithReplaceOptions(ReplaceOptions options)
            {
                if (usingSession)
                {
                    if (async)
                    {
                        subject.ReplaceOneAsync(session, filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        subject.ReplaceOne(session, filterDefinition, replacement, options, cancellationToken);
                    }
                }
                else
                {
                    if (async)
                    {
                        subject.ReplaceOneAsync(filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        subject.ReplaceOne(filterDefinition, replacement, options, cancellationToken);
                    }
                }

                assertOperationResult(expectedBypassDocumentValidation: bypassDocumentValidation, expectedLet: letDocument);
            }

            void assertReplaceOneWithUpdateOptions(UpdateOptions options)
            {
                if (usingSession)
                {
                    if (async)
                    {
#pragma warning disable 618
                        subject.ReplaceOneAsync(session, filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult();
#pragma warning restore 618
                    }
                    else
                    {
#pragma warning disable 618
                        subject.ReplaceOne(session, filterDefinition, replacement, options, cancellationToken);
#pragma warning restore 618
                    }
                }
                else
                {
                    if (async)
                    {
#pragma warning disable 618
                        subject.ReplaceOneAsync(filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult();
#pragma warning restore 618
                    }
                    else
                    {
#pragma warning disable 618
                        subject.ReplaceOne(filterDefinition, replacement, options, cancellationToken);
#pragma warning restore 618
                    }
                }

                assertOperationResult(expectedBypassDocumentValidation: bypassDocumentValidation, expectedLet: letDocument);
            }

            void assertOperationResult(bool? expectedBypassDocumentValidation, BsonDocument expectedLet)
            {
                var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
                VerifySessionAndCancellationToken(call, session, cancellationToken);

                VerifySingleWrite(call, bypassDocumentValidation: expectedBypassDocumentValidation, isOrdered: true, let: expectedLet, processedRequest);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReplaceOne_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool usingSession,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ a : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var replacement = BsonDocument.Parse("{ a : 2 }");
            var collation = new Collation("en_US");
            var hint = new BsonDocument("x", 1);
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new UpdateRequest(UpdateType.Replacement, filterDocument, replacement)
            {
                Collation = collation,
                CorrelationId = 0,
                Hint = hint,
                IsUpsert = isUpsert,
                IsMulti = false
            };
            var operationException = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { processedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());
            _operationExecutor.EnqueueException<BulkWriteOperationResult>(operationException);
            _operationExecutor.EnqueueException<BulkWriteOperationResult>(operationException);
            _operationExecutor.EnqueueException<BulkWriteOperationResult>(operationException);

            assertReplaceOne();

            var replaceOptions = new ReplaceOptions
            {
                Collation = collation,
                Hint = hint,
                BypassDocumentValidation = bypassDocumentValidation,
                IsUpsert = isUpsert,
                Let = letDocument
            };
            assertReplaceOneWithReplaceOptions(replaceOptions);

            var updateOptions = new UpdateOptions
            {
                Collation = collation,
                Hint = hint,
                BypassDocumentValidation = bypassDocumentValidation,
                IsUpsert = isUpsert,
                Let = letDocument
            };
            assertReplaceOneWithUpdateOptions(updateOptions);

            void assertReplaceOne()
            {
                Exception exception;

                if (usingSession)
                {
                    if (async)
                    {
                        exception = Record.Exception(() => subject.ReplaceOneAsync(session, filterDefinition, replacement, cancellationToken: cancellationToken).GetAwaiter().GetResult());
                    }
                    else
                    {
                        exception = Record.Exception(() => subject.ReplaceOne(session, filterDefinition, replacement, cancellationToken: cancellationToken));
                    }
                }
                else
                {
                    if (async)
                    {
                        exception = Record.Exception(() => subject.ReplaceOneAsync(filterDefinition, replacement, cancellationToken: cancellationToken).GetAwaiter().GetResult());
                    }
                    else
                    {
                        exception = Record.Exception(() => subject.ReplaceOne(filterDefinition, replacement, cancellationToken: cancellationToken));
                    }
                }

                assertException(exception);
            }

            void assertReplaceOneWithReplaceOptions(ReplaceOptions options)
            {
                Exception exception;

                if (usingSession)
                {
                    if (async)
                    {
                        exception = Record.Exception(() => subject.ReplaceOneAsync(session, filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult());
                    }
                    else
                    {
                        exception = Record.Exception(() => subject.ReplaceOne(session, filterDefinition, replacement, options, cancellationToken));
                    }
                }
                else
                {
                    if (async)
                    {
                        exception = Record.Exception(() => subject.ReplaceOneAsync(filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult());
                    }
                    else
                    {
                        exception = Record.Exception(() => subject.ReplaceOne(filterDefinition, replacement, options, cancellationToken));
                    }
                }

                assertException(exception);
            }

            void assertReplaceOneWithUpdateOptions(UpdateOptions options)
            {
                Exception exception;

                if (usingSession)
                {
                    if (async)
                    {
#pragma warning disable 618
                        exception = Record.Exception(() => subject.ReplaceOneAsync(session, filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult());
#pragma warning restore 618
                    }
                    else
                    {
#pragma warning disable 618
                        exception = Record.Exception(() => subject.ReplaceOne(session, filterDefinition, replacement, options, cancellationToken));
#pragma warning restore 618
                    }
                }
                else
                {
                    if (async)
                    {
#pragma warning disable 618
                        exception = Record.Exception(() => subject.ReplaceOneAsync(filterDefinition, replacement, options, cancellationToken).GetAwaiter().GetResult());
#pragma warning restore 618
                    }
                    else
                    {
#pragma warning disable 618
                        exception = Record.Exception(() => subject.ReplaceOne(filterDefinition, replacement, options, cancellationToken));
#pragma warning restore 618
                    }
                }

                assertException(exception);
            }

            void assertException(Exception exception)
            {
                var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
                VerifySessionAndCancellationToken(call, session, cancellationToken);

                exception.ShouldBeOfType<MongoWriteException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateMany_should_execute_a_BulkMixedOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ a : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var updateDocument = BsonDocument.Parse("{ $set : { a : 1 } }");
            var updateDefinition = (UpdateDefinition<BsonDocument>)updateDocument;
            var arrayFilterDocument = BsonDocument.Parse("{ b : 1 }");
            var arrayFilterDefinition = (ArrayFilterDefinition<BsonDocument>)arrayFilterDocument;
            var collation = new Collation("en_US");
            var hint = new BsonDocument("x", 1);
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            var options = new UpdateOptions
            {
                ArrayFilters = new[] { arrayFilterDefinition },
                BypassDocumentValidation = bypassDocumentValidation,
                Collation = collation,
                Hint = hint,
                IsUpsert = isUpsert,
                Let = letDocument
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new UpdateRequest(UpdateType.Update, filterDocument, updateDocument)
            {
                ArrayFilters = new[] { arrayFilterDocument },
                Collation = collation,
                Hint = hint,
                CorrelationId = 0,
                IsUpsert = isUpsert,
                IsMulti = true
            };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { processedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            if (usingSession)
            {
                if (async)
                {
                    subject.UpdateManyAsync(session, filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.UpdateMany(session, filterDefinition, updateDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.UpdateManyAsync(filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.UpdateMany(filterDefinition, updateDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            VerifySingleWrite(call, bypassDocumentValidation: bypassDocumentValidation, isOrdered: true, let: letDocument, processedRequest);
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateMany_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool usingSession,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ a : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var updateDocument = BsonDocument.Parse("{ $set : { a : 1 } }");
            var updateDefinition = (UpdateDefinition<BsonDocument>)updateDocument;
            var arrayFilterDocument = BsonDocument.Parse("{ b : 1 }");
            var arrayFilterDefinition = (ArrayFilterDefinition<BsonDocument>)arrayFilterDocument;
            var collation = new Collation("en_US");
            var hint = new BsonDocument("x", 1);
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            var updateOptions = new UpdateOptions
            {
                ArrayFilters = new[] { arrayFilterDefinition },
                BypassDocumentValidation = bypassDocumentValidation,
                Collation = collation,
                Hint = hint,
                IsUpsert = isUpsert,
                Let = letDocument
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new UpdateRequest(UpdateType.Update, filterDocument, updateDocument)
            {
                ArrayFilters = new[] { arrayFilterDocument },
                Collation = collation,
                CorrelationId = 0,
                Hint = hint,
                IsUpsert = isUpsert,
                IsMulti = true
            };
            var operationException = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { processedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());
            _operationExecutor.EnqueueException<BulkWriteOperationResult>(operationException);

            Exception exception;
            if (usingSession)
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.UpdateManyAsync(session, filterDefinition, updateDefinition, updateOptions, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.UpdateMany(session, filterDefinition, updateDefinition, updateOptions, cancellationToken));
                }
            }
            else
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.UpdateManyAsync(filterDefinition, updateDefinition, updateOptions, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.UpdateMany(filterDefinition, updateDefinition, updateOptions, cancellationToken));
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            exception.ShouldBeOfType<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateOne_should_execute_a_BulkMixedOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ a : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var updateDocument = BsonDocument.Parse("{ $set : { a : 1 } }");
            var updateDefinition = (UpdateDefinition<BsonDocument>)updateDocument;
            var arrayFilterDocument = BsonDocument.Parse("{ b : 1 }");
            var arrayFilterDefinition = (ArrayFilterDefinition<BsonDocument>)arrayFilterDocument;
            var collation = new Collation("en_US");
            var hint = new BsonDocument("x", 1);
            var letDocument = let != null ? BsonDocument.Parse(let) : null;
            var options = new UpdateOptions
            {
                ArrayFilters = new[] { arrayFilterDefinition },
                BypassDocumentValidation = bypassDocumentValidation,
                Collation = collation,
                Hint = hint,
                IsUpsert = isUpsert,
                Let = letDocument
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new UpdateRequest(UpdateType.Update, filterDocument, updateDocument)
            {
                ArrayFilters = new[] { arrayFilterDocument },
                Collation = collation,
                CorrelationId = 0,
                Hint = hint,
                IsUpsert = isUpsert,
                IsMulti = false
            };
            var operationResult = new BulkWriteOperationResult.Unacknowledged(9, new[] { processedRequest });
            _operationExecutor.EnqueueResult<BulkWriteOperationResult>(operationResult);

            if (usingSession)
            {
                if (async)
                {
                    subject.UpdateOneAsync(session, filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.UpdateOne(session, filterDefinition, updateDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.UpdateOneAsync(filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.UpdateOne(filterDefinition, updateDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            VerifySingleWrite(call, bypassDocumentValidation: bypassDocumentValidation, isOrdered: true, let: letDocument, processedRequest);
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateOne_should_throw_a_WriteException_when_an_error_occurs(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(null, "{ name : 'name' }")] string let,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ a : 1 }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var updateDocument = BsonDocument.Parse("{ $set : { a : 1 } }");
            var updateDefinition = (UpdateDefinition<BsonDocument>)updateDocument;
            var arrayFilterDocument = BsonDocument.Parse("{ b : 1 }");
            var arrayFilterDefinition = (ArrayFilterDefinition<BsonDocument>)arrayFilterDocument;
            var collation = new Collation("en_US");
            var hint = new BsonDocument("x", 1);
            var letDocument = let != null ? BsonDocument.Parse(let) : null;

            var options = new UpdateOptions
            {
                ArrayFilters = new[] { arrayFilterDefinition },
                BypassDocumentValidation = bypassDocumentValidation,
                Collation = collation,
                Hint = hint,
                IsUpsert = isUpsert,
                Let = letDocument
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var processedRequest = new UpdateRequest(UpdateType.Update, filterDocument, updateDocument)
            {
                ArrayFilters = new[] { arrayFilterDocument },
                Collation = collation,
                CorrelationId = 0,
                Hint = hint,
                IsUpsert = isUpsert,
                IsMulti = false
            };
            var operationException = new MongoBulkWriteOperationException(
                _connectionId,
                new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { processedRequest },
                    upserts: new List<BulkWriteOperationUpsert>()),
                new[] { new BulkWriteOperationError(0, 1, "blah", new BsonDocument()) },
                null,
                new List<WriteRequest>());
            _operationExecutor.EnqueueException<BulkWriteOperationResult>(operationException);

            Exception exception;
            if (usingSession)
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.UpdateOneAsync(session, filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.UpdateOne(session, filterDefinition, updateDefinition, options, cancellationToken));
                }
            }
            else
            {
                if (async)
                {
                    exception = Record.Exception(() => subject.UpdateOneAsync(filterDefinition, updateDefinition, options, cancellationToken).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.UpdateOne(filterDefinition, updateDefinition, options, cancellationToken));
                }
            }

            var call = _operationExecutor.GetWriteCall<BulkWriteOperationResult>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            exception.ShouldBeOfType<MongoWriteException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Watch_should_execute_a_ChangeStreamOperation(
            [Values(false, true)] bool usingSession,
            [Values(null, 1)] int? batchSize,
            [Values(null, "a")] string locale,
            [Values(ChangeStreamFullDocumentOption.Default, ChangeStreamFullDocumentOption.WhenAvailable, ChangeStreamFullDocumentOption.UpdateLookup, ChangeStreamFullDocumentOption.Required)] ChangeStreamFullDocumentOption fullDocument,
            [Values(ChangeStreamFullDocumentBeforeChangeOption.Default, ChangeStreamFullDocumentBeforeChangeOption.Off, ChangeStreamFullDocumentBeforeChangeOption.WhenAvailable, ChangeStreamFullDocumentBeforeChangeOption.Required)] ChangeStreamFullDocumentBeforeChangeOption fullDocumentBeforeChange,
            [Values(null, 1)] int? maxAwaitTimeMS,
            [Values(null, ReadConcernLevel.Local)] ReadConcernLevel? readConcernLevel,
            [Values(null, "{ a : 1 }")] string resumeAferString,
            [Values(false, true)] bool async)
        {
            var collation = locale == null ? null : new Collation(locale);
            var maxAwaitTime = maxAwaitTimeMS == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(maxAwaitTimeMS.Value);
            var readConcern = readConcernLevel == null ? null : new ReadConcern(readConcernLevel);
            var resumeAfter = resumeAferString == null ? null : BsonDocument.Parse(resumeAferString);
            var startAfter = new BsonDocument();
            var startAtOperationTime = new BsonTimestamp(1, 2);
            var subject = CreateSubject<BsonDocument>();
            if (readConcern != null)
            {
                subject = subject.WithReadConcern(readConcern);
            }
            var session = CreateSession(usingSession);
            var stageDocument = BsonDocument.Parse("{ $match : { operationType : \"insert\" } }");
            var pipeline = (PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>)(new[] { stageDocument });
            var options = new ChangeStreamOptions
            {
                BatchSize = batchSize,
                Collation = collation,
                FullDocument = fullDocument,
                FullDocumentBeforeChange = fullDocumentBeforeChange,
                MaxAwaitTime = maxAwaitTime,
                ResumeAfter = resumeAfter,
                StartAfter = startAfter,
                StartAtOperationTime = startAtOperationTime
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.WatchAsync(session, pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Watch(session, pipeline, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.WatchAsync(pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Watch(pipeline, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<ChangeStreamOperation<ChangeStreamDocument<BsonDocument>>>();
            operation.BatchSize.ShouldBe(options.BatchSize);
            operation.Collation.ShouldBe(options.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.DatabaseNamespace.ShouldBeNull();
            operation.FullDocument.ShouldBe(options.FullDocument);
            operation.FullDocumentBeforeChange.ShouldBe(options.FullDocumentBeforeChange);
            operation.MaxAwaitTime.ShouldBe(options.MaxAwaitTime);
            operation.MessageEncoderSettings.ShouldNotBeNull();
            operation.Pipeline.ShouldHaveCount(1);
            operation.Pipeline[0].ShouldBe(stageDocument);
            operation.ReadConcern.ShouldBe(subject.Settings.ReadConcern);
            operation.ResultSerializer.ValueType.ShouldBe(typeof(ChangeStreamDocument<BsonDocument>));
            operation.ResumeAfter.ShouldBe(options.ResumeAfter);
            operation.RetryRequested.ShouldBeTrue();
            operation.StartAfter.ShouldBe(options.StartAfter);
            operation.StartAtOperationTime.ShouldBe(options.StartAtOperationTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void Watch_should_execute_a_ChangeStreamOperation_with_default_options_when_options_is_null(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var session = CreateSession(usingSession);
            var stageDocument = BsonDocument.Parse("{ $match : { operationType : \"insert\" } }");
            var pipeline = (PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>)(new[] { stageDocument });
            ChangeStreamOptions options = null;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.WatchAsync(session, pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Watch(session, pipeline, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.WatchAsync(pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Watch(pipeline, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.ShouldBeOfType<ChangeStreamOperation<ChangeStreamDocument<BsonDocument>>>();
            var defaultOptions = new ChangeStreamOptions();
            operation.BatchSize.ShouldBe(defaultOptions.BatchSize);
            operation.Collation.ShouldBe(defaultOptions.Collation);
            operation.CollectionNamespace.ShouldBe(subject.CollectionNamespace);
            operation.FullDocument.ShouldBe(defaultOptions.FullDocument);
            operation.FullDocumentBeforeChange.ShouldBe(defaultOptions.FullDocumentBeforeChange);
            operation.MaxAwaitTime.ShouldBe(defaultOptions.MaxAwaitTime);
            operation.MessageEncoderSettings.ShouldNotBeNull();
            operation.Pipeline.ShouldHaveCount(1);
            operation.Pipeline[0].ShouldBe(stageDocument);
            operation.ReadConcern.ShouldBe(subject.Settings.ReadConcern);
            operation.ResultSerializer.ValueType.ShouldBe(typeof(ChangeStreamDocument<BsonDocument>));
            operation.ResumeAfter.ShouldBe(defaultOptions.ResumeAfter);
            operation.RetryRequested.ShouldBeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void Watch_should_throw_when_pipeline_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline = null;

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.WatchAsync(pipeline).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Watch(pipeline));
            }

            var e = exception.ShouldBeOfType<ArgumentNullException>();
            e.ParamName.ShouldBe("pipeline");
        }

        [Fact]
        public void Watch_should_support_full_document_with_duplicate_elements()
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            database.DropCollection(collection.CollectionNamespace.CollectionName); // ensure a clean collection state (e.g. no indexes)
            collection.InsertOne(new BsonDocument("_id", 1)); // ensure database exists
            database.DropCollection(collection.CollectionNamespace.CollectionName);

            try
            {
                var cursor = collection.Watch();

                ChangeStreamDocument<BsonDocument> changeStreamDocument = null;
                var document = new BsonDocument(allowDuplicateNames: true) { { "_id", 1 }, { "x", 2 }, { "x", 3 } };
                collection.InsertOne(document);
                SpinWait.SpinUntil(() => cursor.MoveNext() && (changeStreamDocument = cursor.Current.FirstOrDefault()) != null, TimeSpan.FromSeconds(5)).ShouldBeTrue();
                var fullDocument = changeStreamDocument.FullDocument;
                fullDocument.ElementCount.ShouldBe(3);
                var firstElement = fullDocument.GetElement(0);
                firstElement.Name.ShouldBe("_id");
                firstElement.Value.ShouldBe(1);
                var secondElement = fullDocument.GetElement(1);
                secondElement.Name.ShouldBe("x");
                secondElement.Value.ShouldBe(2);
                var thirdElement = fullDocument.GetElement(2);
                thirdElement.Name.ShouldBe("x");
                thirdElement.Value.ShouldBe(3);
            }
            finally
            {
                database.DropCollection(collection.CollectionNamespace.CollectionName);
            }
        }

        [Fact]
        public void WithReadPreference_should_return_a_new_collection_with_the_read_preference_changed()
        {
            var subject = CreateSubject<BsonDocument>();
            var newSubject = subject.WithReadPreference(ReadPreference.Nearest);
            newSubject.Settings.ReadPreference.ShouldBe(ReadPreference.Nearest);
        }

        [Fact]
        public void WithWriteConcern_should_return_a_new_collection_with_the_write_concern_changed()
        {
            var subject = CreateSubject<BsonDocument>();
            var newSubject = subject.WithWriteConcern(WriteConcern.WMajority);
            newSubject.Settings.WriteConcern.ShouldBe(WriteConcern.WMajority);
        }

        // private methods
        private IMongoDatabase CreateDatabase(string databaseName = "foo")
        {
            var mockClient = CreateMockClient();
            return mockClient.Object.GetDatabase(databaseName);
        }

        private Mock<IMongoClient> CreateMockClient()
        {
            var mockClient = new Mock<IMongoClient>();
            mockClient.SetupGet(m => m.Settings).Returns(new MongoClientSettings());
            mockClient
                .Setup(m => m.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                .Returns((string databaseName, MongoDatabaseSettings databaseSettings) =>
                {
                    var databaseNamespace = new DatabaseNamespace(databaseName);
                    var settings = new MongoDatabaseSettings();
                    settings.ApplyDefaultValues(mockClient.Object.Settings);
                    var cluster = new Mock<IClusterInternal>().Object;
                    return new MongoDatabase(mockClient.Object, databaseNamespace, settings, cluster, _operationExecutor);
                });
            return mockClient;
        }

        private IClientSessionHandle CreateSession(bool usingSession)
        {
            if (usingSession)
            {
                var client = new Mock<IMongoClient>().Object;
                var cluster = Mock.Of<IClusterInternal>();
                var options = new ClientSessionOptions();
                var coreServerSession = new CoreServerSession();
                var coreSession = new CoreSession(cluster, coreServerSession, options.ToCore());
                var coreSessionHandle = new CoreSessionHandle(coreSession);
                return new ClientSessionHandle(client, options, coreSessionHandle);
            }
            else
            {
                return null;
            }
        }

        private IMongoCollection<TDocument> CreateSubject<TDocument>(IMongoDatabase database = null, string collectionName = "bar", MongoCollectionSettings settings = null)
        {
            database = database ?? CreateDatabase();
            settings = settings ?? new MongoCollectionSettings();
            settings.ReadConcern = _readConcern;
            return database.GetCollection<TDocument>(collectionName, settings);
        }

        private RenderedPipelineDefinition<TOutput> RenderPipeline<TInput, TOutput>(IMongoCollection<TInput> collection, PipelineDefinition<TInput, TOutput> pipeline)
        {
            var inputSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            return pipeline.Render(new(inputSerializer, serializerRegistry));
        }

        private static void VerifyBulkWrite(MockOperationExecutor.WriteCall<BulkWriteOperationResult> call, bool? bypassDocumentValidation, bool isOrdered, BsonDocument let, WriteRequest[] expectedRequests)
        {
            var operation = call.Operation.ShouldBeOfType<BulkMixedWriteOperation>();
            operation.BypassDocumentValidation.ShouldBe(bypassDocumentValidation);
            operation.CollectionNamespace.FullName.ShouldBe("foo.bar");
            operation.IsOrdered.ShouldBe(isOrdered);
            operation.Let.ShouldBe(let);

            var actualRequests = operation.Requests.ToList();
            actualRequests.Count.ShouldBe(expectedRequests.Length);

            for (int i = 0; i < expectedRequests.Length; i++)
            {
                actualRequests[i].ShouldBeEquivalentTo(expectedRequests[i]);
            }
        }

        private static void VerifySessionAndCancellationToken<TDocument>(MockOperationExecutor.ReadCall<TDocument> call, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            call.CancellationToken.ShouldBe(cancellationToken);
            if (session == null)
            {
                call.UsedImplicitSession.ShouldBeTrue();
            }
            else
            {
                call.SessionId.ShouldBe(session.ServerSession.Id);
            }
        }

        private static void VerifySessionAndCancellationToken<TDocument>(MockOperationExecutor.WriteCall<TDocument> call, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            call.CancellationToken.ShouldBe(cancellationToken);
            if (session == null)
            {
                call.UsedImplicitSession.ShouldBeTrue();
            }
            else
            {
                call.SessionId.ShouldBe(session.ServerSession.Id);
            }
        }

        private static void VerifySingleWrite<TRequest>(MockOperationExecutor.WriteCall<BulkWriteOperationResult> call, bool? bypassDocumentValidation, bool isOrdered, BsonDocument let, TRequest expectedRequest)
            where TRequest : WriteRequest
        {
            VerifyBulkWrite(call, bypassDocumentValidation, isOrdered, let, new[] { expectedRequest });
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
