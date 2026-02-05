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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver
{
    /// <inheritdoc/>
    public sealed class MongoClient : IMongoClient
    {
        // private fields
        private bool _disposed;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IClusterInternal _cluster;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly IAutoEncryptionLibMongoCryptController _libMongoCryptController;
        private readonly Func<IMongoClient, IOperationExecutor> _operationExecutorFactory;
        private readonly IOperationExecutor _operationExecutor;
        private readonly MongoClientSettings _settings;
        private readonly ILogger<LogCategories.Client> _logger;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        public MongoClient()
            : this(new MongoClientSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public MongoClient(MongoClientSettings settings)
            : this(settings, client => new OperationExecutor(client))
        {

        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="url">The URL.</param>
        public MongoClient(MongoUrl url)
            : this(MongoClientSettings.FromUrl(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public MongoClient(string connectionString)
            : this(MongoClientSettings.FromConnectionString(connectionString))
        {
        }

        internal MongoClient(MongoClientSettings settings, Func<IMongoClient, IOperationExecutor> operationExecutorFactory)
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings)).FrozenCopy();
            _operationExecutorFactory = Ensure.IsNotNull(operationExecutorFactory, nameof(operationExecutorFactory));
            _logger = _settings.LoggingSettings?.CreateLogger<LogCategories.Client>();
            _cluster = _settings.ClusterSource.Get(_settings.ToClusterKey());
            _operationExecutor = _operationExecutorFactory(this);

            if (settings.AutoEncryptionOptions != null)
            {
                _libMongoCryptController =
                    MongoClientSettings.Extensions.AutoEncryptionProvider.CreateAutoCryptClientController(this, settings.AutoEncryptionOptions);

                _settings.LoggingSettings?.CreateLogger<LogCategories.Client>()?.LogTrace(
                    StructuredLogTemplateProviders.TopologyId_Message_SharedLibraryVersion,
                    _cluster.ClusterId,
                    "CryptClient created. Configured shared library version: ",
                    _libMongoCryptController.CryptSharedLibraryVersion() ?? "None");
            }
        }

        // public properties
        /// <inheritdoc/>
        public ICluster Cluster => ThrowIfDisposed(_cluster);

        /// <inheritdoc/>
        public MongoClientSettings Settings => ThrowIfDisposed(_settings);

        // internal properties
        internal IAutoEncryptionLibMongoCryptController LibMongoCryptController => ThrowIfDisposed(_libMongoCryptController);

        // internal methods
        internal void ConfigureAutoEncryptionMessageEncoderSettings(MessageEncoderSettings messageEncoderSettings)
        {
            ThrowIfDisposed();
            var autoEncryptionOptions = _settings.AutoEncryptionOptions;
            if (autoEncryptionOptions != null)
            {
                if (!autoEncryptionOptions.BypassAutoEncryption)
                {
                    messageEncoderSettings.Add(MessageEncoderSettingsName.BinaryDocumentFieldEncryptor, _libMongoCryptController);
                }
                messageEncoderSettings.Add(MessageEncoderSettingsName.BinaryDocumentFieldDecryptor, _libMongoCryptController);
            }
        }

        // public methods
        /// <inheritdoc/>
        public ClientBulkWriteResult BulkWrite(IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return BulkWrite(session, models, options, cancellationToken);
        }

        /// <inheritdoc/>
        public ClientBulkWriteResult BulkWrite(IClientSessionHandle session, IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();
            var operation = CreateClientBulkWriteOperation(models, options);
            return ExecuteWriteOperation<ClientBulkWriteResult>(session, operation, options?.Timeout, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ClientBulkWriteResult> BulkWriteAsync(IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return await BulkWriteAsync(session, models, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<ClientBulkWriteResult> BulkWriteAsync(IClientSessionHandle session, IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();
            var operation = CreateClientBulkWriteOperation(models, options);
            return ExecuteWriteOperationAsync<ClientBulkWriteResult>(session, operation, options?.Timeout, cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        public void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logger?.LogDebug(_cluster.ClusterId, "MongoClient disposing");

                    _operationExecutor.Dispose();
                    _settings.ClusterSource.Return(_cluster);
                    _libMongoCryptController?.Dispose();

                    _logger?.LogDebug(_cluster.ClusterId, "MongoClient disposed");
                }

                _disposed = true;
            }
        }

        /// <inheritdoc/>
        public void DropDatabase(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            DropDatabase(session, name, cancellationToken);
        }

        /// <inheritdoc/>
        public void DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();
            var operation = CreateDropDatabaseOperation(name);
            // TODO: CSOT: find a way to add timeout parameter to the interface method
            ExecuteWriteOperation(session, operation, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DropDatabaseAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            await DropDatabaseAsync(session, name, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();
            var opertion = CreateDropDatabaseOperation(name);
            // TODO: CSOT: find a way to add timeout parameter to the interface method
            return ExecuteWriteOperationAsync(session, opertion, null, cancellationToken);
        }

        /// <inheritdoc/>
        public IMongoDatabase GetDatabase(string name, MongoDatabaseSettings settings = null)
        {
            ThrowIfDisposed();

            settings = settings == null ?
                new MongoDatabaseSettings() :
                settings.Clone();

            settings.ApplyDefaultValues(_settings);

            return new MongoDatabase(this, new DatabaseNamespace(name), settings, _cluster, _operationExecutor);
        }

        /// <inheritdoc />
        public IAsyncCursor<string> ListDatabaseNames(CancellationToken cancellationToken = default)
            => ListDatabaseNames(options: null, cancellationToken);

        /// <inheritdoc />
        public IAsyncCursor<string> ListDatabaseNames(
            ListDatabaseNamesOptions options,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return ListDatabaseNames(session, options, cancellationToken);
        }

        /// <inheritdoc />
        public IAsyncCursor<string> ListDatabaseNames(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default)
            => ListDatabaseNames(session, options: null, cancellationToken);

        /// <inheritdoc />
        public IAsyncCursor<string> ListDatabaseNames(
            IClientSessionHandle session,
            ListDatabaseNamesOptions options,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var listDatabasesOptions = CreateListDatabasesOptionsFromListDatabaseNamesOptions(options);
            var databases = ListDatabases(session, listDatabasesOptions, cancellationToken);
            return CreateDatabaseNamesCursor(databases);
        }

        /// <inheritdoc />
        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(CancellationToken cancellationToken = default)
            => ListDatabaseNamesAsync(options: null, cancellationToken);

        /// <inheritdoc />
        public async Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            ListDatabaseNamesOptions options,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return await ListDatabaseNamesAsync(session, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default)
            => ListDatabaseNamesAsync(session, options: null, cancellationToken);

        /// <inheritdoc />
        public async Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            IClientSessionHandle session,
            ListDatabaseNamesOptions options,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var listDatabasesOptions = CreateListDatabasesOptionsFromListDatabaseNamesOptions(options);
            var databases = await ListDatabasesAsync(session, listDatabasesOptions, cancellationToken).ConfigureAwait(false);
            return CreateDatabaseNamesCursor(databases);
        }

        /// <inheritdoc/>
        public IAsyncCursor<BsonDocument> ListDatabases(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return ListDatabases(session, cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncCursor<BsonDocument> ListDatabases(
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return ListDatabases(session, options, cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncCursor<BsonDocument> ListDatabases(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default)
            => ListDatabases(session, null, cancellationToken);

        /// <inheritdoc/>
        public IAsyncCursor<BsonDocument> ListDatabases(
            IClientSessionHandle session,
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            Ensure.IsNotNull(session, nameof(session));
            var operation = CreateListDatabasesOperation(options);
            return ExecuteReadOperation(session, operation, options?.Timeout, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return await ListDatabasesAsync(session, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return await ListDatabasesAsync(session, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default)
            => ListDatabasesAsync(session, null, cancellationToken);

        /// <inheritdoc/>
        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            IClientSessionHandle session,
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();
            var operation = CreateListDatabasesOperation(options);
            return ExecuteReadOperationAsync(session, operation, options?.Timeout, cancellationToken);
        }

        /// <inheritdoc/>
        public IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return StartSession(options);
        }

        /// <inheritdoc/>
        public Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions options = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return Task.FromResult(StartSession(options));
        }

        /// <inheritdoc/>
        public IChangeStreamCursor<TResult> Watch<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return Watch(session, pipeline, options, cancellationToken);
        }

        /// <inheritdoc/>
        public IChangeStreamCursor<TResult> Watch<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            ThrowIfDisposed();
            var operation = CreateChangeStreamOperation(pipeline, options);
            return ExecuteReadOperation(session, operation, options?.Timeout, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            using var session = _operationExecutor.StartImplicitSession();
            return await WatchAsync(session, pipeline, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            ThrowIfDisposed();
            var operation = CreateChangeStreamOperation(pipeline, options);
            return ExecuteReadOperationAsync(session, operation, options?.Timeout, cancellationToken);
        }

        /// <inheritdoc/>
        public IMongoClient WithReadConcern(ReadConcern readConcern)
        {
            Ensure.IsNotNull(readConcern, nameof(readConcern));
            ThrowIfDisposed();

            var newSettings = Settings.Clone();
            newSettings.ReadConcern = readConcern;
            return new MongoClient(newSettings, _operationExecutorFactory);
        }

        /// <inheritdoc/>
        public IMongoClient WithReadPreference(ReadPreference readPreference)
        {
            Ensure.IsNotNull(readPreference, nameof(readPreference));
            ThrowIfDisposed();

            var newSettings = Settings.Clone();
            newSettings.ReadPreference = readPreference;
            return new MongoClient(newSettings, _operationExecutorFactory);
        }

        /// <inheritdoc/>
        public IMongoClient WithWriteConcern(WriteConcern writeConcern)
        {
            Ensure.IsNotNull(writeConcern, nameof(writeConcern));
            ThrowIfDisposed();

            var newSettings = Settings.Clone();
            newSettings.WriteConcern = writeConcern;
            return new MongoClient(newSettings, _operationExecutorFactory);
        }

        // private methods
        private ClientBulkWriteOperation CreateClientBulkWriteOperation(IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options)
        {
            if (_settings.AutoEncryptionOptions != null)
            {
                throw new NotSupportedException("BulkWrite does not currently support automatic encryption.");
            }

            if (options?.WriteConcern?.IsAcknowledged == false && options?.IsOrdered == true)
            {
                throw new NotSupportedException("Cannot request unacknowledged write concern and ordered writes.");
            }

            if (options?.WriteConcern?.IsAcknowledged == false && options?.VerboseResult == true)
            {
                throw new NotSupportedException("Cannot request unacknowledged write concern and verbose results");
            }

            var messageEncoderSettings = GetMessageEncoderSettings();
            var renderArgs = GetRenderArgs();
            var operation = new ClientBulkWriteOperation(models, options, messageEncoderSettings, renderArgs)
            {
                RetryRequested = _settings.RetryWrites,
            };
            if (options?.WriteConcern == null)
            {
                operation.WriteConcern = _settings.WriteConcern;
            }

            return operation;
        }

        private IAsyncCursor<string> CreateDatabaseNamesCursor(IAsyncCursor<BsonDocument> cursor)
            => new BatchTransformingAsyncCursor<BsonDocument, string>(
                cursor,
                databases => databases.Select(database => database["name"].AsString));

        private DropDatabaseOperation CreateDropDatabaseOperation(string name)
            => new(new DatabaseNamespace(name), GetMessageEncoderSettings())
            {
                WriteConcern = _settings.WriteConcern
            };

        private ListDatabasesOperation CreateListDatabasesOperation(ListDatabasesOptions options)
        {
            options ??= new ListDatabasesOptions();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var translationOptions = _settings.TranslationOptions;

            return new ListDatabasesOperation(messageEncoderSettings)
            {
                AuthorizedDatabases = options.AuthorizedDatabases,
                Comment = options.Comment,
                Filter = options.Filter?.Render(new(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry, translationOptions: translationOptions)),
                NameOnly = options.NameOnly,
                RetryRequested = _settings.RetryReads
            };
        }

        private ListDatabasesOptions CreateListDatabasesOptionsFromListDatabaseNamesOptions(ListDatabaseNamesOptions options)
        {
            var listDatabasesOptions = new ListDatabasesOptions { NameOnly = true };
            if (options != null)
            {
                listDatabasesOptions.AuthorizedDatabases = options.AuthorizedDatabases;
                listDatabasesOptions.Filter = options.Filter;
                listDatabasesOptions.Comment = options.Comment;
                listDatabasesOptions.Timeout = options.Timeout;
            }

            return listDatabasesOptions;
        }

        private ChangeStreamOperation<TResult> CreateChangeStreamOperation<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options)
            => ChangeStreamHelper.CreateChangeStreamOperation(
                pipeline,
                options,
                _settings.ReadConcern,
                GetMessageEncoderSettings(),
                _settings.RetryReads,
                _settings.TranslationOptions);

        private OperationContext CreateOperationContext(IClientSessionHandle session, TimeSpan? timeout, string operationName, CancellationToken cancellationToken)
        {
            var operationContext = session.WrappedCoreSession.CurrentTransaction?.OperationContext;
            if (operationContext != null && timeout != null)
            {
                throw new InvalidOperationException("Cannot specify per operation timeout inside transaction.");
            }

            var baseContext = operationContext?.Fork() ?? new OperationContext(timeout ?? _settings.Timeout, cancellationToken);

            if (operationName != null)
            {
                var tracingOptions = _settings.TracingOptions;
                var isTracingEnabled = tracingOptions == null || !tracingOptions.Disabled;
                var contextWithMetadata = baseContext.WithOperationMetadata(operationName, "admin", null, isTracingEnabled);
                baseContext.Dispose();
                return contextWithMetadata;
            }

            return baseContext;
        }

        private TResult ExecuteReadOperation<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var readPreference = session.GetEffectiveReadPreference(_settings.ReadPreference);
            using var operationContext = CreateOperationContext(session, timeout, operation.OperationName, cancellationToken);
            return _operationExecutor.ExecuteReadOperation(operationContext, session, operation, readPreference, false);
        }

        private async Task<TResult> ExecuteReadOperationAsync<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var readPreference = session.GetEffectiveReadPreference(_settings.ReadPreference);
            using var operationContext = CreateOperationContext(session, timeout, operation.OperationName, cancellationToken);
            return await _operationExecutor.ExecuteReadOperationAsync(operationContext, session, operation, readPreference, false).ConfigureAwait(false);
        }

        private TResult ExecuteWriteOperation<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            using var operationContext = CreateOperationContext(session, timeout, operation.OperationName, cancellationToken);
            return _operationExecutor.ExecuteWriteOperation(operationContext, session, operation, false);
        }

        private async Task<TResult> ExecuteWriteOperationAsync<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            using var operationContext = CreateOperationContext(session, timeout, operation.OperationName, cancellationToken);
            return await _operationExecutor.ExecuteWriteOperationAsync(operationContext, session, operation, false).ConfigureAwait(false);
        }

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            var messageEncoderSettings = new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict }
            };

            ConfigureAutoEncryptionMessageEncoderSettings(messageEncoderSettings);

            return messageEncoderSettings;
        }

        private RenderArgs<BsonDocument> GetRenderArgs()
        {
            var translationOptions = Settings.TranslationOptions;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            return new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, serializerRegistry, translationOptions: translationOptions);
        }

        private IClientSessionHandle StartSession(ClientSessionOptions options)
        {
            if (options != null && options.Snapshot && options.CausalConsistency == true)
            {
                throw new NotSupportedException("Combining both causal consistency and snapshot options is not supported.");
            }

            options ??= new ClientSessionOptions();
            if (_settings.Timeout.HasValue && options.DefaultTransactionOptions?.Timeout == null)
            {
                options.DefaultTransactionOptions = new TransactionOptions(
                    _settings.Timeout,
                    options.DefaultTransactionOptions?.ReadConcern,
                    options.DefaultTransactionOptions?.ReadPreference,
                    options.DefaultTransactionOptions?.WriteConcern,
                    options.DefaultTransactionOptions?.MaxCommitTime);
            }

            var coreSession = _cluster.StartSession(options.ToCore());

            return new ClientSessionHandle(this, options, coreSession);
        }

        private void ThrowIfDisposed() => ThrowIfDisposed(string.Empty);
        private T ThrowIfDisposed<T>(T value) => _disposed ? throw new ObjectDisposedException(GetType().Name) : value;
    }
}
