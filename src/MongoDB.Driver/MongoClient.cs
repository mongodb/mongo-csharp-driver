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
using MongoDB.Driver.Core.Bindings;
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
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings)).FrozenCopy();
            _logger = _settings.LoggingSettings?.CreateLogger<LogCategories.Client>();

            _cluster = _settings.ClusterSource.Get(_settings.ToClusterKey());
            _operationExecutor = new OperationExecutor(this);
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

        internal MongoClient(IOperationExecutor operationExecutor, MongoClientSettings settings)
            : this(settings)
        {
            _operationExecutor = operationExecutor;
        }

        // public properties
        /// <inheritdoc/>
        public ICluster Cluster => ThrowIfDisposed(_cluster);

        /// <inheritdoc/>
        public MongoClientSettings Settings => ThrowIfDisposed(_settings);

        // internal properties
        internal IAutoEncryptionLibMongoCryptController LibMongoCryptController => ThrowIfDisposed(_libMongoCryptController);
        internal IOperationExecutor OperationExecutor => ThrowIfDisposed(_operationExecutor);

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
            => UsingImplicitSession(session => BulkWrite(session, models, options, cancellationToken), cancellationToken);

        /// <inheritdoc/>
        public ClientBulkWriteResult BulkWrite(IClientSessionHandle session, IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            var operation = CreateClientBulkWriteOperation(models, options);
            return ExecuteWriteOperation<ClientBulkWriteResult>(session, operation, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<ClientBulkWriteResult> BulkWriteAsync(IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options = null, CancellationToken cancellationToken = default)
            => UsingImplicitSession(session => BulkWriteAsync(session, models, options, cancellationToken), cancellationToken);

        /// <inheritdoc/>
        public Task<ClientBulkWriteResult> BulkWriteAsync(IClientSessionHandle session, IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            var operation = CreateClientBulkWriteOperation(models, options);
            return ExecuteWriteOperationAsync<ClientBulkWriteResult>(session, operation, cancellationToken);
        }

        /// <inheritdoc/>
        public void DropDatabase(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            UsingImplicitSession(session => DropDatabase(session, name, cancellationToken), cancellationToken);
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

                    _settings.ClusterSource.Return(_cluster);
                    _libMongoCryptController?.Dispose();

                    _logger?.LogDebug(_cluster.ClusterId, "MongoClient disposed");
                }

                _disposed = true;
            }
        }

        /// <inheritdoc/>
        public void DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();

            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new DropDatabaseOperation(new DatabaseNamespace(name), messageEncoderSettings)
            {
                WriteConcern = _settings.WriteConcern
            };
            ExecuteWriteOperation(session, operation, cancellationToken);
        }

        /// <inheritdoc/>
        public Task DropDatabaseAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return UsingImplicitSessionAsync(session => DropDatabaseAsync(session, name, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public Task DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            Ensure.IsNotNull(session, nameof(session));
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new DropDatabaseOperation(new DatabaseNamespace(name), messageEncoderSettings)
            {
                WriteConcern = _settings.WriteConcern
            };
            return ExecuteWriteOperationAsync(session, operation, cancellationToken);
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
        public IAsyncCursor<string> ListDatabaseNames(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return ListDatabaseNames(options: null, cancellationToken);
        }

        /// <inheritdoc />
        public IAsyncCursor<string> ListDatabaseNames(
            ListDatabaseNamesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return UsingImplicitSession(session => ListDatabaseNames(session, options, cancellationToken), cancellationToken);
        }

        /// <inheritdoc />
        public IAsyncCursor<string> ListDatabaseNames(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return ListDatabaseNames(session, options: null, cancellationToken);
        }

        /// <inheritdoc />
        public IAsyncCursor<string> ListDatabaseNames(
            IClientSessionHandle session,
            ListDatabaseNamesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            var listDatabasesOptions = CreateListDatabasesOptionsFromListDatabaseNamesOptions(options);
            var databases = ListDatabases(session, listDatabasesOptions, cancellationToken);

            return CreateDatabaseNamesCursor(databases);
        }

        /// <inheritdoc />
        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return ListDatabaseNamesAsync(options: null, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            ListDatabaseNamesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return UsingImplicitSessionAsync(session => ListDatabaseNamesAsync(session, options, cancellationToken), cancellationToken);
        }

        /// <inheritdoc />
        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return ListDatabaseNamesAsync(session, options: null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            IClientSessionHandle session,
            ListDatabaseNamesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            var listDatabasesOptions = CreateListDatabasesOptionsFromListDatabaseNamesOptions(options);
            var databases = await ListDatabasesAsync(session, listDatabasesOptions, cancellationToken).ConfigureAwait(false);

            return CreateDatabaseNamesCursor(databases);
        }

        /// <inheritdoc/>
        public IAsyncCursor<BsonDocument> ListDatabases(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return UsingImplicitSession(session => ListDatabases(session, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncCursor<BsonDocument> ListDatabases(
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return UsingImplicitSession(session => ListDatabases(session, options, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncCursor<BsonDocument> ListDatabases(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return ListDatabases(session, null, cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncCursor<BsonDocument> ListDatabases(
            IClientSessionHandle session,
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            Ensure.IsNotNull(session, nameof(session));
            options = options ?? new ListDatabasesOptions();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var translationOptions = _settings.TranslationOptions;
            var operation = CreateListDatabaseOperation(options, messageEncoderSettings, translationOptions);
            return ExecuteReadOperation(session, operation, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return UsingImplicitSessionAsync(session => ListDatabasesAsync(session, null, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return UsingImplicitSessionAsync(session => ListDatabasesAsync(session, options, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return ListDatabasesAsync(session, null, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            IClientSessionHandle session,
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();

            options = options ?? new ListDatabasesOptions();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var translationOptions = _settings.TranslationOptions;
            var operation = CreateListDatabaseOperation(options, messageEncoderSettings, translationOptions);
            return ExecuteReadOperationAsync(session, operation, cancellationToken);
        }

        /// <summary>
        /// Starts an implicit session.
        /// </summary>
        /// <returns>A session.</returns>
        internal IClientSessionHandle StartImplicitSession(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            return StartImplicitSession();
        }

        /// <summary>
        /// Starts an implicit session.
        /// </summary>
        /// <returns>A Task whose result is a session.</returns>
        internal Task<IClientSessionHandle> StartImplicitSessionAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            return Task.FromResult(StartImplicitSession());
        }

        /// <inheritdoc/>
        public IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return StartSession(options);
        }

        /// <inheritdoc/>
        public Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return Task.FromResult(StartSession(options));
        }

        /// <inheritdoc/>
        public IChangeStreamCursor<TResult> Watch<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return UsingImplicitSession(session => Watch(session, pipeline, options, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public IChangeStreamCursor<TResult> Watch<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            ThrowIfDisposed();

            var translationOptions = _settings.TranslationOptions;
            var operation = CreateChangeStreamOperation(pipeline, options, translationOptions);
            return ExecuteReadOperation(session, operation, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return UsingImplicitSessionAsync(session => WatchAsync(session, pipeline, options, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));

            ThrowIfDisposed();

            var translationOptions = _settings.TranslationOptions;
            var operation = CreateChangeStreamOperation(pipeline, options, translationOptions);
            return ExecuteReadOperationAsync(session, operation, cancellationToken);
        }

        /// <inheritdoc/>
        public IMongoClient WithReadConcern(ReadConcern readConcern)
        {
            Ensure.IsNotNull(readConcern, nameof(readConcern));

            ThrowIfDisposed();

            var newSettings = Settings.Clone();
            newSettings.ReadConcern = readConcern;
            return new MongoClient(_operationExecutor, newSettings);
        }

        /// <inheritdoc/>
        public IMongoClient WithReadPreference(ReadPreference readPreference)
        {
            Ensure.IsNotNull(readPreference, nameof(readPreference));

            ThrowIfDisposed();

            var newSettings = Settings.Clone();
            newSettings.ReadPreference = readPreference;
            return new MongoClient(_operationExecutor, newSettings);
        }

        /// <inheritdoc/>
        public IMongoClient WithWriteConcern(WriteConcern writeConcern)
        {
            Ensure.IsNotNull(writeConcern, nameof(writeConcern));

            ThrowIfDisposed();

            var newSettings = Settings.Clone();
            newSettings.WriteConcern = writeConcern;
            return new MongoClient(_operationExecutor, newSettings);
        }

        // private methods
        private ClientBulkWriteOperation CreateClientBulkWriteOperation(IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options = null)
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
        {
            return new BatchTransformingAsyncCursor<BsonDocument, string>(
                cursor,
                databases => databases.Select(database => database["name"].AsString));
        }

        private ListDatabasesOperation CreateListDatabaseOperation(
            ListDatabasesOptions options,
            MessageEncoderSettings messageEncoderSettings,
            ExpressionTranslationOptions translationOptions)
        {
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
            }

            return listDatabasesOptions;
        }

        private IReadBindingHandle CreateReadBinding(IClientSessionHandle session)
        {
            var readPreference = _settings.ReadPreference;
            if (session.IsInTransaction && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary)
            {
                throw new InvalidOperationException("Read preference in a transaction must be primary.");
            }

            var binding = new ReadPreferenceBinding(_cluster, readPreference, session.WrappedCoreSession.Fork());
            return new ReadBindingHandle(binding);
        }

        private IReadWriteBindingHandle CreateReadWriteBinding(IClientSessionHandle session)
        {
            var binding = new WritableServerBinding(_cluster, session.WrappedCoreSession.Fork());
            return new ReadWriteBindingHandle(binding);
        }

        private ChangeStreamOperation<TResult> CreateChangeStreamOperation<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options,
            ExpressionTranslationOptions translationOptions)
        {
            return ChangeStreamHelper.CreateChangeStreamOperation(
                pipeline,
                options,
                _settings.ReadConcern,
                GetMessageEncoderSettings(),
                _settings.RetryReads,
                translationOptions);
        }

        private TResult ExecuteReadOperation<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = CreateReadBinding(session))
            {
                return _operationExecutor.ExecuteReadOperation(binding, operation, cancellationToken);
            }
        }

        private async Task<TResult> ExecuteReadOperationAsync<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = CreateReadBinding(session))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
            }
        }

        private TResult ExecuteWriteOperation<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = CreateReadWriteBinding(session))
            {
                return _operationExecutor.ExecuteWriteOperation(binding, operation, cancellationToken);
            }
        }

        private async Task<TResult> ExecuteWriteOperationAsync<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = CreateReadWriteBinding(session))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
            }
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

        private IClientSessionHandle StartImplicitSession()
        {
            var options = new ClientSessionOptions { CausalConsistency = false, Snapshot = false };
            ICoreSessionHandle coreSession = _cluster.StartSession(options.ToCore(isImplicit: true));
            return new ClientSessionHandle(this, options, coreSession);
        }

        private IClientSessionHandle StartSession(ClientSessionOptions options)
        {
            if (options != null && options.Snapshot && options.CausalConsistency == true)
            {
                throw new NotSupportedException("Combining both causal consistency and snapshot options is not supported.");
            }

            options = options ?? new ClientSessionOptions();
            var coreSession = _cluster.StartSession(options.ToCore());

            return new ClientSessionHandle(this, options, coreSession);
        }

        private void ThrowIfDisposed() => ThrowIfDisposed(string.Empty);
        private T ThrowIfDisposed<T>(T value) => _disposed ? throw new ObjectDisposedException(GetType().Name) : value;

        private void UsingImplicitSession(Action<IClientSessionHandle> func, CancellationToken cancellationToken)
        {
            using (var session = StartImplicitSession(cancellationToken))
            {
                func(session);
            }
        }

        private TResult UsingImplicitSession<TResult>(Func<IClientSessionHandle, TResult> func, CancellationToken cancellationToken)
        {
            using (var session = StartImplicitSession(cancellationToken))
            {
                return func(session);
            }
        }

        private async Task UsingImplicitSessionAsync(Func<IClientSessionHandle, Task> funcAsync, CancellationToken cancellationToken)
        {
            using (var session = await StartImplicitSessionAsync(cancellationToken).ConfigureAwait(false))
            {
                await funcAsync(session).ConfigureAwait(false);
            }
        }

        private async Task<TResult> UsingImplicitSessionAsync<TResult>(Func<IClientSessionHandle, Task<TResult>> funcAsync, CancellationToken cancellationToken)
        {
            using (var session = await StartImplicitSessionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await funcAsync(session).ConfigureAwait(false);
            }
        }
    }
}
