/* Copyright 2020-present MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.TestHelpers.Core;
using MongoDB.Driver.Tests.Specifications.client_side_encryption;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedEntityMap : IDisposable
    {
        #region static
        public static UnifiedEntityMap Create(Dictionary<string, IEventFormatter> eventFormatters, LoggingSettings loggingSettings, bool async, BsonDocument lastKnownClusterTime)
            => new(eventFormatters, loggingSettings, async, lastKnownClusterTime);

        #endregion

        // private fields
        private readonly bool _async;
        private readonly Dictionary<string, IEventFormatter> _eventFormatters;
        private readonly LoggingSettings _loggingSettings;
        private readonly BsonDocument _lastKnownClusterTime;

        private readonly Dictionary<string, IGridFSBucket> _buckets = new();
        private readonly Dictionary<string, IEnumerator<ChangeStreamDocument<BsonDocument>>> _changeStreams = new();
        private readonly Dictionary<string, IMongoClient> _clients = new();
        private readonly Dictionary<string, ClientEncryption> _clientEncryptions = new();
        private readonly Dictionary<string, EventCapturer> _clientEventCapturers = new();
        private readonly Dictionary<string, SpanCapturer> _spanCapturers = new();
        private readonly Dictionary<string, ClusterId> _clientIdToClusterId = new();
        private readonly Dictionary<string, IMongoCollection<BsonDocument>> _collections = new();
        private readonly Dictionary<string, IEnumerator<BsonDocument>> _cursors = new();
        private readonly Dictionary<string, IMongoDatabase> _databases = new();
        private bool _disposed;
        private readonly List<IDisposable> _disposables = new();
        private readonly Dictionary<string, BsonArray> _errorDocuments = new();
        private readonly Dictionary<string, BsonArray> _failureDocuments = new();
        private readonly Dictionary<string, long> _iterationCounts = new();
        private readonly Dictionary<string, Dictionary<string, LogLevel>> _loggingComponents = new();
        private readonly Dictionary<string, BsonValue> _results = new();
        private readonly Dictionary<string, IClientSessionHandle> _sessions = new();
        private readonly Dictionary<string, BsonDocument> _sessionIds = new();
        private readonly Dictionary<string, long> _successCounts = new();
        private readonly Dictionary<string, Task> _threads = new();
        private readonly Dictionary<string, ClusterDescription> _topologyDescriptions = new();

        private UnifiedEntityMap(
            Dictionary<string, IEventFormatter> eventFormatters,
            LoggingSettings loggingSettings,
            bool async,
            BsonDocument lastKnownClusterTime)
        {
            _eventFormatters = eventFormatters ?? new();
            _loggingSettings = loggingSettings;
            _async = async;
            _lastKnownClusterTime = lastKnownClusterTime;
        }

        // public properties
        public bool Async => _async;

        public Dictionary<string, IGridFSBucket> Buckets
        {
            get
            {
                ThrowIfDisposed();
                return _buckets;
            }
        }

        public Dictionary<string, IEnumerator<ChangeStreamDocument<BsonDocument>>> ChangeStreams
        {
            get
            {
                ThrowIfDisposed();
                return _changeStreams;
            }
        }

        public Dictionary<string, IMongoClient> Clients
        {
            get
            {
                ThrowIfDisposed();
                return _clients;
            }
        }

        public Dictionary<string, ClientEncryption> ClientEncryptions
        {
            get
            {
                ThrowIfDisposed();
                return _clientEncryptions;
            }
        }

        public Dictionary<string, ClusterId> ClientIdToClusterId
        {
            get
            {
                ThrowIfDisposed();
                return _clientIdToClusterId;
            }
        }

        public Dictionary<string, IMongoCollection<BsonDocument>> Collections
        {
            get
            {
                ThrowIfDisposed();
                return _collections;
            }
        }

        public Dictionary<string, IEnumerator<BsonDocument>> Cursors
        {
            get
            {
                ThrowIfDisposed();
                return _cursors;
            }
        }
        public Dictionary<string, IMongoDatabase> Databases
        {
            get
            {
                ThrowIfDisposed();
                return _databases;
            }
        }

        public Dictionary<string, BsonArray> ErrorDocuments
        {
            get
            {
                ThrowIfDisposed();
                return _errorDocuments;
            }
        }
        public Dictionary<string, EventCapturer> EventCapturers
        {
            get
            {
                ThrowIfDisposed();
                return _clientEventCapturers;
            }
        }

        public Dictionary<string, SpanCapturer> SpanCapturers
        {
            get
            {
                ThrowIfDisposed();
                return _spanCapturers;
            }
        }

        public Dictionary<string, BsonArray> FailureDocuments
        {
            get
            {
                ThrowIfDisposed();
                return _failureDocuments;
            }
        }

        public Dictionary<string, long> IterationCounts
        {
            get
            {
                ThrowIfDisposed();
                return _iterationCounts;
            }
        }

        public Dictionary<string, Dictionary<string, LogLevel>> LoggingComponents
        {
            get
            {
                ThrowIfDisposed();
                return _loggingComponents;
            }
        }

        public Dictionary<string, BsonValue> Results
        {
            get
            {
                ThrowIfDisposed();
                return _results;
            }
        }

        public Dictionary<string, IClientSessionHandle> Sessions
        {
            get
            {
                ThrowIfDisposed();
                return _sessions;
            }
        }

        /// <summary>
        /// This needs to have access to session id when session is disposed.
        /// </summary>
        public Dictionary<string, BsonDocument> SessionIds
        {
            get
            {
                ThrowIfDisposed();
                return _sessionIds;
            }
        }

        public Dictionary<string, long> SuccessCounts
        {
            get
            {
                ThrowIfDisposed();
                return _successCounts;
            }
        }

        public Dictionary<string, Task> Threads
        {
            get
            {
                ThrowIfDisposed();
                return _threads;
            }
        }

        public Dictionary<string, ClusterDescription> TopologyDescription
        {
            get
            {
                ThrowIfDisposed();
                return _topologyDescriptions;
            }
        }

        // public methods
        public void AdjustSessionsClusterTime(BsonDocument clusterTime)
        {
            if (clusterTime == null)
            {
                return;
            }

            foreach (var session in _sessions.Values)
            {
                session.WrappedCoreSession.AdvanceClusterTime(clusterTime);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                for (var i = _disposables.Count - 1; i >= 0; i--)
                {
                    _disposables[i].Dispose();
                }

                var toDisposeCollection = SelectDisposables(_changeStreams?.Values, _sessions?.Values, _clients?.Values, _clientEncryptions?.Values, _spanCapturers?.Values);
                foreach (var toDispose in toDisposeCollection)
                {
                    toDispose.Dispose();
                }

                _disposed = true;
            }

            IDisposable[] SelectDisposables(params IEnumerable[] items) => items.SelectMany(i => i.OfType<IDisposable>()).ToArray();
        }

        public void AddRange(BsonArray entities)
            => CreateEntities(entities);

        public void RegisterForDispose(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        // private methods
        private AutoEncryptionOptions ConfigureAutoEncryptionOptions(BsonDocument autoEncryptOpts)
        {
            var extraOptions = new Dictionary<string, object>();
            EncryptionTestHelper.ConfigureDefaultExtraOptions(extraOptions);

            var bypassAutoEncryption = false;
            bool? bypassQueryAnalysis = null;
            Optional<IReadOnlyDictionary<string, BsonDocument>> encryptedFieldsMap = null;
            CollectionNamespace keyVaultNamespace = null;
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders = null;
            Optional<IReadOnlyDictionary<string, BsonDocument>> schemaMap = null;
            Optional<IReadOnlyDictionary<string, SslSettings>> tlsOptions = null;

            foreach (var option in autoEncryptOpts.Elements)
            {
                switch (option.Name)
                {
                    case "bypassAutoEncryption":
                        bypassAutoEncryption = option.Value.AsBoolean;
                        break;
                    case "bypassQueryAnalysis":
                        bypassQueryAnalysis = option.Value.AsBoolean;
                        break;
                    case "encryptedFieldsMap":
                        var encryptedFieldsMapDocument = option.Value.AsBsonDocument;
                        encryptedFieldsMap = encryptedFieldsMapDocument.Elements.ToDictionary(e => e.Name, e => e.Value.AsBsonDocument);
                        break;
                    case "extraOptions":
                        ParseExtraOptions(option.Value.AsBsonDocument, extraOptions);
                        break;
                    case "keyVaultNamespace":
                        keyVaultNamespace = CollectionNamespace.FromFullName(option.Value.AsString);
                        break;
                    case "kmsProviders":
                        kmsProviders = EncryptionTestHelper.ParseKmsProviders(option.Value.AsBsonDocument);
                        tlsOptions = EncryptionTestHelper.CreateTlsOptionsIfAllowed(kmsProviders, allowClientCertificateFunc: (kms) => kms.StartsWith("kmip"));
                        break;
                    case "schemaMap":
                        var schemaMapDocument = option.Value.AsBsonDocument;
                        schemaMap = schemaMapDocument.Elements.ToDictionary(e => e.Name, e => e.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Invalid autoEncryption option argument name {option.Name}.");
                }
            }

            var autoEncryptionOptions = new AutoEncryptionOptions(
                keyVaultNamespace,
                kmsProviders,
                bypassAutoEncryption,
                extraOptions,
                bypassQueryAnalysis: bypassQueryAnalysis,
                encryptedFieldsMap: encryptedFieldsMap,
                schemaMap: schemaMap,
                tlsOptions: tlsOptions);

            return autoEncryptionOptions;
        }

        private void CreateEntities(BsonArray entitiesArray)
        {
            if (entitiesArray != null)
            {
                foreach (var entityItem in entitiesArray)
                {
                    if (entityItem.AsBsonDocument.ElementCount != 1)
                    {
                        throw new FormatException("Entity item should contain single element.");
                    }

                    var entityType = entityItem.AsBsonDocument.GetElement(0).Name;
                    var entity = entityItem[0].AsBsonDocument;
                    var id = entity["id"].AsString;
                    switch (entityType)
                    {
                        case "bucket":
                            EnsureIsNotHandled(_buckets, id);
                            var bucket = CreateBucket(entity, _databases);
                            _buckets.Add(id, bucket);
                            break;
                        case "client":
                            EnsureIsNotHandled(_clients, id);
                            var (client, eventCapturers, clientLoggingComponents, clientSpanCapturer) = CreateClient(entity, _async);
                            _clients.Add(id, client);
                            _clientIdToClusterId.Add(id, client.Cluster.ClusterId);
                            foreach (var createdEventCapturer in eventCapturers)
                            {
                                _clientEventCapturers.Add(createdEventCapturer.Key, createdEventCapturer.Value);
                            }

                            _loggingComponents.Add(id, clientLoggingComponents);
                            if (clientSpanCapturer != null)
                            {
                                _spanCapturers.Add(id, clientSpanCapturer);
                            }
                            break;
                        case "clientEncryption":
                            {
                                EnsureIsNotHandled(_clientEncryptions, id);
                                var clientEncryption = CreateClientEncryption(_clients, entity);
                                _clientEncryptions.Add(id, clientEncryption);
                            }
                            break;
                        case "collection":
                            EnsureIsNotHandled(_collections, id);
                            var collection = CreateCollection(entity, _databases);
                            _collections.Add(id, collection);
                            break;
                        case "database":
                            EnsureIsNotHandled(_databases, id);
                            var database = CreateDatabase(entity, _clients);
                            _databases.Add(id, database);
                            break;
                        case "session":
                            EnsureIsNotHandled(_sessions, id);
                            var session = CreateSession(entity, _clients);
                            var sessionId = session.WrappedCoreSession.Id;
                            _sessions.Add(id, session);
                            _sessionIds.Add(id, sessionId);
                            break;
                        case "thread":
                            EnsureIsNotHandled(_threads, id);
                            _threads.Add(id, null);
                            break;
                        default:
                            throw new FormatException($"Invalid entity type: '{entityType}'.");
                    }
                }
            }

            void EnsureIsNotHandled<TEntity>(IDictionary<string, TEntity> dictionary, string key)
            {
                if (dictionary.ContainsKey(key))
                {
                    throw new Exception($"{typeof(TEntity).Name} entity with id '{key}' already exists.");
                }
            }
        }

        private IGridFSBucket CreateBucket(BsonDocument entity, Dictionary<string, IMongoDatabase> databases)
        {
            IMongoDatabase database = null;

            foreach (var element in entity)
            {
                switch (element.Name)
                {
                    case "id":
                        // handled on higher level
                        break;
                    case "database":
                        var databaseId = element.Value.AsString;
                        database = databases[databaseId];
                        break;
                    default:
                        throw new FormatException($"Invalid bucket argument name: '{element.Name}'.");
                }
            }

            return new GridFSBucket(database);
        }

        private (IMongoClient Client, Dictionary<string, EventCapturer> ClientEventCapturers, Dictionary<string, LogLevel> LoggingComponents, SpanCapturer SpanCapturer) CreateClient(BsonDocument entity, bool async)
        {
            string appName = null;
            string authMechanism = null;
            var authMechanismProperties = new Dictionary<string, object>();
            AutoEncryptionOptions autoEncryptionOptions = null;
            var clientEventCapturers = new Dictionary<string, EventCapturer>();
            Dictionary<string, LogLevel> loggingComponents = null;
            string clientId = null;
            var commandNamesToSkipInEvents = new List<string>();
            SpanCapturer spanCapturer = null;
            TracingOptions tracingOptions = null;
            TimeSpan? connectTimeout = null;
            List<(string Key, IEnumerable<string> Events, List<string> CommandNotToCapture)> eventTypesToCapture = new();
            TimeSpan? heartbeatFrequency = null;
            bool? loadBalanced = null;
            int? maxConnecting = null;
            TimeSpan? maxIdleTime = null;
            int? maxPoolSize = null;
            int? minPoolSize = null;
            bool? observeSensitiveCommands = null;
            var readConcern = ReadConcern.Default;
            ReadPreference readPreference = null;
            var retryReads = true;
            var retryWrites = true;
            ServerMonitoringMode? serverMonitoringMode = null;
            TimeSpan? serverSelectionTimeout = null;
            int? waitQueueSize = null;
            TimeSpan? socketTimeout = null;
            TimeSpan? timeout = null;
            var useMultipleShardRouters = false;
            TimeSpan? waitQueueTimeout = null;
            var writeConcern = WriteConcern.Acknowledged;
            var serverApi = CoreTestConfiguration.ServerApi;
            TimeSpan? wTimeout = null;
            TimeSpan? awaitMinPoolSizeTimeout = null;

            foreach (var element in entity)
            {
                switch (element.Name)
                {
                    case "id":
                        clientId = element.Value.AsString;
                        break;
                    case "autoEncryptOpts":
                        autoEncryptionOptions = ConfigureAutoEncryptionOptions(element.Value.AsBsonDocument);
                        break;
                    case "awaitMinPoolSizeMS":
                        awaitMinPoolSizeTimeout = TimeSpan.FromMilliseconds(element.Value.AsInt32);
                        break;
                    case "uriOptions":
                        foreach (var option in element.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "appname":
                                case "appName":
                                    appName = option.Value.AsString;
                                    break;
                                case "authMechanism":
                                    authMechanism = option.Value.AsString;
                                    break;
                                case "authMechanismProperties":
                                    foreach (var property in option.Value.AsBsonDocument)
                                    {
                                        if (string.Equals(property.Name, "$$placeholder"))
                                        {
                                            var environment = Environment.GetEnvironmentVariable("OIDC_ENV");
                                            authMechanismProperties.Add(OidcConfiguration.EnvironmentMechanismPropertyName, environment);
                                            switch (environment)
                                            {
                                                case "azure":
                                                case "gcp":
                                                    authMechanismProperties.Add(OidcConfiguration.TokenResourceMechanismPropertyName, Environment.GetEnvironmentVariable("TOKEN_RESOURCE"));
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            authMechanismProperties.Add(property.Name, property.Value.AsString);
                                        }
                                    }

                                    break;
                                case "heartbeatFrequencyMS":
                                    heartbeatFrequency = TimeSpan.FromMilliseconds(option.Value.AsInt32);
                                    break;
                                case "connectTimeoutMS":
                                    connectTimeout = TimeSpan.FromMilliseconds(option.Value.AsInt32);
                                    break;
                                case "loadBalanced":
                                    loadBalanced = option.Value.ToBoolean();
                                    break;
                                case "maxConnecting":
                                    maxConnecting = option.Value.ToInt32();
                                    break;
                                case "maxIdleTimeMS":
                                    maxIdleTime = TimeSpan.FromMilliseconds(option.Value.ToInt32());
                                    break;
                                case "maxPoolSize":
                                    maxPoolSize = option.Value.ToInt32();
                                    break;
                                case "minPoolSize":
                                    minPoolSize = option.Value.ToInt32();
                                    break;
                                case "retryWrites":
                                    retryWrites = option.Value.AsBoolean;
                                    break;
                                case "retryReads":
                                    retryReads = option.Value.AsBoolean;
                                    break;
                                case "readConcernLevel":
                                    var levelValue = option.Value.AsString;
                                    var level = (ReadConcernLevel)Enum.Parse(typeof(ReadConcernLevel), levelValue, true);
                                    readConcern = new ReadConcern(level);
                                    break;
                                case "readPreference":
                                    if (option.Value.IsBsonDocument)
                                    {
                                        readPreference = ReadPreference.FromBsonDocument(option.Value.AsBsonDocument);
                                    }
                                    else
                                    {
                                        readPreference = option.Value.AsString switch
                                        {
                                            "primary" => ReadPreference.Primary,
                                            "primaryPreferred" => ReadPreference.PrimaryPreferred,
                                            "secondary" => ReadPreference.Secondary,
                                            "secondaryPreferred" => ReadPreference.SecondaryPreferred,
                                            "nearest" => ReadPreference.Nearest,
                                            _ => throw new FormatException($"Invalid read preference type: '{option.Value.AsString}'.")
                                        };
                                    }

                                    break;
                                case "serverMonitoringMode":
                                    serverMonitoringMode = (ServerMonitoringMode)Enum.Parse(typeof(ServerMonitoringMode), option.Value.AsString, true);
                                    break;
                                case "serverSelectionTimeoutMS":
                                    serverSelectionTimeout = TimeSpan.FromMilliseconds(option.Value.AsInt32);
                                    break;
                                case "socketTimeoutMS":
                                    socketTimeout = TimeSpan.FromMilliseconds(option.Value.AsInt32);
                                    break;
                                case "timeoutMS":
                                    timeout = ParseTimeout(option.Value);
                                    break;
                                case "w":
                                    writeConcern = new WriteConcern(WriteConcern.WValue.Parse(option.Value.ToString()));
                                    break;
                                case "waitQueueSize":
                                    waitQueueSize = option.Value.ToInt32();
                                    break;
                                case "waitQueueTimeoutMS":
                                    waitQueueTimeout = TimeSpan.FromMilliseconds(option.Value.ToInt32());
                                    break;
                                case "wTimeoutMS":
                                    wTimeout = TimeSpan.FromMilliseconds(option.Value.ToInt32());
                                    break;
                                default:
                                    throw new FormatException($"Invalid client uriOption argument name: '{option.Name}'.");
                            }
                        }

                        break;
                    case "useMultipleMongoses":
                        useMultipleShardRouters = element.Value.AsBoolean;
                        RequireServer.Check().MultipleMongosesIfSharded(required: useMultipleShardRouters);
                        break;
                    case "observeLogMessages":
                        loggingComponents = element.Value.AsBsonDocument
                            .ToDictionary(
                                pair => UnifiedLogHelper.ParseCategory(pair.Name),
                                pair => UnifiedLogHelper.ParseLogLevel(pair.Value.AsString));
                        break;
                    case "observeEvents":
                        var observeEvents = element.Value.AsBsonArray.Select(x => x.AsString);
                        eventTypesToCapture.Add(
                            (Key: Ensure.IsNotNull(clientId, nameof(clientId)),
                                Events: observeEvents,
                                CommandNotToCapture: commandNamesToSkipInEvents));
                        break;
                    case "observeSensitiveCommands":
                        observeSensitiveCommands = element.Value.AsBoolean;
                        break;
                    case "observeTracingMessages":
                        spanCapturer = new SpanCapturer();
                        tracingOptions = new TracingOptions();
                        var tracingConfig = element.Value.AsBsonDocument;
                        if (tracingConfig.TryGetValue("enableCommandPayload", out var enableCommandPayload) && enableCommandPayload.ToBoolean())
                        {
                            tracingOptions.QueryTextMaxLength = 1024; // Default value when enabled
                        }
                        break;
                    case "ignoreCommandMonitoringEvents":
                        commandNamesToSkipInEvents.AddRange(element.Value.AsBsonArray.Select(x => x.AsString));
                        break;
                    case "serverApi":
                        ServerApiVersion serverApiVersion = null;
                        bool? serverApiStrict = null;
                        bool? serverApiDeprecationErrors = null;
                        foreach (var option in element.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "version":
                                    var serverApiVersionString = option.Value.AsString;
                                    switch (serverApiVersionString)
                                    {
                                        case "1":
                                            serverApiVersion = ServerApiVersion.V1;
                                            break;
                                        default:
                                            throw new FormatException($"Invalid serverApi version: '{serverApiVersionString}'.");
                                    }

                                    break;
                                case "strict":
                                    serverApiStrict = option.Value.AsBoolean;
                                    break;
                                case "deprecationErrors":
                                    serverApiDeprecationErrors = option.Value.AsBoolean;
                                    break;
                                default:
                                    throw new FormatException($"Invalid client serverApi argument name: '{option.Name}'.");
                            }
                        }

                        if (serverApiVersion != null)
                        {
                            serverApi = new ServerApi(serverApiVersion, serverApiStrict, serverApiDeprecationErrors);
                        }

                        break;
                    case "storeEventsAsEntities":
                        var eventsBatches = element.Value.AsBsonArray;
                        foreach (var batch in eventsBatches.Cast<BsonDocument>())
                        {
                            var id = batch["id"].AsString;
                            var events = batch["events"].AsBsonArray.Select(e => e.AsString);
                            eventTypesToCapture.Add((id, events, CommandNotToCapture: null));
                        }

                        break;
                    default:
                        throw new FormatException($"Invalid client argument name: '{element.Name}'.");
                }
            }

            if (wTimeout.HasValue)
            {
                writeConcern = writeConcern.With(wTimeout: wTimeout);
            }

            // Regardless of whether events are observed, we still need to track some info about the pool in order to implement
            // the assertNumberConnectionsCheckedOut operation
            if (eventTypesToCapture.Count == 0)
            {
                eventTypesToCapture.Add(
                    (Key: Ensure.IsNotNull(clientId, nameof(clientId)),
                        Events: new[] { "connectionCheckedInEvent", "connectionCheckedOutEvent" },
                        CommandNotToCapture: commandNamesToSkipInEvents));
            }

            var defaultCommandNamesToSkip = new List<string>
            {
                "configureFailPoint",
                "getLastError",
                OppressiveLanguageConstants.LegacyHelloCommandName, // skip handshake events, should be reconsidered in the scope of CSHARP-3823
                "hello"
            };

            if (!observeSensitiveCommands.GetValueOrDefault())
            {
                defaultCommandNamesToSkip.AddRange(new[] { "authenticate", "getnonce", "saslContinue", "saslStart" });
            }

            foreach (var eventsDetails in eventTypesToCapture)
            {
                var commandNamesNotToCapture = Enumerable.Concat(eventsDetails.CommandNotToCapture ?? Enumerable.Empty<string>(), defaultCommandNamesToSkip);
                var formatter = _eventFormatters.ContainsKey(eventsDetails.Key) ? _eventFormatters[eventsDetails.Key] : null;
                var eventCapturer = CreateEventCapturer(eventsDetails.Events, commandNamesNotToCapture, formatter);
                clientEventCapturers.Add(eventsDetails.Key, eventCapturer);
            }

            var eventCapturers = clientEventCapturers.Select(c => c.Value).ToArray();
            var client = DriverTestConfiguration.CreateMongoClient(
                settings =>
                {
                    settings.ApplicationName = FailPoint.DecorateApplicationName(appName, async);
                    settings.AutoEncryptionOptions = autoEncryptionOptions;
                    settings.ConnectTimeout = connectTimeout.GetValueOrDefault(defaultValue: settings.ConnectTimeout);
                    settings.LoadBalanced = loadBalanced.GetValueOrDefault(defaultValue: settings.LoadBalanced);
                    settings.LoggingSettings = _loggingSettings;
                    settings.MaxConnecting = maxConnecting.GetValueOrDefault(defaultValue: settings.MaxConnecting);
                    settings.MaxConnectionIdleTime = maxIdleTime.GetValueOrDefault(defaultValue: settings.MaxConnectionIdleTime);
                    settings.MaxConnectionPoolSize = maxPoolSize.GetValueOrDefault(defaultValue: settings.MaxConnectionPoolSize);
                    settings.MinConnectionPoolSize = minPoolSize.GetValueOrDefault(defaultValue: settings.MinConnectionPoolSize);
                    settings.RetryReads = retryReads;
                    settings.RetryWrites = retryWrites;
                    settings.ReadConcern = readConcern;
                    if (readPreference != null)
                    {
                        settings.ReadPreference = readPreference;
                    }

#pragma warning disable CS0618 // Type or member is obsolete
                    settings.WaitQueueSize = waitQueueSize.GetValueOrDefault(defaultValue: settings.WaitQueueSize);
#pragma warning restore CS0618 // Type or member is obsolete
                    settings.WaitQueueTimeout = waitQueueTimeout.GetValueOrDefault(defaultValue: settings.WaitQueueTimeout);
                    settings.WriteConcern = writeConcern;
                    settings.HeartbeatInterval = heartbeatFrequency.GetValueOrDefault(defaultValue: TimeSpan.FromMilliseconds(5)); // 5 ms default value for spec tests
                    settings.ServerApi = serverApi;
                    settings.ServerMonitoringMode = serverMonitoringMode.GetValueOrDefault(settings.ServerMonitoringMode);
                    settings.ServerSelectionTimeout = serverSelectionTimeout.GetValueOrDefault(defaultValue: settings.ServerSelectionTimeout);
                    settings.SocketTimeout = socketTimeout.GetValueOrDefault(defaultValue: settings.SocketTimeout);
                    settings.Timeout = timeout;
                    settings.TracingOptions = tracingOptions ?? new TracingOptions { Disabled = true };
                    if (eventCapturers.Length > 0)
                    {
                        settings.ClusterConfigurator = c =>
                        {
                            foreach (var eventCapturer in eventCapturers)
                            {
                                c.Subscribe(eventCapturer);
                            }
                        };
                    }

                    if (!string.IsNullOrEmpty(authMechanism))
                    {
                        settings.Credential = authMechanism switch
                        {
                            "MONGODB-OIDC" => MongoCredential.CreateRawOidcCredential(null),
                            _ => throw new NotSupportedException($"Cannot create credential for {authMechanism} auth mechanism")
                        };

                        if (authMechanismProperties.Count > 0)
                        {
                            foreach (var mechanismProperty in authMechanismProperties)
                            {
                                settings.Credential = settings.Credential.WithMechanismProperty(mechanismProperty.Key, mechanismProperty.Value);
                            }
                        }
                    }
                },
                useMultipleShardRouters);

            if (awaitMinPoolSizeTimeout.HasValue && minPoolSize is > 0)
            {
                if (!SpinWait.SpinUntil(() =>
                    {
                        var servers = ((IClusterInternal)client.Cluster).Servers.Where(s => s.Description.IsDataBearing).ToArray();
                        return servers.Any() && servers.All(s => ((ExclusiveConnectionPool)s.ConnectionPool).DormantCount >= minPoolSize);
                    }, awaitMinPoolSizeTimeout.Value))
                {
                    client.Dispose();
                    throw new TimeoutException("MinPoolSize population took too long");
                }

                foreach (var eventCapturer in clientEventCapturers.Values)
                {
                    eventCapturer.Clear();
                }
            }

            return (client, clientEventCapturers, loggingComponents, spanCapturer);
        }

        private ClientEncryption CreateClientEncryption(Dictionary<string, IMongoClient> clients, BsonDocument entity)
        {
            ClientEncryptionOptions options = null;
            foreach (var element in entity)
            {
                switch (element.Name)
                {
                    case "id":
                        // handled on higher level
                        break;
                    case "clientEncryptionOpts":
                        IMongoClient keyVaultClient = null;
                        CollectionNamespace keyVaultCollectionNamespace = null;
                        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders = null;
                        TimeSpan? keyExpiration = null;

                        foreach (var option in element.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "keyVaultClient":
                                    keyVaultClient = clients[option.Value.AsString];
                                    break;
                                case "keyVaultNamespace":
                                    keyVaultCollectionNamespace = CollectionNamespace.FromFullName(option.Value.AsString);
                                    break;
                                case "kmsProviders":
                                    kmsProviders = EncryptionTestHelper.ParseKmsProviders(option.Value.AsBsonDocument);
                                    break;
                                case "keyExpirationMS":
                                    keyExpiration = TimeSpan.FromMilliseconds(option.Value.AsInt32);
                                    break;
                                default:
                                    throw new FormatException($"Invalid clientEncryption option argument name: '{option.Name}'.");
                            }
                        }

                        var tlsOptions = EncryptionTestHelper.CreateTlsOptionsIfAllowed(kmsProviders, ((kmsProviderName) => kmsProviderName.StartsWith("kmip"))); // configure Tls for kmip by default
                        options = new ClientEncryptionOptions(
                            Ensure.IsNotNull(keyVaultClient, nameof(keyVaultClient)),
                            Ensure.IsNotNull(keyVaultCollectionNamespace, nameof(keyVaultCollectionNamespace)),
                            Ensure.IsNotNull(kmsProviders, nameof(kmsProviders)),
                            tlsOptions: tlsOptions);
                        options.SetKeyExpiration(keyExpiration);
                        break;
                    default:
                        throw new FormatException($"Invalid clientEncryption argument name: '{element.Name}'.");
                }
            }

            return new ClientEncryption(options);
        }

        private IMongoCollection<BsonDocument> CreateCollection(BsonDocument entity, Dictionary<string, IMongoDatabase> databases)
        {
            string collectionName = null;
            IMongoDatabase database = null;
            var settings = new MongoCollectionSettings { ReadPreference = ReadPreference.Primary };

            foreach (var element in entity)
            {
                switch (element.Name)
                {
                    case "id":
                        // handled on higher level
                        break;
                    case "database":
                        var databaseId = entity["database"].AsString;
                        database = databases[databaseId];
                        break;
                    case "collectionName":
                        collectionName = entity["collectionName"].AsString;
                        break;
                    case "collectionOptions":
                        foreach (var option in element.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "readConcern":
                                    settings.ReadConcern = ReadConcern.FromBsonDocument(option.Value.AsBsonDocument);
                                    break;
                                case "readPreference":
                                    settings.ReadPreference = ReadPreference.FromBsonDocument(option.Value.AsBsonDocument);
                                    break;
                                case "timeoutMS":
                                    settings.Timeout = ParseTimeout(option.Value);
                                    break;
                                case "writeConcern":
                                    settings.WriteConcern = ParseWriteConcern(option.Value.AsBsonDocument);
                                    break;
                                default:
                                    throw new FormatException($"Invalid collection option argument name: '{option.Name}'.");
                            }
                        }

                        break;
                    default:
                        throw new FormatException($"Invalid collection argument name: '{element.Name}'.");
                }
            }

            return database.GetCollection<BsonDocument>(collectionName, settings);
        }

        private IMongoDatabase CreateDatabase(BsonDocument entity, Dictionary<string, IMongoClient> clients)
        {
            IMongoClient client = null;
            string databaseName = null;
            MongoDatabaseSettings databaseSettings = null;

            foreach (var element in entity)
            {
                switch (element.Name)
                {
                    case "id":
                        // handled on higher level
                        break;
                    case "client":
                        var clientId = element.Value.AsString;
                        client = clients[clientId];
                        break;
                    case "databaseName":
                        databaseName = element.Value.AsString;
                        break;
                    case "databaseOptions":
                        databaseSettings = new MongoDatabaseSettings();
                        foreach (var option in element.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "readConcern":
                                    databaseSettings.ReadConcern = ReadConcern.FromBsonDocument(option.Value.AsBsonDocument);
                                    break;
                                case "readPreference":
                                    databaseSettings.ReadPreference = ReadPreference.FromBsonDocument(option.Value.AsBsonDocument);
                                    break;
                                case "timeoutMS":
                                    databaseSettings.Timeout = ParseTimeout(option.Value);
                                    break;
                                case "writeConcern":
                                    databaseSettings.WriteConcern = ParseWriteConcern(option.Value.AsBsonDocument);
                                    break;
                                default:
                                    throw new FormatException($"Invalid database option argument name: '{option.Name}'.");
                            }
                        }

                        break;
                    default:
                        throw new FormatException($"Invalid database argument name: '{element.Name}'.");
                }
            }

            return client.GetDatabase(databaseName, databaseSettings);
        }

        private EventCapturer CreateEventCapturer(IEnumerable<string> eventTypesToCapture, IEnumerable<string> commandNamesToSkip, IEventFormatter eventFormatter)
        {
            var eventCapturer = new EventCapturer(eventFormatter);

            foreach (var eventTypeToCapture in eventTypesToCapture)
            {
                switch (eventTypeToCapture.ToLowerInvariant())
                {
                    case "commandstartedevent":
                        eventCapturer = eventCapturer.Capture<CommandStartedEvent>(x => !commandNamesToSkip.Contains(x.CommandName));
                        break;
                    case "commandsucceededevent":
                        eventCapturer = eventCapturer.Capture<CommandSucceededEvent>(x => !commandNamesToSkip.Contains(x.CommandName));
                        break;
                    case "commandfailedevent":
                        eventCapturer = eventCapturer.Capture<CommandFailedEvent>(x => !commandNamesToSkip.Contains(x.CommandName));
                        break;
                    case "poolreadyevent":
                        eventCapturer = eventCapturer.Capture<ConnectionPoolReadyEvent>();
                        break;
                    case "poolcreatedevent":
                        eventCapturer = eventCapturer.Capture<ConnectionPoolOpenedEvent>();
                        break;
                    case "poolclearedevent":
                        eventCapturer = eventCapturer.Capture<ConnectionPoolClearedEvent>();
                        break;
                    case "poolclosedevent":
                        eventCapturer = eventCapturer.Capture<ConnectionPoolClosedEvent>();
                        break;
                    case "connectioncreatedevent":
                        eventCapturer = eventCapturer.Capture<ConnectionCreatedEvent>();
                        break;
                    case "connectionclosedevent":
                        eventCapturer = eventCapturer.Capture<ConnectionClosedEvent>();
                        break;
                    case "connectionreadyevent":
                        eventCapturer = eventCapturer.Capture<ConnectionOpenedEvent>();
                        break;
                    case "connectioncheckoutstartedevent":
                        eventCapturer = eventCapturer.Capture<ConnectionPoolCheckingOutConnectionEvent>();
                        break;
                    case "connectioncheckoutfailedevent":
                        eventCapturer = eventCapturer.Capture<ConnectionPoolCheckingOutConnectionFailedEvent>();
                        break;
                    case "connectioncheckedoutevent":
                        eventCapturer = eventCapturer.Capture<ConnectionPoolCheckedOutConnectionEvent>();
                        break;
                    case "connectioncheckedinevent":
                        eventCapturer = eventCapturer.Capture<ConnectionPoolCheckedInConnectionEvent>();
                        break;
                    case "serverdescriptionchangedevent":
                        eventCapturer = eventCapturer.Capture<ServerDescriptionChangedEvent>();
                        break;
                    case "serverheartbeatfailedevent":
                        eventCapturer = eventCapturer.Capture<ServerHeartbeatFailedEvent>();
                        break;
                    case "serverheartbeatstartedevent":
                        eventCapturer = eventCapturer.Capture<ServerHeartbeatStartedEvent>();
                        break;
                    case "serverheartbeatsucceededevent":
                        eventCapturer = eventCapturer.Capture<ServerHeartbeatSucceededEvent>();
                        break;
                    case "topologyclosedevent":
                        eventCapturer = eventCapturer.Capture<ClusterClosedEvent>();
                        break;
                    case "topologydescriptionchangedevent":
                        eventCapturer = eventCapturer.Capture<ClusterDescriptionChangedEvent>();
                        break;
                    case "topologyopeningevent":
                        eventCapturer = eventCapturer.Capture<ClusterOpeningEvent>();
                        break;
                    default:
                        throw new FormatException($"Invalid event name: {eventTypeToCapture}.");
                }
            }

            return eventCapturer;
        }

        private IClientSessionHandle CreateSession(BsonDocument entity, Dictionary<string, IMongoClient> clients)
        {
            IMongoClient client = null;
            ClientSessionOptions options = null;

            foreach (var element in entity)
            {
                switch (element.Name)
                {
                    case "id":
                        // handled on higher level
                        break;
                    case "client":
                        var clientId = element.Value.AsString;
                        client = clients[clientId];
                        break;
                    case "sessionOptions":
                        options = new ClientSessionOptions();
                        foreach (var option in element.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "snapshot":
                                    options.Snapshot = option.Value.ToBoolean();
                                    break;
                                case "causalConsistency":
                                    options.CausalConsistency = option.Value.ToBoolean();
                                    break;
                                case "defaultTimeoutMS":
                                    var timeout = ParseTimeout(option.Value);
                                    options.DefaultTransactionOptions = new TransactionOptions(
                                        timeout,
                                        options.DefaultTransactionOptions?.ReadConcern,
                                        options.DefaultTransactionOptions?.ReadPreference,
                                        options.DefaultTransactionOptions?.WriteConcern,
                                        options.DefaultTransactionOptions?.MaxCommitTime);
                                    break;
                                case "defaultTransactionOptions":
                                    ReadConcern readConcern = null;
                                    ReadPreference readPreference = null;
                                    WriteConcern writeConcern = null;
                                    TimeSpan? maxCommitTime = null;
                                    foreach (var transactionOption in option.Value.AsBsonDocument)
                                    {
                                        switch (transactionOption.Name)
                                        {
                                            case "maxCommitTimeMS":
                                                maxCommitTime = TimeSpan.FromMilliseconds(transactionOption.Value.AsInt32);
                                                break;
                                            case "readConcern":
                                                readConcern = ReadConcern.FromBsonDocument(transactionOption.Value.AsBsonDocument);
                                                break;
                                            case "readPreference":
                                                readPreference = ReadPreference.FromBsonDocument(transactionOption.Value.AsBsonDocument);
                                                break;
                                            case "writeConcern":
                                                writeConcern = ParseWriteConcern(transactionOption.Value.AsBsonDocument);
                                                break;
                                            default:
                                                throw new FormatException($"Invalid session transaction option: '{transactionOption.Name}'.");
                                        }
                                    }

                                    options.DefaultTransactionOptions = new TransactionOptions(options.DefaultTransactionOptions?.Timeout, readConcern, readPreference, writeConcern, maxCommitTime);
                                    break;
                                default:
                                    throw new FormatException($"Invalid session option argument name: '{option.Name}'.");
                            }
                        }

                        break;
                    default:
                        throw new FormatException($"Invalid session argument name: '{element.Name}'.");
                }
            }

            var session = client.StartSession(options);
            if (_lastKnownClusterTime != null)
            {
                session.WrappedCoreSession.AdvanceClusterTime(_lastKnownClusterTime);
            }

            return session;
        }

        private void ParseExtraOptions(BsonDocument extraOptionsDocument, Dictionary<string, object> extraOptions)
        {
            foreach (var extraOption in extraOptionsDocument.Elements)
            {
                switch (extraOption.Name)
                {
                    case "mongocryptdBypassSpawn":
                        extraOptions.Add(extraOption.Name, extraOption.Value.ToBoolean());
                        break;
                    default:
                        throw new FormatException($"Invalid extraOption argument name {extraOption.Name}.");
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnifiedEntityMap));
            }
        }

        public static WriteConcern ParseWriteConcern(BsonDocument writeConcernDocument)
        {
            var writeConcern = WriteConcern.FromBsonDocument(writeConcernDocument);

            // some tests contains wtimeoutMS and journal instead of standard wtimeout and j
            if (writeConcernDocument.TryGetValue("wtimeoutMS", out var wTimeout))
            {
                writeConcern = writeConcern.With(wTimeout: TimeSpan.FromMilliseconds(wTimeout.AsInt32));
            }

            if (writeConcernDocument.TryGetValue("journal", out var j))
            {
                writeConcern = writeConcern.With(journal: j.AsBoolean);
            }

            return writeConcern;
        }

        public static TimeSpan ParseTimeout(BsonValue value)
            => value.AsInt32 == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(value.AsInt32);
    }
}
