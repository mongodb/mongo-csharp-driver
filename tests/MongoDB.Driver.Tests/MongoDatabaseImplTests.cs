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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests;
using Moq;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoDatabaseImplTests
    {
        private IMongoClient _client;
        private MockOperationExecutor _operationExecutor;
        private MongoDatabaseImpl _subject;

        public MongoDatabaseImplTests()
        {
            _client = CreateMockClient().Object;
            _operationExecutor = new MockOperationExecutor();
            _operationExecutor.Client = _client;
            _subject = CreateSubject();
        }

        [Fact]
        public void Client_should_be_set()
        {
            _subject.Client.Should().NotBeNull();
        }

        [Fact]
        public void DatabaseName_should_be_set()
        {
            _subject.DatabaseNamespace.DatabaseName.Should().Be("foo");
        }

        [Fact]
        public void Settings_should_be_set()
        {
            _subject.Settings.Should().NotBeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_should_execute_an_AggregateOperation_when_out_is_not_specified(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = _subject;
            var session = CreateSession(usingSession);
            var pipeline = new EmptyPipelineDefinition<NoPipelineInput>()
                .AppendStage<NoPipelineInput, NoPipelineInput, BsonDocument>("{ $currentOp : { } }")
                .Limit(1);
            var options = new AggregateOptions()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                Collation = new Collation("en_US"),
                Comment = "test",
                Hint = new BsonDocument("x", 1),
                MaxAwaitTime = TimeSpan.FromSeconds(4),
                MaxTime = TimeSpan.FromSeconds(3),
#pragma warning disable 618
                UseCursor = false
#pragma warning restore 618
            };
            var cancellationToken = new CancellationTokenSource().Token;
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

            var operation = call.Operation.Should().BeOfType<AggregateOperation<BsonDocument>>().Subject;
            operation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            operation.BatchSize.Should().Be(options.BatchSize);
            operation.Collation.Should().BeSameAs(options.Collation);
            operation.CollectionNamespace.Should().BeNull();
            operation.Comment.Should().Be(options.Comment);
            operation.DatabaseNamespace.Should().BeSameAs(subject.DatabaseNamespace);
            operation.Hint.Should().Be(options.Hint);
            operation.MaxAwaitTime.Should().Be(options.MaxAwaitTime);
            operation.MaxTime.Should().Be(options.MaxTime);
            operation.Pipeline.Should().Equal(renderedPipeline.Documents);
            operation.ReadConcern.Should().Be(subject.Settings.ReadConcern);
            operation.RetryRequested.Should().BeTrue();
            operation.ResultSerializer.Should().BeSameAs(renderedPipeline.OutputSerializer);
#pragma warning disable 618
            operation.UseCursor.Should().Be(options.UseCursor);
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
            var subject = CreateSubject(databaseName: "inputDatabaseName").WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var outputDatabase = usingDifferentOutputDatabase ? subject.Client.GetDatabase("outputDatabaseName") : subject;
            var outputCollection = outputDatabase.GetCollection<BsonDocument>("outputCollectionName");
            var pipeline = new EmptyPipelineDefinition<NoPipelineInput>()
                .AppendStage<NoPipelineInput, NoPipelineInput, BsonDocument>("{ $currentOp : { } }")
                .Out(outputCollection);
            var options = new AggregateOptions()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                BypassDocumentValidation = true,
                Collation = new Collation("en_US"),
                Comment = "test",
                Hint = new BsonDocument("x", 1),
                MaxTime = TimeSpan.FromSeconds(3),
#pragma warning disable 618
                UseCursor = false
#pragma warning restore 618
            };
            var cancellationToken1 = new CancellationTokenSource().Token;
            var cancellationToken2 = new CancellationTokenSource().Token;
            var expectedPipeline = new List<BsonDocument>(RenderPipeline(subject, pipeline).Documents); // top level clone
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

            var aggregateOperation = aggregateCall.Operation.Should().BeOfType<AggregateToCollectionOperation>().Subject;
            aggregateOperation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            aggregateOperation.BypassDocumentValidation.Should().Be(options.BypassDocumentValidation);
            aggregateOperation.Collation.Should().BeSameAs(options.Collation);
            aggregateOperation.CollectionNamespace.Should().BeNull();
            aggregateOperation.Comment.Should().Be(options.Comment);
            aggregateOperation.DatabaseNamespace.Should().BeSameAs(subject.DatabaseNamespace);
            aggregateOperation.Hint.Should().Be(options.Hint);
            aggregateOperation.MaxTime.Should().Be(options.MaxTime);
            aggregateOperation.Pipeline.Should().Equal(expectedPipeline);
            aggregateOperation.WriteConcern.Should().BeSameAs(writeConcern);

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

            var findOperation = findCall.Operation.Should().BeOfType<FindOperation<BsonDocument>>().Subject;
            findOperation.AllowDiskUse.Should().NotHaveValue();
            findOperation.AllowPartialResults.Should().NotHaveValue();
            findOperation.BatchSize.Should().Be(options.BatchSize);
            findOperation.Collation.Should().BeSameAs(options.Collation);
            findOperation.CollectionNamespace.FullName.Should().Be(outputCollection.CollectionNamespace.FullName);
            findOperation.Comment.Should().BeNull();
            findOperation.CursorType.Should().Be(Core.Operations.CursorType.NonTailable);
            findOperation.Filter.Should().BeNull();
            findOperation.Limit.Should().Be(null);
            findOperation.MaxTime.Should().Be(options.MaxTime);
#pragma warning disable 618
            findOperation.Modifiers.Should().BeNull();
#pragma warning restore 618
            findOperation.NoCursorTimeout.Should().NotHaveValue();
#pragma warning disable 618
            findOperation.OplogReplay.Should().NotHaveValue();
#pragma warning restore 618
            findOperation.Projection.Should().BeNull();
            findOperation.RetryRequested.Should().BeTrue();
            findOperation.Skip.Should().Be(null);
            findOperation.Sort.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void AggregateToCollection_should_execute_an_AggregateToCollectionOperation(
            [Values(false, true)] bool usingSession,
            [Values("$out", "$merge")] string lastStageName,
            [Values(false, true)] bool usingDifferentOutputDatabase,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = CreateSubject(databaseName: "inputDatabaseName").WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var outputDatabase = usingDifferentOutputDatabase ? subject.Client.GetDatabase("outputDatabaseName") : subject;
            var outputCollection = outputDatabase.GetCollection<BsonDocument>("outputCollectionName");
            var pipeline = new EmptyPipelineDefinition<NoPipelineInput>()
                .AppendStage<NoPipelineInput, NoPipelineInput, BsonDocument>("{ $currentOp : { } }");
            switch (lastStageName)
            {
                case "$out": pipeline = pipeline.Out(outputCollection); break;
                case "$merge": pipeline = pipeline.Merge(outputCollection, new MergeStageOptions<BsonDocument>()); break;
                default: throw new Exception($"Unexpected lastStageName: {lastStageName}.");
            }
            var options = new AggregateOptions()
            {
                AllowDiskUse = true,
                BatchSize = 10,
                BypassDocumentValidation = true,
                Collation = new Collation("en_US"),
                Comment = "test",
                Hint = new BsonDocument("x", 1),
                MaxTime = TimeSpan.FromSeconds(3),
#pragma warning disable 618
                UseCursor = false
#pragma warning restore 618
            };
            var cancellationToken = new CancellationTokenSource().Token;
            var expectedPipeline = new List<BsonDocument>(RenderPipeline(subject, pipeline).Documents);
            if (!usingDifferentOutputDatabase && lastStageName == "$out")
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

            var aggregateOperation = aggregateCall.Operation.Should().BeOfType<AggregateToCollectionOperation>().Subject;
            aggregateOperation.AllowDiskUse.Should().Be(options.AllowDiskUse);
            aggregateOperation.BypassDocumentValidation.Should().Be(options.BypassDocumentValidation);
            aggregateOperation.Collation.Should().BeSameAs(options.Collation);
            aggregateOperation.CollectionNamespace.Should().BeNull();
            aggregateOperation.Comment.Should().Be(options.Comment);
            aggregateOperation.DatabaseNamespace.Should().BeSameAs(subject.DatabaseNamespace);
            aggregateOperation.Hint.Should().Be(options.Hint);
            aggregateOperation.MaxTime.Should().Be(options.MaxTime);
            aggregateOperation.Pipeline.Should().Equal(expectedPipeline);
            aggregateOperation.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void AggregateToCollection_should_throw_when_last_stage_is_not_an_output_stage(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var session = CreateSession(usingSession);
            var pipeline = new EmptyPipelineDefinition<NoPipelineInput>()
                .AppendStage<NoPipelineInput, NoPipelineInput, BsonDocument>("{ $currentOp : { } }");
            var options = new AggregateOptions();
            var cancellationToken = new CancellationTokenSource().Token;

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

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCollection_should_execute_a_CreateCollectionOperation_when_options_is_generic(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var name = "bar";
            var storageEngine = new BsonDocument("awesome", true);
            var validatorDocument = BsonDocument.Parse("{ x : 1 }");
            var validatorDefinition = (FilterDefinition<BsonDocument>)validatorDocument;
#pragma warning disable 618
            var options = new CreateCollectionOptions<BsonDocument>
            {
                AutoIndexId = false,
                Capped = true,
                Collation = new Collation("en_US"),
                IndexOptionDefaults = new IndexOptionDefaults { StorageEngine = new BsonDocument("x", 1) },
                MaxDocuments = 10,
                MaxSize = 11,
                NoPadding = true,
                StorageEngine = storageEngine,
                UsePowerOf2Sizes = true,
                ValidationAction = DocumentValidationAction.Warn,
                ValidationLevel = DocumentValidationLevel.Off,
                Validator = validatorDefinition
            };
#pragma warning restore
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.CreateCollectionAsync(session, name, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CreateCollection(session, name, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.CreateCollectionAsync(name, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CreateCollection(name, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var op = call.Operation.Should().BeOfType<CreateCollectionOperation>().Subject;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(_subject.DatabaseNamespace, name));
#pragma warning disable 618
            op.AutoIndexId.Should().Be(options.AutoIndexId);
#pragma warning restore
            op.Capped.Should().Be(options.Capped);
            op.Collation.Should().BeSameAs(options.Collation);
            op.IndexOptionDefaults.ToBsonDocument().Should().Be(options.IndexOptionDefaults.ToBsonDocument());
            op.MaxDocuments.Should().Be(options.MaxDocuments);
            op.MaxSize.Should().Be(options.MaxSize);
            op.NoPadding.Should().Be(options.NoPadding);
            op.StorageEngine.Should().Be(storageEngine);
            op.UsePowerOf2Sizes.Should().Be(options.UsePowerOf2Sizes);
            op.ValidationAction.Should().Be(options.ValidationAction);
            op.ValidationLevel.Should().Be(options.ValidationLevel);
            op.Validator.Should().Be(validatorDocument);
            op.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCollection_should_execute_a_CreateCollectionOperation_when_options_is_not_generic(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var name = "bar";
            var storageEngine = new BsonDocument("awesome", true);
#pragma warning disable 618
            var options = new CreateCollectionOptions
            {
                AutoIndexId = false,
                Capped = true,
                Collation = new Collation("en_US"),
                IndexOptionDefaults = new IndexOptionDefaults { StorageEngine = new BsonDocument("x", 1) },
                MaxDocuments = 10,
                MaxSize = 11,
                NoPadding = true,
                StorageEngine = storageEngine,
                UsePowerOf2Sizes = true,
                ValidationAction = DocumentValidationAction.Warn,
                ValidationLevel = DocumentValidationLevel.Off
            };
#pragma warning restore
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.CreateCollectionAsync(session, name, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CreateCollection(session, name, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.CreateCollectionAsync(name, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CreateCollection(name, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var op = call.Operation.Should().BeOfType<CreateCollectionOperation>().Subject;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(_subject.DatabaseNamespace, name));
#pragma warning disable 618
            op.AutoIndexId.Should().Be(options.AutoIndexId);
#pragma warning restore
            op.Capped.Should().Be(options.Capped);
            op.Collation.Should().BeSameAs(options.Collation);
            op.IndexOptionDefaults.ToBsonDocument().Should().Be(options.IndexOptionDefaults.ToBsonDocument());
            op.MaxDocuments.Should().Be(options.MaxDocuments);
            op.MaxSize.Should().Be(options.MaxSize);
            op.NoPadding.Should().Be(options.NoPadding);
            op.StorageEngine.Should().Be(storageEngine);
            op.UsePowerOf2Sizes.Should().Be(options.UsePowerOf2Sizes);
            op.ValidationAction.Should().Be(options.ValidationAction);
            op.ValidationLevel.Should().Be(options.ValidationLevel);
            op.Validator.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCollection_should_execute_a_CreateCollectionOperation_when_options_is_null(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var name = "bar";
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.CreateCollectionAsync(session, name, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CreateCollection(session, name, null, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.CreateCollectionAsync(name, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CreateCollection(name, null, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var op = call.Operation.Should().BeOfType<CreateCollectionOperation>().Subject;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(_subject.DatabaseNamespace, name));
#pragma warning disable 618
            op.AutoIndexId.Should().NotHaveValue();
#pragma warning restore
            op.Capped.Should().NotHaveValue();
            op.IndexOptionDefaults.Should().BeNull();
            op.MaxDocuments.Should().NotHaveValue();
            op.MaxSize.Should().NotHaveValue();
            op.NoPadding.Should().NotHaveValue();
            op.StorageEngine.Should().BeNull();
            op.UsePowerOf2Sizes.Should().NotHaveValue();
            op.ValidationAction.Should().BeNull();
            op.ValidationLevel.Should().BeNull();
            op.Validator.Should().BeNull();
            op.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_execute_a_CreateViewOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var viewName = "view";
            var viewOn = "on";
            var pipelineDocuments = new[] { BsonDocument.Parse("{ a : 1 }") };
            var pipelineDefinition = (PipelineDefinition<BsonDocument, BsonDocument>)pipelineDocuments;
            var collation = new Collation("en-us");
            var options = new CreateViewOptions<BsonDocument>
            {
                Collation = collation,
                DocumentSerializer = BsonDocumentSerializer.Instance,
                SerializerRegistry = BsonSerializer.SerializerRegistry
            };
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.CreateViewAsync(session, viewName, viewOn, pipelineDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CreateView(session, viewName, viewOn, pipelineDefinition, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.CreateViewAsync(viewName, viewOn, pipelineDefinition, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.CreateView(viewName, viewOn, pipelineDefinition, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var operation = call.Operation.Should().BeOfType<CreateViewOperation>().Subject;
            operation.Collation.Should().Be(collation);
            operation.DatabaseNamespace.Should().Be(subject.DatabaseNamespace);
            operation.Pipeline.Should().Equal(pipelineDocuments);
            operation.ViewName.Should().Be(viewName);
            operation.ViewOn.Should().Be(viewOn);
            operation.WriteConcern.Should().Be(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_throw_when_viewName_is_null(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();

            var exception = Record.Exception(() =>
            {
                if (usingSession)
                {
                    if (async)
                    {
                        _subject.CreateViewAsync(session, null, "viewOn", pipeline).GetAwaiter().GetResult();
                    }
                    else
                    {
                        _subject.CreateView(session, null, "viewOn", pipeline);
                    }
                }
                else
                {
                    if (async)
                    {
                        _subject.CreateViewAsync(null, "viewOn", pipeline).GetAwaiter().GetResult();
                    }
                    else
                    {
                        _subject.CreateView(null, "viewOn", pipeline);
                    }
                }
            });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("viewName");
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_throw_when_viewOn_is_null(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();

            var exception = Record.Exception(() =>
            {
                if (usingSession)
                {
                    if (async)
                    {
                        _subject.CreateViewAsync(session, "viewName", null, pipeline).GetAwaiter().GetResult();
                    }
                    else
                    {
                        _subject.CreateView(session, "viewName", null, pipeline);
                    }
                }
                else
                {
                    if (async)
                    {
                        _subject.CreateViewAsync("viewName", null, pipeline).GetAwaiter().GetResult();
                    }
                    else
                    {
                        _subject.CreateView("viewName", null, pipeline);
                    }
                }
            });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("viewOn");
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_throw_when_pipeline_is_null(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);

            var exception = Record.Exception(() =>
            {
                if (usingSession)
                {
                    if (async)
                    {
                        _subject.CreateViewAsync<BsonDocument, BsonDocument>(session, "viewName", "viewOn", null).GetAwaiter().GetResult();
                    }
                    else
                    {
                        _subject.CreateView<BsonDocument, BsonDocument>(session, "viewName", "viewOn", null);
                    }
                }
                else
                {
                    if (async)
                    {
                        _subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", "viewOn", null).GetAwaiter().GetResult();
                    }
                    else
                    {
                        _subject.CreateView<BsonDocument, BsonDocument>("viewName", "viewOn", null);
                    }
                }
            });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Theory]
        [ParameterAttributeData]
        public void DropCollection_should_execute_a_DropCollectionOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var name = "bar";
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.DropCollectionAsync(session, name, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DropCollection(session, name, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.DropCollectionAsync(name, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DropCollection(name, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var op = call.Operation.Should().BeOfType<DropCollectionOperation>().Subject;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(subject.DatabaseNamespace, name));
            op.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void ListCollectionNames_should_execute_a_ListCollectionsOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingOptions,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ name : \"awesome\" }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            ListCollectionNamesOptions options = null;
            if (usingOptions)
            {
                options = new ListCollectionNamesOptions
                {
                    Filter = filterDefinition
                };
            }
            var cancellationToken = new CancellationTokenSource().Token;

            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult<IAsyncCursor<BsonDocument>>(mockCursor.Object);

            if (usingSession)
            {
                if (async)
                {
                    _subject.ListCollectionNamesAsync(session, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.ListCollectionNames(session, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    _subject.ListCollectionNamesAsync(options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.ListCollectionNames(options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var op = call.Operation.Should().BeOfType<ListCollectionsOperation>().Subject;
            op.DatabaseNamespace.Should().Be(_subject.DatabaseNamespace);
            op.NameOnly.Should().BeTrue();
            if (usingOptions)
            {
                op.Filter.Should().Be(filterDocument);
            }
            else
            {
                op.Filter.Should().BeNull();
            }
            op.RetryRequested.Should().BeTrue();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void ListCollectionNames_should_return_expected_result(
            [Values(0, 1, 2, 10)] int numberOfCollections,
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            if (usingSession)
            {
                RequireServer.Check().VersionGreaterThanOrEqualTo("3.6.0");
            }

            var collectionNames = Enumerable.Range(1, numberOfCollections).Select(n => $"c{n}").ToArray();

            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("ListCollectionNames-test");
            client.DropDatabase(database.DatabaseNamespace.DatabaseName);
            foreach (var collectionName in collectionNames)
            {
                database.CreateCollection(collectionName);
            }

            using (var session = usingSession ? client.StartSession() : null)
            {
                IAsyncCursor<string> cursor;
                if (usingSession)
                {
                    if (async)
                    {
                        cursor = database.ListCollectionNamesAsync(session).GetAwaiter().GetResult();
                    }
                    else
                    {
                        cursor = database.ListCollectionNames(session);
                    }
                }
                else
                {
                    if (async)
                    {
                        cursor = database.ListCollectionNamesAsync().GetAwaiter().GetResult();
                    }
                    else
                    {
                        cursor = database.ListCollectionNames();
                    }
                }

                var actualCollectionNames = cursor.ToList();
                actualCollectionNames.Where(n => n != "system.indexes").Should().BeEquivalentTo(collectionNames);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ListCollections_should_execute_a_ListCollectionsOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingOptions,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ name : \"awesome\" }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            ListCollectionsOptions options = null;
            if (usingOptions)
            {
                options = new ListCollectionsOptions
                {
                    Filter = filterDefinition
                };
            }
            var cancellationToken = new CancellationTokenSource().Token;

            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult<IAsyncCursor<BsonDocument>>(mockCursor.Object);

            if (usingSession)
            {
                if (async)
                {
                    _subject.ListCollectionsAsync(session, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.ListCollections(session, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    _subject.ListCollectionsAsync(options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.ListCollections(options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var op = call.Operation.Should().BeOfType<ListCollectionsOperation>().Subject;
            op.DatabaseNamespace.Should().Be(_subject.DatabaseNamespace);
            op.NameOnly.Should().NotHaveValue();
            if (usingOptions)
            {
                op.Filter.Should().Be(filterDocument);
            }
            else
            {
                op.Filter.Should().BeNull();
            }
            op.RetryRequested.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void RenameCollection_should_execute_a_RenameCollectionOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var oldName = "bar";
            var newName = "baz";
            var options = new RenameCollectionOptions
            {
                DropTarget = true,
            };
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.RenameCollectionAsync(session, oldName, newName, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.RenameCollection(session, oldName, newName, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.RenameCollectionAsync(oldName, newName, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.RenameCollection(oldName, newName, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var op = call.Operation.Should().BeOfType<RenameCollectionOperation>().Subject;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(_subject.DatabaseNamespace, oldName));
            op.NewCollectionNamespace.Should().Be(new CollectionNamespace(_subject.DatabaseNamespace, newName));
            op.DropTarget.Should().Be(options.DropTarget);
            op.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_default_to_ReadPreference_primary(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var commandDocument = BsonDocument.Parse("{ count : \"foo\" }");
            var command = (Command<BsonDocument>)commandDocument;
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    _subject.RunCommandAsync(session, command, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(session, command, null, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    _subject.RunCommandAsync(command, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(command, null, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var binding = call.Binding.Should().BeOfType<ReadBindingHandle>().Subject;
            binding.ReadPreference.Should().Be(ReadPreference.Primary);

            var op = call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>().Subject;
            op.DatabaseNamespace.Should().Be(_subject.DatabaseNamespace);
            op.Command.Should().Be(commandDocument);
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_use_the_provided_ReadPreference(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var commandDocument = new BsonDocument("count", "foo");
            var command = (Command<BsonDocument>)commandDocument;
            var readPreference = ReadPreference.Secondary;
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    _subject.RunCommandAsync(session, command, readPreference, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(session, command, readPreference, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    _subject.RunCommandAsync(command, readPreference, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(command, readPreference, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var binding = call.Binding.Should().BeOfType<ReadBindingHandle>().Subject;
            binding.ReadPreference.Should().Be(readPreference);

            var op = call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>().Subject;
            op.DatabaseNamespace.Should().Be(_subject.DatabaseNamespace);
            op.Command.Should().Be(commandDocument);
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_run_a_non_read_command(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var commandDocument = new BsonDocument("shutdown", 1);
            var command = (Command<BsonDocument>)commandDocument;
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    _subject.RunCommandAsync(session, command, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(session, command, null, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    _subject.RunCommandAsync(command, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(command, null, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var binding = call.Binding.Should().BeOfType<ReadBindingHandle>().Subject;
            binding.ReadPreference.Should().Be(ReadPreference.Primary);

            var op = call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>().Subject;
            op.DatabaseNamespace.Should().Be(_subject.DatabaseNamespace);
            op.Command.Should().Be(commandDocument);
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_run_a_json_command(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var commandJson = "{ count : \"foo\" }";
            var commandDocument = BsonDocument.Parse(commandJson);
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    _subject.RunCommandAsync<BsonDocument>(session, commandJson, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand<BsonDocument>(session, commandJson, null, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    _subject.RunCommandAsync<BsonDocument>(commandJson, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand<BsonDocument>(commandJson, null, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var binding = call.Binding.Should().BeOfType<ReadBindingHandle>().Subject;
            binding.ReadPreference.Should().Be(ReadPreference.Primary);

            var op = call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>().Subject;
            op.DatabaseNamespace.Should().Be(_subject.DatabaseNamespace);
            op.Command.Should().Be(commandDocument);
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_run_a_serialized_command(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var commandObject = new CountCommand { Collection = "foo" };
            var command = new ObjectCommand<BsonDocument>(commandObject);
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    _subject.RunCommandAsync(session, command, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(session, command, null, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    _subject.RunCommandAsync(command, null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(command, null, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();
            VerifySessionAndCancellationToken(call, session, cancellationToken);

            var binding = call.Binding.Should().BeOfType<ReadBindingHandle>().Subject;
            binding.ReadPreference.Should().Be(ReadPreference.Primary);

            var op = call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>().Subject;
            op.DatabaseNamespace.Should().Be(_subject.DatabaseNamespace);
            op.Command.Should().Be("{ count : \"foo\" }");
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_set_RetryRequested_to_false(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var commandDocument = new BsonDocument("count", "foo");
            var command = (Command<BsonDocument>)commandDocument;
            var readPreference = ReadPreference.Secondary;
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    _subject.RunCommandAsync(session, command, readPreference, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(session, command, readPreference, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    _subject.RunCommandAsync(command, readPreference, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.RunCommand(command, readPreference, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            var op = call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>().Subject;
            op.RetryRequested.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void Watch_should_invoke_the_correct_operation(
           [Values(false, true)] bool usingSession,
           [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().Limit(1);
            var options = new ChangeStreamOptions
            {
                BatchSize = 123,
                Collation = new Collation("en-us"),
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(123),
                ResumeAfter = new BsonDocument(),
                StartAfter = new BsonDocument(),
                StartAtOperationTime = new BsonTimestamp(1, 2)
            };
            var cancellationToken = new CancellationTokenSource().Token;
            var renderedPipeline = new[] { BsonDocument.Parse("{ $limit : 1 }") };

            if (usingSession)
            {
                if (async)
                {
                    _subject.WatchAsync(session, pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.Watch(session, pipeline, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    _subject.WatchAsync(pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.Watch(pipeline, options, cancellationToken);
                }
            }

            var call = _operationExecutor.GetReadCall<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
            if (usingSession)
            {
                call.SessionId.Should().BeSameAs(session.ServerSession.Id);
            }
            else
            {
                call.UsedImplicitSession.Should().BeTrue();
            }
            call.CancellationToken.Should().Be(cancellationToken);

            var changeStreamOperation = call.Operation.Should().BeOfType<ChangeStreamOperation<ChangeStreamDocument<BsonDocument>>>().Subject;
            changeStreamOperation.BatchSize.Should().Be(options.BatchSize);
            changeStreamOperation.Collation.Should().BeSameAs(options.Collation);
            changeStreamOperation.CollectionNamespace.Should().BeNull();
            changeStreamOperation.DatabaseNamespace.Should().Be(_subject.DatabaseNamespace);
            changeStreamOperation.FullDocument.Should().Be(options.FullDocument);
            changeStreamOperation.MaxAwaitTime.Should().Be(options.MaxAwaitTime);
            changeStreamOperation.MessageEncoderSettings.Should().NotBeNull();
            changeStreamOperation.Pipeline.Should().Equal(renderedPipeline);
            changeStreamOperation.ReadConcern.Should().Be(_subject.Settings.ReadConcern);
            changeStreamOperation.ResultSerializer.Should().BeOfType<ChangeStreamDocumentSerializer<BsonDocument>>();
            changeStreamOperation.ResumeAfter.Should().Be(options.ResumeAfter);
            changeStreamOperation.RetryRequested.Should().BeTrue();
            changeStreamOperation.StartAfter.Should().Be(options.StartAfter);
            changeStreamOperation.StartAtOperationTime.Should().Be(options.StartAtOperationTime);
        }

        [Fact]
        public void WithReadConcern_should_return_expected_result()
        {
            var originalReadConcern = new ReadConcern(ReadConcernLevel.Linearizable);
            var subject = CreateSubject().WithReadConcern(originalReadConcern);
            var newReadConcern = new ReadConcern(ReadConcernLevel.Majority);

            var result = subject.WithReadConcern(newReadConcern);

            subject.Settings.ReadConcern.Should().BeSameAs(originalReadConcern);
            result.Settings.ReadConcern.Should().BeSameAs(newReadConcern);
            result.WithReadConcern(originalReadConcern).Settings.Should().Be(subject.Settings);
        }

        [Fact]
        public void WithReadPreference_should_return_expected_result()
        {
            var originalReadPreference = new ReadPreference(ReadPreferenceMode.Secondary);
            var subject = CreateSubject().WithReadPreference(originalReadPreference);
            var newReadPreference = new ReadPreference(ReadPreferenceMode.SecondaryPreferred);

            var result = subject.WithReadPreference(newReadPreference);

            subject.Settings.ReadPreference.Should().BeSameAs(originalReadPreference);
            result.Settings.ReadPreference.Should().BeSameAs(newReadPreference);
            result.WithReadPreference(originalReadPreference).Settings.Should().Be(subject.Settings);
        }

        [Fact]
        public void WithWriteConcern_should_return_expected_result()
        {
            var originalWriteConcern = new WriteConcern(2);
            var subject = CreateSubject().WithWriteConcern(originalWriteConcern);
            var newWriteConcern = new WriteConcern(3);

            var result = subject.WithWriteConcern(newWriteConcern);

            subject.Settings.WriteConcern.Should().BeSameAs(originalWriteConcern);
            result.Settings.WriteConcern.Should().BeSameAs(newWriteConcern);
            result.WithWriteConcern(originalWriteConcern).Settings.Should().Be(subject.Settings);
        }

        // private methods
        private Mock<IMongoClient> CreateMockClient()
        {
            var mockCluster = new Mock<ICluster>();
            var clientSettings = new MongoClientSettings();

            var mockClient = new Mock<IMongoClient>();
            mockClient.SetupGet(m => m.Cluster).Returns(mockCluster.Object);
            mockClient.SetupGet(m => m.Settings).Returns(clientSettings);
            mockClient
                .Setup(m => m.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                .Returns((string databaseName, MongoDatabaseSettings settings) =>
                {
                    var databaseNamespace = new DatabaseNamespace(databaseName);
                    settings = settings ?? new MongoDatabaseSettings();
                    settings.ApplyDefaultValues(mockClient.Object.Settings);
                    var cluster = new Mock<ICluster>().Object;
                    return new MongoDatabaseImpl(mockClient.Object, databaseNamespace, settings, cluster, _operationExecutor);
                });

            return mockClient;
        }

        private IClientSessionHandle CreateSession(bool usingSession)
        {
            if (usingSession)
            {
                var cluster = Mock.Of<ICluster>();
                var options = new ClientSessionOptions();
                var coreServerSession = new CoreServerSession();
                var coreSession = new CoreSession(cluster, coreServerSession, options.ToCore());
                var coreSessionHandle = new CoreSessionHandle(coreSession);
                return new ClientSessionHandle(_client, options, coreSessionHandle);
            }
            else
            {
                return null;
            }
        }

        private MongoDatabaseImpl CreateSubject(string databaseName = "foo", IOperationExecutor operationExecutor = null)
        {
            var settings = new MongoDatabaseSettings();
            settings.ApplyDefaultValues(new MongoClientSettings());
            return new MongoDatabaseImpl(
                _client,
                new DatabaseNamespace(databaseName),
                settings,
                new Mock<ICluster>().Object,
                operationExecutor ?? _operationExecutor);
        }

        private RenderedPipelineDefinition<TOutput> RenderPipeline<TOutput>(IMongoDatabase dataabse, PipelineDefinition<NoPipelineInput, TOutput> pipeline)
        {
            var inputSerializer = NoPipelineInputSerializer.Instance;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            return pipeline.Render(inputSerializer, serializerRegistry);
        }

        private static void VerifySessionAndCancellationToken<TDocument>(MockOperationExecutor.ReadCall<TDocument> call, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            call.CancellationToken.Should().Be(cancellationToken);
            if (session == null)
            {
                call.UsedImplicitSession.Should().BeTrue();
            }
            else
            {
                call.SessionId.Should().Be(session.ServerSession.Id);
            }
        }

        private static void VerifySessionAndCancellationToken<TDocument>(MockOperationExecutor.WriteCall<TDocument> call, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            call.CancellationToken.Should().Be(cancellationToken);
            if (session == null)
            {
                call.UsedImplicitSession.Should().BeTrue();
            }
            else
            {
                call.SessionId.Should().Be(session.ServerSession.Id);
            }
        }

        private class CountCommand
        {
            [BsonElement("count")]
            public string Collection;
        }
    }
}
