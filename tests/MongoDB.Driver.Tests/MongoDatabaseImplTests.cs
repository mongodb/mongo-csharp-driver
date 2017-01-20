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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Tests;
using Moq;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoDatabaseImplTests
    {
        private MockOperationExecutor _operationExecutor;
        private MongoDatabaseImpl _subject;

        public MongoDatabaseImplTests()
        {
            _operationExecutor = new MockOperationExecutor();
            _subject = CreateSubject(_operationExecutor);
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
        public void CreateCollection_should_execute_the_CreateCollectionOperation_when_options_is_generic(
            [Values(false, true)] bool async)
        {
            var storageEngine = new BsonDocument("awesome", true);
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
                Validator = new BsonDocument("x", 1)
            };
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);

            if (async)
            {
                subject.CreateCollectionAsync("bar", options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.CreateCollection("bar", options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<CreateCollectionOperation>();
            var op = (CreateCollectionOperation)call.Operation;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "bar"));
            op.AutoIndexId.Should().Be(options.AutoIndexId);
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
            var serializerRegistry = options.SerializerRegistry ?? BsonSerializer.SerializerRegistry;
            var documentSerializer = options.DocumentSerializer ?? serializerRegistry.GetSerializer<BsonDocument>();
            var renderedValidator = options.Validator.Render(documentSerializer, serializerRegistry);
            op.Validator.Should().Be(renderedValidator);
            op.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCollection_should_execute_the_CreateCollectionOperation_when_options_is_not_generic(
            [Values(false, true)] bool async)
        {
            var storageEngine = new BsonDocument("awesome", true);
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
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);

            if (async)
            {
                subject.CreateCollectionAsync("bar", options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.CreateCollection("bar", options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<CreateCollectionOperation>();
            var op = (CreateCollectionOperation)call.Operation;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "bar"));
            op.AutoIndexId.Should().Be(options.AutoIndexId);
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
        public void CreateCollection_should_execute_the_CreateCollectionOperation_when_options_is_null(
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);
            CreateCollectionOptions options = null;

            if (async)
            {
                subject.CreateCollectionAsync("bar", options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.CreateCollection("bar", options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<CreateCollectionOperation>();
            var op = (CreateCollectionOperation)call.Operation;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "bar"));
            op.AutoIndexId.Should().NotHaveValue();
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
        public void CreateView_should_execute_a_CreateViewOperation_with_the_expected_ViewName(
            [Values("a", "b")]
            string viewName,
            [Values(false, true)]
            bool async)
        {
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new BsonDocument[0]);

            if (async)
            {
                _subject.CreateViewAsync<BsonDocument, BsonDocument>(viewName, "viewOn", pipeline, null, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _subject.CreateView<BsonDocument, BsonDocument>(viewName, "viewOn", pipeline, null, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            var operation = call.Operation.Should().BeOfType<CreateViewOperation>().Subject;
            operation.ViewName.Should().Be(viewName);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_execute_a_CreateViewOperation_with_the_expected_ViewOn(
            [Values("a", "b")]
            string viewOn,
            [Values(false, true)]
            bool async)
        {
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new BsonDocument[0]);

            if (async)
            {
                _subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", viewOn, pipeline, null, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _subject.CreateView<BsonDocument, BsonDocument>("viewName", viewOn, pipeline, null, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            var operation = call.Operation.Should().BeOfType<CreateViewOperation>().Subject;
            operation.ViewOn.Should().Be(viewOn);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_execute_a_CreateViewOperation_with_the_expected_WriteConcern(
            [Values(1, 2)]
            int w,
            [Values(false, true)]
            bool async)
        {
            var writeConcern = new WriteConcern(w);
            var subject = _subject.WithWriteConcern(writeConcern);
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new BsonDocument[0]);

            if (async)
            {
                subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", "viewOn", pipeline, null, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.CreateView<BsonDocument, BsonDocument>("viewName", "viewOn", pipeline, null, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            var operation = call.Operation.Should().BeOfType<CreateViewOperation>().Subject;
            operation.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_execute_a_CreateViewOperation_with_the_expected_Pipeline(
            [Values(1, 2)]
            int x,
            [Values(false, true)]
            bool async)
        {
            var stages = new[] { new BsonDocument("$match", new BsonDocument("x", x)) };
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(stages);

            if (async)
            {
                _subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", "viewOn", pipeline, null, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _subject.CreateView<BsonDocument, BsonDocument>("viewName", "viewOn", pipeline, null, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            var operation = call.Operation.Should().BeOfType<CreateViewOperation>().Subject;
            operation.Pipeline.Should().Equal(stages);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_execute_a_CreateViewOperation_with_the_expected_Collation(
            [Values(null, "en_US", "fr_CA")]
            string locale,
            [Values(false, true)]
            bool async)
        {
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new BsonDocument[0]);
            var collation = locale == null ? null : new Collation(locale);
            var options = new CreateViewOptions<BsonDocument>
            {
                Collation = collation
            };

            if (async)
            {
                _subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", "viewOn", pipeline, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _subject.CreateView<BsonDocument, BsonDocument>("viewName", "viewOn", pipeline, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            var operation = call.Operation.Should().BeOfType<CreateViewOperation>().Subject;
            operation.Collation.Should().BeSameAs(collation);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_execute_a_CreateViewOperation_with_the_expected_DocumentSerializer(
            [Values(false, true)]
            bool isDocumentSerializerNull,
            [Values(false, true)]
            bool async)
        {
            var mockPipeline = new Mock<PipelineDefinition<BsonDocument, BsonDocument>>();
            var documentSerializer = isDocumentSerializerNull ? null : new BsonDocumentSerializer();
            var stages = new [] { new BsonDocument("$match", new BsonDocument("x", 1)) };
            var renderedPipeline = new RenderedPipelineDefinition<BsonDocument>(stages, BsonDocumentSerializer.Instance);
            mockPipeline.Setup(p => p.Render(documentSerializer ?? BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).Returns(renderedPipeline);

            var options = new CreateViewOptions<BsonDocument>
            {
                DocumentSerializer = documentSerializer
            };

            if (async)
            {
                _subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", "viewOn", mockPipeline.Object, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _subject.CreateView<BsonDocument, BsonDocument>("viewName", "viewOn", mockPipeline.Object, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            var operation = call.Operation.Should().BeOfType<CreateViewOperation>().Subject;
            operation.Pipeline.Should().Equal(stages); // test output of call to Render to see if DocumentSerializer was used
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_execute_a_CreateViewOperation_with_the_expected_SerializerRegistry(
            [Values(false, true)]
            bool isSerializerRegistryNull,
            [Values(false, true)]
            bool async)
        {
            var mockPipeline = new Mock<PipelineDefinition<BsonDocument, BsonDocument>>();
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
            var mockSerializerRegistry = new Mock<IBsonSerializerRegistry>();
            mockSerializerRegistry.Setup(r => r.GetSerializer(typeof(BsonDocument))).Returns(BsonDocumentSerializer.Instance);
            mockSerializerRegistry.Setup(r => r.GetSerializer<BsonDocument>()).Returns(BsonDocumentSerializer.Instance);
            var serializerRegistry = isSerializerRegistryNull ? null : mockSerializerRegistry.Object;
            var stages = new[] { new BsonDocument("$match", new BsonDocument("x", 1)) };
            var renderedPipeline = new RenderedPipelineDefinition<BsonDocument>(stages, BsonDocumentSerializer.Instance);
            mockPipeline.Setup(p => p.Render(documentSerializer, serializerRegistry ?? BsonSerializer.SerializerRegistry)).Returns(renderedPipeline);

            var options = new CreateViewOptions<BsonDocument>
            {
                SerializerRegistry = serializerRegistry
            };

            if (async)
            {
                _subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", "viewOn", mockPipeline.Object, options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _subject.CreateView<BsonDocument, BsonDocument>("viewName", "viewOn", mockPipeline.Object, options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            var operation = call.Operation.Should().BeOfType<CreateViewOperation>().Subject;
            operation.Pipeline.Should().Equal(stages); // test output of call to Render
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_execute_a_CreateViewOperation_with_the_expected_CancellationToken(
            [Values(false, true)]
            bool async)
        {
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new BsonDocument[0]);
            var cancellationToken = new CancellationTokenSource().Token;

            if (async)
            {
                _subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", "viewOn", pipeline, null, cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                _subject.CreateView<BsonDocument, BsonDocument>("viewName", "viewOn", pipeline, null, cancellationToken);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();
            var operation = call.Operation.Should().BeOfType<CreateViewOperation>().Subject;
            call.CancellationToken.Should().Be(cancellationToken);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_throw_when_viewName_is_null(
            [Values(false, true)]
            bool async)
        {
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new BsonDocument[0]);

            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    _subject.CreateViewAsync<BsonDocument, BsonDocument>(null, "viewOn", pipeline).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.CreateView<BsonDocument, BsonDocument>(null, "viewOn", pipeline);
                }
            });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("viewName");
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_throw_when_viewOn_is_null(
            [Values(false, true)]
            bool async)
        {
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new BsonDocument[0]);

            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    _subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", null, pipeline).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.CreateView<BsonDocument, BsonDocument>("viewName", null, pipeline);
                }
            });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("viewOn");
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateView_should_throw_when_pipeline_is_null(
            [Values(false, true)]
            bool async)
        {
            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    _subject.CreateViewAsync<BsonDocument, BsonDocument>("viewName", "viewOn", null).GetAwaiter().GetResult();
                }
                else
                {
                    _subject.CreateView<BsonDocument, BsonDocument>("viewName", "viewOn", null);
                }
            });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Theory]
        [ParameterAttributeData]
        public void DropCollection_should_execute_the_DropCollectionOperation(
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);

            if (async)
            {
                subject.DropCollectionAsync("bar", CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.DropCollection("bar", CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropCollectionOperation>();
            var op = (DropCollectionOperation)call.Operation;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "bar"));
            op.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void ListCollections_should_execute_the_ListCollectionsOperation(
            [Values(false, true)] bool async)
        {
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult<IAsyncCursor<BsonDocument>>(mockCursor.Object);
            var filter = new BsonDocument("name", "awesome");

            if (async)
            {
                _subject.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter }, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _subject.ListCollections(new ListCollectionsOptions { Filter = filter }, CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            call.Operation.Should().BeOfType<ListCollectionsOperation>();
            var op = (ListCollectionsOperation)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Filter.Should().Be(filter);
        }

        [Theory]
        [ParameterAttributeData]
        public void RenameCollection_should_execute_the_RenameCollectionOperation(
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(1);
            var subject = _subject.WithWriteConcern(writeConcern);
            var options = new RenameCollectionOptions
            {
                DropTarget = false,
            };

            if (async)
            {
                subject.RenameCollectionAsync("bar", "baz", options, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.RenameCollection("bar", "baz", options, CancellationToken.None);
            }

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<RenameCollectionOperation>();
            var op = (RenameCollectionOperation)call.Operation;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "bar"));
            op.NewCollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "baz"));
            op.DropTarget.Should().Be(options.DropTarget);
            op.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_default_to_ReadPreference_primary(
            [Values(false, true)] bool async)
        {
            var cmd = new BsonDocument("count", "foo");

            if (async)
            {
                _subject.RunCommandAsync<BsonDocument>(cmd).GetAwaiter().GetResult();
            }
            else
            {
                _subject.RunCommand<BsonDocument>(cmd);
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Binding.Should().BeOfType<ReadPreferenceBinding>();
            var binding = (ReadPreferenceBinding)call.Binding;
            binding.ReadPreference.Should().Be(ReadPreference.Primary);

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_use_the_provided_ReadPreference(
            [Values(false, true)] bool async)
        {
            var cmd = new BsonDocument("count", "foo");

            if (async)
            {
                _subject.RunCommandAsync<BsonDocument>(cmd, ReadPreference.Secondary, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _subject.RunCommand<BsonDocument>(cmd, ReadPreference.Secondary, CancellationToken.None);
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Binding.Should().BeOfType<ReadPreferenceBinding>();
            var binding = (ReadPreferenceBinding)call.Binding;
            binding.ReadPreference.Should().Be(ReadPreference.Secondary);

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_run_a_non_read_command(
            [Values(false, true)] bool async)
        {
            var cmd = new BsonDocument("shutdown", 1);

            if (async)
            {
                _subject.RunCommandAsync<BsonDocument>(cmd).GetAwaiter().GetResult();
            }
            else
            {
                _subject.RunCommand<BsonDocument>(cmd);
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Binding.Should().BeOfType<ReadPreferenceBinding>();
            var binding = (ReadPreferenceBinding)call.Binding;
            binding.ReadPreference.Should().Be(ReadPreference.Primary);

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be(cmd);
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_run_a_json_command(
            [Values(false, true)] bool async)
        {
            if (async)
            {
                _subject.RunCommandAsync<BsonDocument>("{count: \"foo\"}").GetAwaiter().GetResult();
            }
            else
            {
                _subject.RunCommand<BsonDocument>("{count: \"foo\"}");
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Binding.Should().BeOfType<ReadPreferenceBinding>();
            var binding = (ReadPreferenceBinding)call.Binding;
            binding.ReadPreference.Should().Be(ReadPreference.Primary);

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Theory]
        [ParameterAttributeData]
        public void RunCommand_should_run_a_serialized_command(
            [Values(false, true)] bool async)
        {
            var cmd = new CountCommand { Collection = "foo" };

            if (async)
            {
                _subject.RunCommandAsync(new ObjectCommand<BsonDocument>(cmd)).GetAwaiter().GetResult();
            }
            else
            {
                _subject.RunCommand(new ObjectCommand<BsonDocument>(cmd));
            }

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Binding.Should().BeOfType<ReadPreferenceBinding>();
            var binding = (ReadPreferenceBinding)call.Binding;
            binding.ReadPreference.Should().Be(ReadPreference.Primary);

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Fact]
        public void WithReadConcern_should_return_expected_result()
        {
            var originalReadConcern = new ReadConcern(ReadConcernLevel.Linearizable);
            var subject = CreateSubject(_operationExecutor).WithReadConcern(originalReadConcern);
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
            var subject = CreateSubject(_operationExecutor).WithReadPreference(originalReadPreference);
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
            var subject = CreateSubject(_operationExecutor).WithWriteConcern(originalWriteConcern);
            var newWriteConcern = new WriteConcern(3);

            var result = subject.WithWriteConcern(newWriteConcern);

            subject.Settings.WriteConcern.Should().BeSameAs(originalWriteConcern);
            result.Settings.WriteConcern.Should().BeSameAs(newWriteConcern);
            result.WithWriteConcern(originalWriteConcern).Settings.Should().Be(subject.Settings);
        }

        // private methods
        private MongoDatabaseImpl CreateSubject(IOperationExecutor operationExecutor)
        {
            var settings = new MongoDatabaseSettings();
            settings.ApplyDefaultValues(new MongoClientSettings());
            return new MongoDatabaseImpl(
                new Mock<IMongoClient>().Object,
                new DatabaseNamespace("foo"),
                settings,
                new Mock<ICluster>().Object,
                _operationExecutor);
        }

        private class CountCommand
        {
            [BsonElement("count")]
            public string Collection;
        }
    }
}