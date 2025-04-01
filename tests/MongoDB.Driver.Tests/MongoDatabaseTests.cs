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
using Shouldly;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Bson.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoDatabaseImplTests
    {
        private IMongoClient _client;
        private MockOperationExecutor _operationExecutor;
        private MongoDatabase _subject;

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
            _subject.Client.ShouldNotBeNull();
        }

        [Fact]
        public void DatabaseName_should_be_set()
        {
            _subject.DatabaseNamespace.DatabaseName.ShouldBe("foo");
        }

        [Fact]
        public void Settings_should_be_set()
        {
            _subject.Settings.ShouldNotBeNull();
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
            operation.CollectionNamespace.ShouldBeNull();
            operation.Comment.ShouldBe(options.Comment);
            operation.DatabaseNamespace.ShouldBeSameAs(subject.DatabaseNamespace);
            operation.Hint.ShouldBe(options.Hint);
            operation.Let.ShouldBe(options.Let);
            operation.MaxAwaitTime.ShouldBe(options.MaxAwaitTime);
            operation.MaxTime.ShouldBe(options.MaxTime);
            operation.Pipeline.ShouldBe(renderedPipeline.Documents);
            operation.ReadConcern.ShouldBe(subject.Settings.ReadConcern);
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
                Let = new BsonDocument("y", "z"),
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

            var aggregateOperation = aggregateCall.Operation.ShouldBeOfType<AggregateToCollectionOperation>();
            aggregateOperation.AllowDiskUse.ShouldBe(options.AllowDiskUse);
            aggregateOperation.BypassDocumentValidation.ShouldBe(options.BypassDocumentValidation);
            aggregateOperation.Collation.ShouldBeSameAs(options.Collation);
            aggregateOperation.CollectionNamespace.ShouldBeNull();
            aggregateOperation.Comment.ShouldBe(options.Comment);
            aggregateOperation.DatabaseNamespace.ShouldBeSameAs(subject.DatabaseNamespace);
            aggregateOperation.Hint.ShouldBe(options.Hint);
            aggregateOperation.Let.ShouldBe(options.Let);
            aggregateOperation.MaxTime.ShouldBe(options.MaxTime);
            aggregateOperation.Pipeline.ShouldBe(expectedPipeline);
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
                Let = new BsonDocument("y", "z"),
                MaxTime = TimeSpan.FromSeconds(3),
#pragma warning disable 618
                UseCursor = false
#pragma warning restore 618
            };
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
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

            var aggregateOperation = aggregateCall.Operation.ShouldBeOfType<AggregateToCollectionOperation>();
            aggregateOperation.AllowDiskUse.ShouldBe(options.AllowDiskUse);
            aggregateOperation.BypassDocumentValidation.ShouldBe(options.BypassDocumentValidation);
            aggregateOperation.Collation.ShouldBeSameAs(options.Collation);
            aggregateOperation.CollectionNamespace.ShouldBeNull();
            aggregateOperation.Comment.ShouldBe(options.Comment);
            aggregateOperation.DatabaseNamespace.ShouldBeSameAs(subject.DatabaseNamespace);
            aggregateOperation.Hint.ShouldBe(options.Hint);
            aggregateOperation.Let.ShouldBe(options.Let);
            aggregateOperation.MaxTime.ShouldBe(options.MaxTime);
            aggregateOperation.Pipeline.ShouldBe(expectedPipeline);
            aggregateOperation.WriteConcern.ShouldBeSameAs(writeConcern);
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
        public void CreateCollection_should_execute_a_CreateCollectionOperation_when_options_is_generic(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool clustered,
            [Values(false, true)] bool async)
        {
            if (clustered)
            {
                RequireServer.Check().Supports(Feature.ClusteredIndexes);
            }

            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);
            var session = CreateSession(usingSession);
            var name = "bar";
            var storageEngine = new BsonDocument("awesome", true);
            var validatorDocument = BsonDocument.Parse("{ x : 1 }");
            var validatorDefinition = (FilterDefinition<BsonDocument>)validatorDocument;
            var changeStreamPreAndPostImagesOptions = new ChangeStreamPreAndPostImagesOptions { Enabled = true };

            var options = new CreateCollectionOptions<BsonDocument>
            {
                Capped = true,
                ChangeStreamPreAndPostImagesOptions = changeStreamPreAndPostImagesOptions,
                ClusteredIndex = clustered ? new ClusteredIndexOptions<BsonDocument>() : null,
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var op = call.Operation.ShouldBeOfType<CreateCollectionOperation>();
            op.CollectionNamespace.ShouldBe(new CollectionNamespace(_subject.DatabaseNamespace, name));
            op.Capped.ShouldBe(options.Capped);
            op.ChangeStreamPreAndPostImages.ShouldBe(options.ChangeStreamPreAndPostImagesOptions.BackingDocument);
            if (clustered)
            {
                op.ClusteredIndex.ShouldNotBeNull();
            }
            else
            {
                op.ClusteredIndex.ShouldBeNull();
            }
            op.Collation.ShouldBeSameAs(options.Collation);
            op.IndexOptionDefaults.ToBsonDocument().ShouldBe(options.IndexOptionDefaults.ToBsonDocument());
            op.MaxDocuments.ShouldBe(options.MaxDocuments);
            op.MaxSize.ShouldBe(options.MaxSize);
            op.NoPadding.ShouldBe(options.NoPadding);
            op.StorageEngine.ShouldBe(storageEngine);
            op.UsePowerOf2Sizes.ShouldBe(options.UsePowerOf2Sizes);
            op.ValidationAction.ShouldBe(options.ValidationAction);
            op.ValidationLevel.ShouldBe(options.ValidationLevel);
            op.Validator.ShouldBe(validatorDocument);
            op.WriteConcern.ShouldBeSameAs(writeConcern);
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
            var options = new CreateCollectionOptions
            {
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var op = call.Operation.ShouldBeOfType<CreateCollectionOperation>();
            op.CollectionNamespace.ShouldBe(new CollectionNamespace(_subject.DatabaseNamespace, name));
            op.Capped.ShouldBe(options.Capped);
            op.ChangeStreamPreAndPostImages.ShouldBeNull();
            op.ClusteredIndex.ShouldBeNull();
            op.Collation.ShouldBeSameAs(options.Collation);
            op.IndexOptionDefaults.ToBsonDocument().ShouldBe(options.IndexOptionDefaults.ToBsonDocument());
            op.MaxDocuments.ShouldBe(options.MaxDocuments);
            op.MaxSize.ShouldBe(options.MaxSize);
            op.NoPadding.ShouldBe(options.NoPadding);
            op.StorageEngine.ShouldBe(storageEngine);
            op.UsePowerOf2Sizes.ShouldBe(options.UsePowerOf2Sizes);
            op.ValidationAction.ShouldBe(options.ValidationAction);
            op.ValidationLevel.ShouldBe(options.ValidationLevel);
            op.Validator.ShouldBeNull();
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var op = call.Operation.ShouldBeOfType<CreateCollectionOperation>();
            op.CollectionNamespace.ShouldBe(new CollectionNamespace(_subject.DatabaseNamespace, name));
            op.Capped.ShouldNotHaveValue();
            op.ClusteredIndex.ShouldBeNull();
            op.IndexOptionDefaults.ShouldBeNull();
            op.MaxDocuments.ShouldNotHaveValue();
            op.MaxSize.ShouldNotHaveValue();
            op.NoPadding.ShouldNotHaveValue();
            op.StorageEngine.ShouldBeNull();
            op.UsePowerOf2Sizes.ShouldNotHaveValue();
            op.ValidationAction.ShouldBeNull();
            op.ValidationLevel.ShouldBeNull();
            op.Validator.ShouldBeNull();
            op.WriteConcern.ShouldBeSameAs(writeConcern);
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var operation = call.Operation.ShouldBeOfType<CreateViewOperation>();
            operation.Collation.ShouldBe(collation);
            operation.DatabaseNamespace.ShouldBe(subject.DatabaseNamespace);
            operation.Pipeline.ShouldBe(pipelineDocuments);
            operation.ViewName.ShouldBe(viewName);
            operation.ViewOn.ShouldBe(viewOn);
            operation.WriteConcern.ShouldBe(writeConcern);
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

            var argumentNullException = exception.ShouldBeOfType<ArgumentNullException>();
            argumentNullException.ParamName.ShouldBe("viewName");
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

            var argumentNullException = exception.ShouldBeOfType<ArgumentNullException>();
            argumentNullException.ParamName.ShouldBe("viewOn");
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

            var argumentNullException = exception.ShouldBeOfType<ArgumentNullException>();
            argumentNullException.ParamName.ShouldBe("pipeline");
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var op = call.Operation.ShouldBeOfType<DropCollectionOperation>();
            op.CollectionNamespace.ShouldBe(new CollectionNamespace(subject.DatabaseNamespace, name));
            op.WriteConcern.ShouldBeSameAs(writeConcern);
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
                    AuthorizedCollections = true,
                    Filter = filterDefinition
                };
            }
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var op = call.Operation.ShouldBeOfType<ListCollectionsOperation>();
            op.DatabaseNamespace.ShouldBe(_subject.DatabaseNamespace);
            op.NameOnly.ShouldBeTrue();
            if (usingOptions)
            {
                op.Filter.ShouldBe(filterDocument);
                op.AuthorizedCollections.ShouldBeTrue();
            }
            else
            {
                op.Filter.ShouldBeNull();
                op.AuthorizedCollections.ShouldNotHaveValue();
            }
            op.RetryRequested.ShouldBeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void ListCollectionNames_should_return_expected_result(
            [Values(0, 1, 2, 10)] int numberOfCollections,
            [Values(null, false, true)] bool? usingAuthorizedCollections,
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            if (usingAuthorizedCollections.HasValue)
            {
                RequireServer.Check().VersionGreaterThanOrEqualTo("4.0.0");
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
                var listCollectionNamesOptions = new ListCollectionNamesOptions();
                if (usingAuthorizedCollections.HasValue)
                {
                    listCollectionNamesOptions.AuthorizedCollections = usingAuthorizedCollections.Value;
                }

                if (usingSession)
                {
                    if (async)
                    {
                        cursor = database.ListCollectionNamesAsync(session, listCollectionNamesOptions).GetAwaiter().GetResult();
                    }
                    else
                    {
                        cursor = database.ListCollectionNames(session, listCollectionNamesOptions);
                    }
                }
                else
                {
                    if (async)
                    {
                        cursor = database.ListCollectionNamesAsync(listCollectionNamesOptions).GetAwaiter().GetResult();
                    }
                    else
                    {
                        cursor = database.ListCollectionNames(listCollectionNamesOptions);
                    }
                }

                var actualCollectionNames = cursor.ToList();
                actualCollectionNames.Where(n => n != "system.indexes").ShouldBeEquivalentTo(collectionNames);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ListCollections_should_execute_a_ListCollectionsOperation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool usingBatchSize,
            [Values(false, true)] bool usingFilter,
            [Values(false, true)] bool async)
        {
            var session = CreateSession(usingSession);
            var filterDocument = BsonDocument.Parse("{ name : \"awesome\" }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            ListCollectionsOptions options = null;
            if (usingFilter || usingBatchSize)
            {
                options = new ListCollectionsOptions
                {
                    BatchSize = usingBatchSize ? 10 : (int?)null,
                    Filter = usingFilter ? filterDefinition : null
                };
            }
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var op = call.Operation.ShouldBeOfType<ListCollectionsOperation>();
            op.DatabaseNamespace.ShouldBe(_subject.DatabaseNamespace);
            op.NameOnly.ShouldNotHaveValue();
            if (usingFilter || usingBatchSize)
            {
                op.ShouldMatch<ListCollectionsOperation>(
                    (o) =>
                        o.BatchSize == (usingBatchSize ? 10 : (int?)null) &&
                        o.Filter == (usingFilter ? filterDocument : null)
                );
            }
            op.RetryRequested.ShouldBeTrue();
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var op = call.Operation.ShouldBeOfType<RenameCollectionOperation>();
            op.CollectionNamespace.ShouldBe(new CollectionNamespace(_subject.DatabaseNamespace, oldName));
            op.NewCollectionNamespace.ShouldBe(new CollectionNamespace(_subject.DatabaseNamespace, newName));
            op.DropTarget.ShouldBe(options.DropTarget);
            op.WriteConcern.ShouldBeSameAs(writeConcern);
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var binding = call.Binding.ShouldBeOfType<ReadBindingHandle>();
            binding.ReadPreference.ShouldBe(ReadPreference.Primary);

            var op = call.Operation.ShouldBeOfType<ReadCommandOperation<BsonDocument>>();
            op.DatabaseNamespace.ShouldBe(_subject.DatabaseNamespace);
            op.Command.ShouldBe(commandDocument);
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var binding = call.Binding.ShouldBeOfType<ReadBindingHandle>();
            binding.ReadPreference.ShouldBe(readPreference);

            var op = call.Operation.ShouldBeOfType<ReadCommandOperation<BsonDocument>>();
            op.DatabaseNamespace.ShouldBe(_subject.DatabaseNamespace);
            op.Command.ShouldBe(commandDocument);
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var binding = call.Binding.ShouldBeOfType<ReadBindingHandle>();
            binding.ReadPreference.ShouldBe(ReadPreference.Primary);

            var op = call.Operation.ShouldBeOfType<ReadCommandOperation<BsonDocument>>();
            op.DatabaseNamespace.ShouldBe(_subject.DatabaseNamespace);
            op.Command.ShouldBe(commandDocument);
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var binding = call.Binding.ShouldBeOfType<ReadBindingHandle>();
            binding.ReadPreference.ShouldBe(ReadPreference.Primary);

            var op = call.Operation.ShouldBeOfType<ReadCommandOperation<BsonDocument>>();
            op.DatabaseNamespace.ShouldBe(_subject.DatabaseNamespace);
            op.Command.ShouldBe(commandDocument);
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var binding = call.Binding.ShouldBeOfType<ReadBindingHandle>();
            binding.ReadPreference.ShouldBe(ReadPreference.Primary);

            var op = call.Operation.ShouldBeOfType<ReadCommandOperation<BsonDocument>>();
            op.DatabaseNamespace.ShouldBe(_subject.DatabaseNamespace);
            op.Command.ShouldBe("{ count : \"foo\" }");
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var op = call.Operation.ShouldBeOfType<ReadCommandOperation<BsonDocument>>();
            op.RetryRequested.ShouldBeFalse();
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
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
                call.SessionId.ShouldBeSameAs(session.ServerSession.Id);
            }
            else
            {
                call.UsedImplicitSession.ShouldBeTrue();
            }
            call.CancellationToken.ShouldBe(cancellationToken);

            var changeStreamOperation = call.Operation.ShouldBeOfType<ChangeStreamOperation<ChangeStreamDocument<BsonDocument>>>();
            changeStreamOperation.BatchSize.ShouldBe(options.BatchSize);
            changeStreamOperation.Collation.ShouldBeSameAs(options.Collation);
            changeStreamOperation.CollectionNamespace.ShouldBeNull();
            changeStreamOperation.DatabaseNamespace.ShouldBe(_subject.DatabaseNamespace);
            changeStreamOperation.FullDocument.ShouldBe(options.FullDocument);
            changeStreamOperation.MaxAwaitTime.ShouldBe(options.MaxAwaitTime);
            changeStreamOperation.MessageEncoderSettings.ShouldNotBeNull();
            changeStreamOperation.Pipeline.ShouldBe(renderedPipeline);
            changeStreamOperation.ReadConcern.ShouldBe(_subject.Settings.ReadConcern);
            changeStreamOperation.ResultSerializer.ShouldBeOfType<ChangeStreamDocumentSerializer<BsonDocument>>();
            changeStreamOperation.ResumeAfter.ShouldBe(options.ResumeAfter);
            changeStreamOperation.RetryRequested.ShouldBeTrue();
            changeStreamOperation.StartAfter.ShouldBe(options.StartAfter);
            changeStreamOperation.StartAtOperationTime.ShouldBe(options.StartAtOperationTime);
        }

        [Fact]
        public void WithReadConcern_should_return_expected_result()
        {
            var originalReadConcern = new ReadConcern(ReadConcernLevel.Linearizable);
            var subject = CreateSubject().WithReadConcern(originalReadConcern);
            var newReadConcern = new ReadConcern(ReadConcernLevel.Majority);

            var result = subject.WithReadConcern(newReadConcern);

            subject.Settings.ReadConcern.ShouldBeSameAs(originalReadConcern);
            result.Settings.ReadConcern.ShouldBeSameAs(newReadConcern);
            result.WithReadConcern(originalReadConcern).Settings.ShouldBe(subject.Settings);
        }

        [Fact]
        public void WithReadPreference_should_return_expected_result()
        {
            var originalReadPreference = new ReadPreference(ReadPreferenceMode.Secondary);
            var subject = CreateSubject().WithReadPreference(originalReadPreference);
            var newReadPreference = new ReadPreference(ReadPreferenceMode.SecondaryPreferred);

            var result = subject.WithReadPreference(newReadPreference);

            subject.Settings.ReadPreference.ShouldBeSameAs(originalReadPreference);
            result.Settings.ReadPreference.ShouldBeSameAs(newReadPreference);
            result.WithReadPreference(originalReadPreference).Settings.ShouldBe(subject.Settings);
        }

        [Fact]
        public void WithWriteConcern_should_return_expected_result()
        {
            var originalWriteConcern = new WriteConcern(2);
            var subject = CreateSubject().WithWriteConcern(originalWriteConcern);
            var newWriteConcern = new WriteConcern(3);

            var result = subject.WithWriteConcern(newWriteConcern);

            subject.Settings.WriteConcern.ShouldBeSameAs(originalWriteConcern);
            result.Settings.WriteConcern.ShouldBeSameAs(newWriteConcern);
            result.WithWriteConcern(originalWriteConcern).Settings.ShouldBe(subject.Settings);
        }

        // private methods
        private Mock<IMongoClient> CreateMockClient()
        {
            var mockCluster = new Mock<IClusterInternal>();
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
                    var cluster = new Mock<IClusterInternal>().Object;
                    return new MongoDatabase(mockClient.Object, databaseNamespace, settings, cluster, _operationExecutor);
                });

            return mockClient;
        }

        private IClientSessionHandle CreateSession(bool usingSession)
        {
            if (usingSession)
            {
                var cluster = Mock.Of<IClusterInternal>();
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

        private MongoDatabase CreateSubject(string databaseName = "foo", IOperationExecutor operationExecutor = null)
        {
            var settings = new MongoDatabaseSettings();
            settings.ApplyDefaultValues(new MongoClientSettings());
            return new MongoDatabase(
                _client,
                new DatabaseNamespace(databaseName),
                settings,
                new Mock<IClusterInternal>().Object,
                operationExecutor ?? _operationExecutor);
        }

        private RenderedPipelineDefinition<TOutput> RenderPipeline<TOutput>(IMongoDatabase database, PipelineDefinition<NoPipelineInput, TOutput> pipeline)
        {
            var inputSerializer = NoPipelineInputSerializer.Instance;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            return pipeline.Render(new(inputSerializer, serializerRegistry));
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

        private class CountCommand
        {
            [BsonElement("count")]
            public string Collection;
        }
    }
}
