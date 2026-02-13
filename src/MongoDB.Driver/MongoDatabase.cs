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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver
{
    internal sealed class MongoDatabase : IMongoDatabase
    {
        // private fields
        private readonly IMongoClient _client;
        private readonly IClusterInternal _cluster;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly IOperationExecutor _operationExecutor;
        private readonly MongoDatabaseSettings _settings;

        // constructors
        public MongoDatabase(IMongoClient client, DatabaseNamespace databaseNamespace, MongoDatabaseSettings settings, IClusterInternal cluster, IOperationExecutor operationExecutor)
        {
            _client = Ensure.IsNotNull(client, nameof(client));
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _settings = Ensure.IsNotNull(settings, nameof(settings)).Freeze();
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _operationExecutor = Ensure.IsNotNull(operationExecutor, nameof(operationExecutor));
        }

        // public properties
        public IMongoClient Client => _client;
        public DatabaseNamespace DatabaseNamespace => _databaseNamespace;
        public MongoDatabaseSettings Settings => _settings;

        // public methods
        public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return Aggregate(session, pipeline, options, cancellationToken: cancellationToken);
        }

        public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            options ??= new AggregateOptions();

            var renderArgs = GetRenderArgs(NoPipelineInputSerializer.Instance, options.TranslationOptions);
            var renderedPipeline = AggregateHelper.RenderAggregatePipeline(pipeline, renderArgs, out var isAggregateToCollection);
            if (isAggregateToCollection)
            {
                var aggregateOperation = CreateAggregateToCollectionOperation(renderedPipeline, options);
                ExecuteWriteOperation(session, aggregateOperation, options.Timeout, null, cancellationToken);
                return CreateAggregateToCollectionResultCursor(session, renderedPipeline, options);
            }
            else
            {
                var aggregateOperation = CreateAggregateOperation(renderedPipeline, options);
                return ExecuteReadOperation(session, aggregateOperation, options.Timeout, cancellationToken);
            }
        }

        public async Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return await AggregateAsync(session, pipeline, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            options ??= new AggregateOptions();

            var renderArgs = GetRenderArgs(NoPipelineInputSerializer.Instance, options.TranslationOptions);
            var renderedPipeline = AggregateHelper.RenderAggregatePipeline(pipeline, renderArgs, out var isAggregateToCollection);
            if (isAggregateToCollection)
            {
                var aggregateOperation = CreateAggregateToCollectionOperation(renderedPipeline, options);
                await ExecuteWriteOperationAsync(session, aggregateOperation, options.Timeout, null, cancellationToken).ConfigureAwait(false);
                return CreateAggregateToCollectionResultCursor(session, renderedPipeline, options);
            }
            else
            {
                var aggregateOperation = CreateAggregateOperation(renderedPipeline, options);
                return await ExecuteReadOperationAsync(session, aggregateOperation, options.Timeout, cancellationToken).ConfigureAwait(false);
            }
        }

        public void AggregateToCollection<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            AggregateToCollection(session, pipeline, options, cancellationToken);
        }

        public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            options ??= new AggregateOptions();

            var renderArgs = GetRenderArgs(NoPipelineInputSerializer.Instance, options.TranslationOptions);
            var renderedPipeline = AggregateHelper.RenderAggregatePipeline(pipeline, renderArgs, out var isAggregateToCollection);
            if (!isAggregateToCollection)
            {
                throw new InvalidOperationException("AggregateToCollection requires that the last stage be $out or $merge.");
            }

            var aggregateOperation = CreateAggregateToCollectionOperation(renderedPipeline, options);
            ExecuteWriteOperation(session, aggregateOperation, options.Timeout, null, cancellationToken);
        }

        public async Task AggregateToCollectionAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            await AggregateToCollectionAsync(session, pipeline, options, cancellationToken).ConfigureAwait(false);
        }

        public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            options ??= new AggregateOptions();

            var renderArgs = GetRenderArgs(NoPipelineInputSerializer.Instance, options.TranslationOptions);
            var renderedPipeline = AggregateHelper.RenderAggregatePipeline(pipeline, renderArgs, out var isAggregateToCollection);
            if (!isAggregateToCollection)
            {
                throw new InvalidOperationException("AggregateToCollectionAsync requires that the last stage be $out or $merge.");
            }

            var aggregateOperation = CreateAggregateToCollectionOperation(renderedPipeline, options);
            return ExecuteWriteOperationAsync(session, aggregateOperation, options.Timeout, null, cancellationToken);
        }

        public void CreateCollection(string name, CreateCollectionOptions options, CancellationToken cancellationToken)
        {
            using var session = _operationExecutor.StartImplicitSession();
            CreateCollection(session, name, options, cancellationToken);
        }

        public void CreateCollection(IClientSessionHandle session, string name, CreateCollectionOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNullOrEmpty(name, nameof(name));

            if (options == null)
            {
                CreateCollectionHelper<BsonDocument>(session, name, null, cancellationToken);
                return;
            }

            if (options.GetType() == typeof(CreateCollectionOptions))
            {
                var genericOptions = CreateCollectionOptions<BsonDocument>.CoercedFrom(options);
                CreateCollectionHelper<BsonDocument>(session, name, genericOptions, cancellationToken);
                return;
            }

            var genericMethodDefinition = typeof(MongoDatabase).GetTypeInfo().GetMethod(nameof(CreateCollectionHelper), BindingFlags.NonPublic | BindingFlags.Instance);
            var documentType = options.GetType().GetTypeInfo().GetGenericArguments()[0];
            var methodInfo = genericMethodDefinition.MakeGenericMethod(documentType);
            try
            {
                methodInfo.Invoke(this, new object[] { session, name, options, cancellationToken });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public async Task CreateCollectionAsync(string name, CreateCollectionOptions options, CancellationToken cancellationToken)
        {
            using var session = _operationExecutor.StartImplicitSession();
            await CreateCollectionAsync(session, name, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateCollectionAsync(IClientSessionHandle session, string name, CreateCollectionOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNullOrEmpty(name, nameof(name));

            if (options == null)
            {
                await CreateCollectionHelperAsync<BsonDocument>(session, name, null, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (options.GetType() == typeof(CreateCollectionOptions))
            {
                var genericOptions = CreateCollectionOptions<BsonDocument>.CoercedFrom(options);
                await CreateCollectionHelperAsync<BsonDocument>(session, name, genericOptions, cancellationToken).ConfigureAwait(false);
                return;
            }

            var genericMethodDefinition = typeof(MongoDatabase).GetTypeInfo().GetMethod(nameof(CreateCollectionHelperAsync), BindingFlags.NonPublic | BindingFlags.Instance);
            var documentType = options.GetType().GetTypeInfo().GetGenericArguments()[0];
            var methodInfo = genericMethodDefinition.MakeGenericMethod(documentType);
            try
            {
                await ((Task)methodInfo.Invoke(this, new object[] { session, name, options, cancellationToken })).ConfigureAwait(false);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public void CreateView<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            CreateView(session, viewName, viewOn, pipeline, options, cancellationToken);
        }

        public void CreateView<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(viewName, nameof(viewName));
            Ensure.IsNotNull(viewOn, nameof(viewOn));
            Ensure.IsNotNull(pipeline, nameof(pipeline));

            var operation = CreateCreateViewOperation(viewName, viewOn, pipeline, options);
            ExecuteWriteOperation(session, operation, options?.Timeout, viewName, cancellationToken);
        }

        public async Task CreateViewAsync<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            await CreateViewAsync(session, viewName, viewOn, pipeline, options, cancellationToken).ConfigureAwait(false);
        }

        public Task CreateViewAsync<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(viewName, nameof(viewName));
            Ensure.IsNotNull(viewOn, nameof(viewOn));
            Ensure.IsNotNull(pipeline, nameof(pipeline));

            var operation = CreateCreateViewOperation(viewName, viewOn, pipeline, options);
            return ExecuteWriteOperationAsync(session, operation, options?.Timeout, viewName, cancellationToken);
        }

        public void DropCollection(string name, CancellationToken cancellationToken)
        {
            DropCollection(name, options: null, cancellationToken);
        }

        public void DropCollection(string name, DropCollectionOptions options, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            DropCollection(session, name, options, cancellationToken);
        }

        public void DropCollection(IClientSessionHandle session, string name, CancellationToken cancellationToken)
        {
            DropCollection(session, name, options: null, cancellationToken);
        }

        public void DropCollection(IClientSessionHandle session, string name, DropCollectionOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNullOrEmpty(name, nameof(name));

            var collectionNamespace = new CollectionNamespace(_databaseNamespace, name);
            var encryptedFields = GetEffectiveEncryptedFields(session, collectionNamespace, options, cancellationToken);
            var operation = CreateDropCollectionOperation(collectionNamespace, encryptedFields);
            ExecuteWriteOperation(session, operation, options?.Timeout, name, cancellationToken);
        }

        public Task DropCollectionAsync(string name, CancellationToken cancellationToken)
            => DropCollectionAsync(name, options: null, cancellationToken);

        public async Task DropCollectionAsync(string name, DropCollectionOptions options, CancellationToken cancellationToken)
        {
            using var session = _operationExecutor.StartImplicitSession();
            await DropCollectionAsync(session, name, options, cancellationToken).ConfigureAwait(false);
        }

        public Task DropCollectionAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken)
            => DropCollectionAsync(session, name, options: null, cancellationToken);

        public async Task DropCollectionAsync(IClientSessionHandle session, string name, DropCollectionOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNullOrEmpty(name, nameof(name));

            var collectionNamespace = new CollectionNamespace(_databaseNamespace, name);
            var encryptedFields = await GetEffectiveEncryptedFieldsAsync(session, collectionNamespace, options, cancellationToken).ConfigureAwait(false);
            var operation = CreateDropCollectionOperation(collectionNamespace, encryptedFields);
            await ExecuteWriteOperationAsync(session, operation, options?.Timeout, name, cancellationToken).ConfigureAwait(false);
        }

        public IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings)
        {
            Ensure.IsNotNullOrEmpty(name, nameof(name));

            settings = settings == null ?
                new MongoCollectionSettings() :
                settings.Clone();

            settings.ApplyDefaultValues(_settings);

            return new MongoCollectionImpl<TDocument>(this, new CollectionNamespace(_databaseNamespace, name), settings, _cluster, _operationExecutor);
        }

        public IAsyncCursor<string> ListCollectionNames(ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return ListCollectionNames(session, options, cancellationToken);
        }

        public IAsyncCursor<string> ListCollectionNames(IClientSessionHandle session, ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            var operation = CreateListCollectionNamesOperation(options);
            var readPreference = session.GetEffectiveReadPreference(ReadPreference.Primary);
            var cursor = ExecuteReadOperation(session, operation, readPreference, options?.Timeout, cancellationToken);
            return new BatchTransformingAsyncCursor<BsonDocument, string>(cursor, ExtractCollectionNames);
        }

        public async Task<IAsyncCursor<string>> ListCollectionNamesAsync(ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return await ListCollectionNamesAsync(session, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IAsyncCursor<string>> ListCollectionNamesAsync(IClientSessionHandle session, ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            var operation = CreateListCollectionNamesOperation(options);
            var readPreference = session.GetEffectiveReadPreference(ReadPreference.Primary);
            var cursor = await ExecuteReadOperationAsync(session, operation, readPreference, options?.Timeout, cancellationToken).ConfigureAwait(false);
            return new BatchTransformingAsyncCursor<BsonDocument, string>(cursor, ExtractCollectionNames);
        }

        public IAsyncCursor<BsonDocument> ListCollections(ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return ListCollections(session, options, cancellationToken);
        }

        public IAsyncCursor<BsonDocument> ListCollections(IClientSessionHandle session, ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(session, nameof(session));
            var operation = CreateListCollectionsOperation(options);
            var readPreference = session.GetEffectiveReadPreference(ReadPreference.Primary);
            return ExecuteReadOperation(session, operation, readPreference, options?.Timeout, cancellationToken);
        }

        public async Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return await ListCollectionsAsync(session, options, cancellationToken).ConfigureAwait(false);
        }

        public Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(IClientSessionHandle session, ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(session, nameof(session));
            var operation = CreateListCollectionsOperation(options);
            var readPreference = session.GetEffectiveReadPreference(ReadPreference.Primary);
            return ExecuteReadOperationAsync(session, operation, readPreference, options?.Timeout, cancellationToken);
        }

        public void RenameCollection(string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
        {
            using var session = _operationExecutor.StartImplicitSession();
            RenameCollection(session, oldName, newName, options, cancellationToken);
        }

        public void RenameCollection(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNullOrEmpty(oldName, nameof(oldName));
            Ensure.IsNotNullOrEmpty(newName, nameof(newName));

            var operation = CreateRenameCollectionOperation(oldName, newName, options);
            ExecuteWriteOperation(session, operation, options?.Timeout, null, cancellationToken);
        }

        public async Task RenameCollectionAsync(string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
        {
            using var session = _operationExecutor.StartImplicitSession();
            await RenameCollectionAsync(session, oldName, newName, options, cancellationToken).ConfigureAwait(false);
        }

        public Task RenameCollectionAsync(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNullOrEmpty(oldName, nameof(oldName));
            Ensure.IsNotNullOrEmpty(newName, nameof(newName));

            var operation = CreateRenameCollectionOperation(oldName, newName, options);
            return ExecuteWriteOperationAsync(session, operation, options?.Timeout, null, cancellationToken);
        }

        public TResult RunCommand<TResult>(Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return RunCommand(session, command, readPreference, cancellationToken);
        }

        public TResult RunCommand<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(command, nameof(command));

            var operation = CreateRunCommandOperation(command);
            var effectiveReadPreference = readPreference;
            if (readPreference == null)
            {
                effectiveReadPreference = session.GetEffectiveReadPreference(ReadPreference.Primary);
            }

            // TODO: CSOT: See what run command should do with timeout
            return ExecuteReadOperation(session, operation, effectiveReadPreference, null, cancellationToken);
        }

        public async Task<TResult> RunCommandAsync<TResult>(Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return await RunCommandAsync(session, command, readPreference, cancellationToken).ConfigureAwait(false);
        }

        public Task<TResult> RunCommandAsync<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(command, nameof(command));

            var operation = CreateRunCommandOperation(command);
            var effectiveReadPreference = readPreference;
            if (readPreference == null)
            {
                effectiveReadPreference = session.GetEffectiveReadPreference(ReadPreference.Primary);
            }

            // TODO: CSOT: See what run command should do with timeout
            return ExecuteReadOperationAsync(session, operation, effectiveReadPreference, null, cancellationToken);
        }

        public IChangeStreamCursor<TResult> Watch<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return Watch(session, pipeline, options, cancellationToken);
        }

        public IChangeStreamCursor<TResult> Watch<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));

            var operation = CreateChangeStreamOperation(pipeline, options);
            return ExecuteReadOperation(session, operation, options?.Timeout, cancellationToken);
        }

        public async Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            using var session = _operationExecutor.StartImplicitSession();
            return await WatchAsync(session, pipeline, options, cancellationToken).ConfigureAwait(false);
        }

        public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));

            var operation = CreateChangeStreamOperation(pipeline, options);
            return ExecuteReadOperationAsync(session, operation, options?.Timeout, cancellationToken);
        }

        public IMongoDatabase WithReadConcern(ReadConcern readConcern)
        {
            Ensure.IsNotNull(readConcern, nameof(readConcern));
            var newSettings = _settings.Clone();
            newSettings.ReadConcern = readConcern;
            return new MongoDatabase(_client, _databaseNamespace, newSettings, _cluster, _operationExecutor);
        }

        public IMongoDatabase WithReadPreference(ReadPreference readPreference)
        {
            Ensure.IsNotNull(readPreference, nameof(readPreference));
            var newSettings = _settings.Clone();
            newSettings.ReadPreference = readPreference;
            return new MongoDatabase(_client, _databaseNamespace, newSettings, _cluster, _operationExecutor);
        }

        public IMongoDatabase WithWriteConcern(WriteConcern writeConcern)
        {
            Ensure.IsNotNull(writeConcern, nameof(writeConcern));
            var newSettings = _settings.Clone();
            newSettings.WriteConcern = writeConcern;
            return new MongoDatabase(_client, _databaseNamespace, newSettings, _cluster, _operationExecutor);
        }

        // private methods

        private AggregateOperation<TResult> CreateAggregateOperation<TResult>(RenderedPipelineDefinition<TResult> renderedPipeline, AggregateOptions options)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            return new AggregateOperation<TResult>(
                _databaseNamespace,
                renderedPipeline.Documents,
                renderedPipeline.OutputSerializer,
                messageEncoderSettings)
            {
                AllowDiskUse = options.AllowDiskUse,
                BatchSize = options.BatchSize,
                Collation = options.Collation,
                Comment = options.Comment,
                Hint = options.Hint,
                Let = options.Let,
                MaxAwaitTime = options.MaxAwaitTime,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _client.Settings.RetryReads,
#pragma warning disable 618
                UseCursor = options.UseCursor
#pragma warning restore 618
            };
        }

        private IAsyncCursor<TResult> CreateAggregateToCollectionResultCursor<TResult>(IClientSessionHandle session, RenderedPipelineDefinition<TResult> pipeline, AggregateOptions options)
        {
            var outputCollectionNamespace = AggregateHelper.GetOutCollection(pipeline.Documents.Last(), _databaseNamespace);

            // because auto encryption is not supported for non-collection commands.
            // So, an error will be thrown in the previous CreateAggregateToCollectionOperation step.
            // However, since we've added encryption configuration for CreateAggregateToCollectionOperation operation,
            // it's not superfluous to also add it here
            var messageEncoderSettings = GetMessageEncoderSettings();
            var findOperation = new FindOperation<TResult>(outputCollectionNamespace, pipeline.OutputSerializer, messageEncoderSettings)
            {
                BatchSize = options.BatchSize,
                Collation = options.Collation,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _client.Settings.RetryReads
            };

            // we want to delay execution of the find because the user may
            // not want to iterate the results at all...
            var forkedSession = session.Fork();
            var deferredCursor = new DeferredAsyncCursor<TResult>(
                () => forkedSession.Dispose(),
                ct => ExecuteReadOperation(forkedSession, findOperation, ReadPreference.Primary, options.Timeout, ct),
                ct => ExecuteReadOperationAsync(forkedSession, findOperation, ReadPreference.Primary, options.Timeout, ct));
            return deferredCursor;
        }

        private AggregateToCollectionOperation CreateAggregateToCollectionOperation<TResult>(RenderedPipelineDefinition<TResult> renderedPipeline, AggregateOptions options)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            return new AggregateToCollectionOperation(
                _databaseNamespace,
                renderedPipeline.Documents,
                messageEncoderSettings)
            {
                AllowDiskUse = options.AllowDiskUse,
                BypassDocumentValidation = options.BypassDocumentValidation,
                Collation = options.Collation,
                Comment = options.Comment,
                Hint = options.Hint,
                Let = options.Let,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                ReadPreference = _settings.ReadPreference,
                WriteConcern = _settings.WriteConcern
            };
        }

        private void CreateCollectionHelper<TDocument>(IClientSessionHandle session, string name, CreateCollectionOptions<TDocument> options, CancellationToken cancellationToken)
        {
            var operation = CreateCreateCollectionOperation(name, options);
            ExecuteWriteOperation(session, operation, options?.Timeout, name, cancellationToken);
        }

        private Task CreateCollectionHelperAsync<TDocument>(IClientSessionHandle session, string name, CreateCollectionOptions<TDocument> options, CancellationToken cancellationToken)
        {
            var operation = CreateCreateCollectionOperation(name, options);
            return ExecuteWriteOperationAsync(session, operation, options?.Timeout, name, cancellationToken);
        }

        private IWriteOperation<BsonDocument> CreateCreateCollectionOperation<TDocument>(string name, CreateCollectionOptions<TDocument> options)
        {
            options ??= new CreateCollectionOptions<TDocument>();
            var translationOptions = _client.Settings.TranslationOptions;
            var serializerRegistry = options.SerializerRegistry ?? BsonSerializer.SerializerRegistry;
            var documentSerializer = options.DocumentSerializer ?? serializerRegistry.GetSerializer<TDocument>();

            var clusteredIndex = options.ClusteredIndex?.Render(documentSerializer, serializerRegistry, translationOptions);
            var validator = options.Validator?.Render(new(documentSerializer, serializerRegistry, translationOptions: translationOptions));

            var collectionNamespace = new CollectionNamespace(_databaseNamespace, name);

            var effectiveEncryptedFields = EncryptedCollectionHelper.GetEffectiveEncryptedFields(collectionNamespace, options.EncryptedFields, _client.Settings?.AutoEncryptionOptions?.EncryptedFieldsMap);
            var messageEncoderSettings = GetMessageEncoderSettings();

            return CreateCollectionOperation.CreateEncryptedCreateCollectionOperationIfConfigured(
                collectionNamespace,
                effectiveEncryptedFields,
                messageEncoderSettings,
                createCollectionOperationConfigurator: cco =>
                {
                    cco.Capped = options.Capped;
                    cco.ChangeStreamPreAndPostImages = options.ChangeStreamPreAndPostImagesOptions?.BackingDocument;
                    cco.ClusteredIndex = clusteredIndex;
                    cco.Collation = options.Collation;
                    cco.ExpireAfter = options.ExpireAfter;
                    cco.IndexOptionDefaults = options.IndexOptionDefaults?.ToBsonDocument();
                    cco.MaxDocuments = options.MaxDocuments;
                    cco.MaxSize = options.MaxSize;
                    cco.StorageEngine = options.StorageEngine;
                    cco.TimeSeriesOptions = options.TimeSeriesOptions;
                    cco.ValidationAction = options.ValidationAction;
                    cco.ValidationLevel = options.ValidationLevel;
                    cco.Validator = validator;
                    cco.WriteConcern = _settings.WriteConcern;
                });
        }

        private CreateViewOperation CreateCreateViewOperation<TDocument, TResult>(
            string viewName,
            string viewOn,
            PipelineDefinition<TDocument, TResult> pipeline,
            CreateViewOptions<TDocument> options)
        {
            options ??= new CreateViewOptions<TDocument>();

            var translationOptions = _client.Settings.TranslationOptions;
            var serializerRegistry = options.SerializerRegistry ?? BsonSerializer.SerializerRegistry;
            var documentSerializer = options.DocumentSerializer ?? serializerRegistry.GetSerializer<TDocument>();
            var pipelineDocuments = pipeline.Render(new (documentSerializer, serializerRegistry, translationOptions: translationOptions)).Documents;
            return new CreateViewOperation(_databaseNamespace, viewName, viewOn, pipelineDocuments, GetMessageEncoderSettings())
            {
                Collation = options.Collation,
                WriteConcern = _settings.WriteConcern
            };
        }

        private IWriteOperation<BsonDocument> CreateDropCollectionOperation(CollectionNamespace collectionNamespace, BsonDocument effectiveEncryptedFields)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            return DropCollectionOperation.CreateEncryptedDropCollectionOperationIfConfigured(
                collectionNamespace,
                effectiveEncryptedFields,
                messageEncoderSettings,
                (dco) =>
                {
                    dco.WriteConcern = _settings.WriteConcern;
                });
        }

        private ListCollectionsOperation CreateListCollectionNamesOperation(ListCollectionNamesOptions options)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var renderArgs = GetRenderArgs(BsonDocumentSerializer.Instance);
            return new ListCollectionsOperation(_databaseNamespace, messageEncoderSettings)
            {
                AuthorizedCollections = options?.AuthorizedCollections,
                Comment = options?.Comment,
                Filter = options?.Filter?.Render(renderArgs),
                NameOnly = true,
                RetryRequested = _client.Settings.RetryReads
            };
        }

        private ListCollectionsOperation CreateListCollectionsOperation(ListCollectionsOptions options)
        {
            var renderArgs = GetRenderArgs(BsonDocumentSerializer.Instance);
            var messageEncoderSettings = GetMessageEncoderSettings();
            return new ListCollectionsOperation(_databaseNamespace, messageEncoderSettings)
            {
                BatchSize = options?.BatchSize,
                Comment = options?.Comment,
                Filter = options?.Filter?.Render(renderArgs),
                RetryRequested = _client.Settings.RetryReads
            };
        }

        private RenameCollectionOperation CreateRenameCollectionOperation(string oldName, string newName, RenameCollectionOptions options)
        {
            options ??= new RenameCollectionOptions();

            var messageEncoderSettings = GetMessageEncoderSettings();
            return new RenameCollectionOperation(
                new CollectionNamespace(_databaseNamespace, oldName),
                new CollectionNamespace(_databaseNamespace, newName),
                messageEncoderSettings)
            {
                DropTarget = options.DropTarget,
                WriteConcern = _settings.WriteConcern
            };
        }

        private ReadCommandOperation<TResult> CreateRunCommandOperation<TResult>(Command<TResult> command)
        {
            var renderedCommand = command.Render(_settings.SerializerRegistry);
            var messageEncoderSettings = GetMessageEncoderSettings();
            return new ReadCommandOperation<TResult>(_databaseNamespace, renderedCommand.Document, renderedCommand.ResultSerializer, messageEncoderSettings, operationName: "runCommand")
            {
                RetryRequested = false
            };
        }

        private ChangeStreamOperation<TResult> CreateChangeStreamOperation<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options)
        {
            var translationOptions = _client.Settings.TranslationOptions;

            return ChangeStreamHelper.CreateChangeStreamOperation(
                this,
                pipeline,
                options,
                _settings.ReadConcern,
                GetMessageEncoderSettings(),
                _client.Settings.RetryReads,
                translationOptions);
        }

        private OperationContext CreateOperationContext(IClientSessionHandle session, TimeSpan? timeout, string operationName, string collectionName, CancellationToken cancellationToken)
        {
            var operationContext = session.WrappedCoreSession.CurrentTransaction?.OperationContext;
            if (operationContext != null && timeout != null)
            {
                throw new InvalidOperationException("Cannot specify per operation timeout inside transaction.");
            }

            var baseContext = operationContext?.Fork() ?? new OperationContext(timeout ?? _settings.Timeout, cancellationToken);

            if (operationName != null)
            {
                var tracingOptions = _client.Settings.TracingOptions;
                var isTracingEnabled = tracingOptions == null || !tracingOptions.Disabled;
                var contextWithMetadata = baseContext.WithOperationMetadata(operationName, _databaseNamespace.DatabaseName, collectionName, isTracingEnabled);
                baseContext.Dispose();
                return contextWithMetadata;
            }

            return baseContext;
        }

        private TResult ExecuteReadOperation<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
            => ExecuteReadOperation(session, operation, null, timeout, cancellationToken);

        private TResult ExecuteReadOperation<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, ReadPreference explicitReadPreference, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var readPreference = explicitReadPreference ?? session.GetEffectiveReadPreference(_settings.ReadPreference);
            using var operationContext = CreateOperationContext(session, timeout, operation.OperationName, null, cancellationToken);
            return _operationExecutor.ExecuteReadOperation(operationContext, session, operation, readPreference, true);
        }

        private Task<TResult> ExecuteReadOperationAsync<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
            => ExecuteReadOperationAsync(session, operation, null, timeout, cancellationToken);

        private async Task<TResult> ExecuteReadOperationAsync<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, ReadPreference explicitReadPreference, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var readPreference = explicitReadPreference ?? session.GetEffectiveReadPreference(_settings.ReadPreference);
            using var operationContext = CreateOperationContext(session, timeout, operation.OperationName, null, cancellationToken);
            return await _operationExecutor.ExecuteReadOperationAsync(operationContext, session, operation, readPreference, true).ConfigureAwait(false);
        }

        private TResult ExecuteWriteOperation<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, TimeSpan? timeout, string collectionName, CancellationToken cancellationToken)
        {
            using var operationContext = CreateOperationContext(session, timeout, operation.OperationName, collectionName, cancellationToken);
            return _operationExecutor.ExecuteWriteOperation(operationContext, session, operation, true);
        }

        private async Task<TResult> ExecuteWriteOperationAsync<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, TimeSpan? timeout, string collectionName, CancellationToken cancellationToken)
        {
            using var operationContext = CreateOperationContext(session, timeout, operation.OperationName, collectionName, cancellationToken);
            return await _operationExecutor.ExecuteWriteOperationAsync(operationContext, session, operation, true).ConfigureAwait(false);
        }

        private IEnumerable<string> ExtractCollectionNames(IEnumerable<BsonDocument> collections)
        {
            return collections.Select(collection => collection["name"].AsString);
        }

        private BsonDocument GetEffectiveEncryptedFields(IClientSessionHandle session, CollectionNamespace collectionNamespace, DropCollectionOptions options, CancellationToken cancellationToken)
        {
            var encryptedFieldsMap = _client.Settings?.AutoEncryptionOptions?.EncryptedFieldsMap;
            if (!EncryptedCollectionHelper.TryGetEffectiveEncryptedFields(collectionNamespace, options?.EncryptedFields, encryptedFieldsMap, out var effectiveEncryptedFields))
            {
                if (encryptedFieldsMap != null)
                {
                    var listCollectionOptions = new ListCollectionsOptions() { Filter = $"{{ name : '{collectionNamespace.CollectionName}' }}" };
                    var currentCollectionInfo = ListCollections(session, listCollectionOptions, cancellationToken: cancellationToken).FirstOrDefault();
                    effectiveEncryptedFields = currentCollectionInfo
                        ?.GetValue("options", defaultValue: null)
                        ?.AsBsonDocument
                        ?.GetValue("encryptedFields", defaultValue: null)
                        ?.ToBsonDocument();
                }
            }

            return effectiveEncryptedFields;
        }

        private async Task<BsonDocument> GetEffectiveEncryptedFieldsAsync(IClientSessionHandle session, CollectionNamespace collectionNamespace, DropCollectionOptions options, CancellationToken cancellationToken)
        {
            var encryptedFieldsMap = _client.Settings?.AutoEncryptionOptions?.EncryptedFieldsMap;
            if (!EncryptedCollectionHelper.TryGetEffectiveEncryptedFields(collectionNamespace, options?.EncryptedFields, encryptedFieldsMap, out var effectiveEncryptedFields))
            {
                if (encryptedFieldsMap != null)
                {
                    var listCollectionOptions = new ListCollectionsOptions() { Filter = $"{{ name : '{collectionNamespace.CollectionName}' }}" };
                    var currentCollectionsInfo = await ListCollectionsAsync(session, listCollectionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var currentCollectionInfo = await currentCollectionsInfo.FirstOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    effectiveEncryptedFields = currentCollectionInfo
                        ?.GetValue("options", defaultValue: null)
                        ?.AsBsonDocument
                        ?.GetValue("encryptedFields", defaultValue: null)
                        ?.ToBsonDocument();
                }
            }

            return effectiveEncryptedFields;
        }

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            var messageEncoderSettings = new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict }
            };

            if (_client is MongoClient mongoClient)
            {
                mongoClient.ConfigureAutoEncryptionMessageEncoderSettings(messageEncoderSettings);
            }

            return messageEncoderSettings;
        }

        private RenderArgs<TDocument> GetRenderArgs<TDocument>(IBsonSerializer<TDocument> documentSerializer)
        {
            var translationOptions = _client.Settings.TranslationOptions;
            return new RenderArgs<TDocument>(documentSerializer, _settings.SerializerRegistry, translationOptions: translationOptions);
        }

        private RenderArgs<TDocument> GetRenderArgs<TDocument>(IBsonSerializer<TDocument> documentSerializer, ExpressionTranslationOptions translationOptions)
        {
            translationOptions = translationOptions.AddMissingOptionsFrom(_client.Settings.TranslationOptions);
            return new RenderArgs<TDocument>(documentSerializer, _settings.SerializerRegistry, translationOptions: translationOptions);
        }
    }
}
