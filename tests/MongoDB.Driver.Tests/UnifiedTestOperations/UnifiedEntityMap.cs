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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests.Specifications.client_side_encryption;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedEntityMap : IDisposable
    {
        #region static
        public static UnifiedEntityMap Create(Dictionary<string, IEventFormatter> eventFormatters, LoggingSettings loggingSettings, bool async)
        {
            var creator = new UnifiedEntityMapCreator(eventFormatters, loggingSettings);
            return new UnifiedEntityMap(creator, async);
        }
        #endregion

        // private fields
        private readonly bool _async;
        private readonly UnifiedEntityMapCreator _creator;

        private readonly Dictionary<string, IGridFSBucket> _buckets;
        private readonly Dictionary<string, IEnumerator<ChangeStreamDocument<BsonDocument>>> _changeStreams;
        private readonly Dictionary<string, DisposableMongoClient> _clients;
        private readonly Dictionary<string, ClientEncryption> _clientEncryptions;
        private readonly Dictionary<string, EventCapturer> _clientEventCapturers;
        private readonly Dictionary<string, IMongoCollection<BsonDocument>> _collections;
        private readonly Dictionary<string, IEnumerator<BsonDocument>> _cursors;
        private readonly Dictionary<string, IMongoDatabase> _databases;
        private bool _disposed;
        private readonly Dictionary<string, BsonArray> _errorDocuments;
        private readonly Dictionary<string, BsonArray> _failureDocuments;
        private readonly Dictionary<string, long> _iterationCounts;
        private readonly Dictionary<string, Dictionary<string, LogLevel>> _loggingComponents;
        private readonly Dictionary<string, BsonValue> _results;
        private readonly Dictionary<string, IClientSessionHandle> _sessions;
        private readonly Dictionary<string, BsonDocument> _sessionIds;
        private readonly Dictionary<string, long> _successCounts;
        private readonly Dictionary<string, Task> _threads;
        private readonly Dictionary<string, ClusterDescription> _topologyDescriptions;

        private UnifiedEntityMap(
            UnifiedEntityMapCreator creator,
            bool async,
            Dictionary<string, IGridFSBucket> buckets = null,
            Dictionary<string, IEnumerator<ChangeStreamDocument<BsonDocument>>> changeStreams = null,
            Dictionary<string, DisposableMongoClient> clients = null,
            Dictionary<string, ClientEncryption> clientEncryptions = null,
            Dictionary<string, EventCapturer> clientEventCapturers = null,
            Dictionary<string, Dictionary<string, LogLevel>> loggingComponents = null,
            Dictionary<string, IMongoCollection<BsonDocument>> collections = null,
            Dictionary<string, IEnumerator<BsonDocument>> cursors = null,
            Dictionary<string, IMongoDatabase> databases = null,
            Dictionary<string, BsonArray> errorDocuments = null,
            Dictionary<string, BsonArray> failureDocuments = null,
            Dictionary<string, long> iterationCounts = null,
            Dictionary<string, BsonValue> results = null,
            Dictionary<string, IClientSessionHandle> sessions = null,
            Dictionary<string, BsonDocument> sessionIds = null,
            Dictionary<string, long> successCounts = null,
            Dictionary<string, Task> threads = null,
            Dictionary<string, ClusterDescription> topologyDescriptions = null)
        {
            _buckets = buckets ?? new();
            _changeStreams = changeStreams ?? new();
            _clients = clients ?? new();
            _clientEncryptions = clientEncryptions ?? new();
            _clientEventCapturers = clientEventCapturers ?? new();
            _loggingComponents = loggingComponents ?? new();
            _collections = collections ?? new();
            _cursors = cursors ?? new();
            _databases = databases ?? new();
            _errorDocuments = errorDocuments ?? new();
            _failureDocuments = failureDocuments ?? new();
            _iterationCounts = iterationCounts ?? new();
            _results = results ?? new();
            _sessions = sessions ?? new();
            _sessionIds = sessionIds ?? new();
            _successCounts = successCounts ?? new();
            _threads = threads ?? new();
            _topologyDescriptions = topologyDescriptions ?? new();

            _async = async;
            _creator = Ensure.IsNotNull(creator, nameof(creator));
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

        public Dictionary<string, DisposableMongoClient> Clients
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

        public Dictionary<string, BsonValue> Resutls
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
        public void Dispose()
        {
            if (!_disposed)
            {
                var toDisposeCollection = SelectDisposables(_changeStreams?.Values, _sessions?.Values, _clients?.Values, _clientEncryptions?.Values);
                foreach (var toDispose in toDisposeCollection)
                {
                    toDispose.Dispose();
                }

                _disposed = true;
            }

            IDisposable[] SelectDisposables(params IEnumerable[] items) => items.SelectMany(i => i.OfType<IDisposable>()).ToArray();
        }

        public void AddRange(BsonArray entities)
        {
            var entityMap = _creator.Create(entities, _async);
            _buckets.AddRange(entityMap._buckets);
            _changeStreams.AddRange(entityMap._changeStreams);
            _clients.AddRange(entityMap._clients);
            _clientEncryptions.AddRange(entityMap._clientEncryptions);
            _clientEventCapturers.AddRange(entityMap._clientEventCapturers);
            _collections.AddRange(entityMap._collections);
            _cursors.AddRange(entityMap._cursors);
            _databases.AddRange(entityMap._databases);
            _errorDocuments.AddRange(entityMap._errorDocuments);
            _failureDocuments.AddRange(entityMap._failureDocuments);
            _iterationCounts.AddRange(entityMap._iterationCounts);
            _loggingComponents.AddRange(entityMap._loggingComponents);
            _results.AddRange(entityMap._results);
            _sessions.AddRange(entityMap._sessions);
            _sessionIds.AddRange(entityMap._sessionIds);
            _successCounts.AddRange(entityMap._successCounts);
            _threads.AddRange(entityMap._threads);
            _topologyDescriptions.AddRange(entityMap._topologyDescriptions);
        }

        // private methods
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnifiedEntityMap));
            }
        }

        private sealed class UnifiedEntityMapCreator
        {
            private readonly Dictionary<string, IEventFormatter> _eventFormatters;
            private readonly LoggingSettings _loggingSettings;

            public UnifiedEntityMapCreator(Dictionary<string, IEventFormatter> eventFormatters, LoggingSettings loggingSettings)
            {
                _eventFormatters = eventFormatters ?? new();
                _loggingSettings = loggingSettings;
            }

            public UnifiedEntityMap Create(BsonArray entitiesArray, bool async)
            {
                var buckets = new Dictionary<string, IGridFSBucket>();
                var changeStreams = new Dictionary<string, IEnumerator<ChangeStreamDocument<BsonDocument>>>();
                var clientEventCapturers = new Dictionary<string, EventCapturer>();
                var loggingComponents = new Dictionary<string, Dictionary<string, LogLevel>>();
                var clients = new Dictionary<string, DisposableMongoClient>();
                var clientEncryptions = new Dictionary<string, ClientEncryption>();
                var collections = new Dictionary<string, IMongoCollection<BsonDocument>>();
                var cursors = new Dictionary<string, IEnumerator<BsonDocument>>();
                var databases = new Dictionary<string, IMongoDatabase>();
                var errorDocumentsMap = new Dictionary<string, BsonArray>();
                var failureDocumentsMap = new Dictionary<string, BsonArray>();
                var iterationCounts = new Dictionary<string, long>();
                var results = new Dictionary<string, BsonValue>();
                var sessions = new Dictionary<string, IClientSessionHandle>();
                var sessionIds = new Dictionary<string, BsonDocument>();
                var successCounts = new Dictionary<string, long>();
                var threads = new Dictionary<string, Task>();
                var topologyDescriptions = new Dictionary<string, ClusterDescription>();

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
                                EnsureIsNotHandled(buckets, id);
                                var bucket = CreateBucket(entity, databases);
                                buckets.Add(id, bucket);
                                break;
                            case "client":
                                EnsureIsNotHandled(clients, id);
                                var (client, eventCapturers, clientLoggingComponents) = CreateClient(entity, async);
                                clients.Add(id, client);
                                foreach (var createdEventCapturer in eventCapturers)
                                {
                                    clientEventCapturers.Add(createdEventCapturer.Key, createdEventCapturer.Value);
                                }

                                loggingComponents.Add(id, clientLoggingComponents);
                                break;
                            case "clientEncryption":
                                {
                                    EnsureIsNotHandled(clientEncryptions, id);
                                    var clientEncryption = CreateClientEncryption(clients, entity);
                                    clientEncryptions.Add(id, clientEncryption);
                                }
                                break;
                            case "collection":
                                EnsureIsNotHandled(collections, id);
                                var collection = CreateCollection(entity, databases);
                                collections.Add(id, collection);
                                break;
                            case "database":
                                EnsureIsNotHandled(databases, id);
                                var database = CreateDatabase(entity, clients);
                                databases.Add(id, database);
                                break;
                            case "session":
                                EnsureIsNotHandled(sessions, id);
                                var session = CreateSession(entity, clients);
                                var sessionId = session.WrappedCoreSession.Id;
                                sessions.Add(id, session);
                                sessionIds.Add(id, sessionId);
                                break;
                            case "thread":
                                EnsureIsNotHandled(threads, id);
                                threads.Add(id, null);
                                break;
                            default:
                                throw new FormatException($"Invalid entity type: '{entityType}'.");
                        }
                    }
                }

                return new UnifiedEntityMap(
                    this,
                    async,
                    buckets,
                    changeStreams,
                    clients,
                    clientEncryptions,
                    clientEventCapturers,
                    loggingComponents,
                    collections,
                    cursors,
                    databases,
                    errorDocumentsMap,
                    failureDocumentsMap,
                    iterationCounts,
                    results,
                    sessions,
                    sessionIds,
                    successCounts,
                    threads,
                    topologyDescriptions);

                void EnsureIsNotHandled<TEntity>(IDictionary<string, TEntity> dictionary, string key)
                {
                    if (dictionary.ContainsKey(key))
                    {
                        throw new Exception($"{typeof(TEntity).Name} entity with id '{key}' already exists.");
                    }
                }
            }

            // private methods
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

            private (DisposableMongoClient Client, Dictionary<string, EventCapturer> ClientEventCapturers, Dictionary<string, LogLevel> LoggingComponents) CreateClient(BsonDocument entity, bool async)
            {
                string appName = null;
                var clientEventCapturers = new Dictionary<string, EventCapturer>();
                Dictionary<string, LogLevel> loggingComponents = null;
                string clientId = null;
                var commandNamesToSkipInEvents = new List<string>();
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
                var retryReads = true;
                var retryWrites = true;
                TimeSpan? serverSelectionTimeout = null;
                int? waitQueueSize = null;
                TimeSpan? socketTimeout = null;                
                var useMultipleShardRouters = false;
                TimeSpan? waitQueueTimeout = null;
                var writeConcern = WriteConcern.Acknowledged;
                var serverApi = CoreTestConfiguration.ServerApi;

                foreach (var element in entity)
                {
                    switch (element.Name)
                    {
                        case "id":
                            clientId = element.Value.AsString;
                            break;
                        case "uriOptions":
                            foreach (var option in element.Value.AsBsonDocument)
                            {
                                switch (option.Name)
                                {
                                    case "appname":
                                        appName = option.Value.AsString;
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
                                    case "serverSelectionTimeoutMS":
                                        serverSelectionTimeout = TimeSpan.FromMilliseconds(option.Value.AsInt32);
                                        break;
                                    case "socketTimeoutMS":
                                        socketTimeout = TimeSpan.FromMilliseconds(option.Value.AsInt32);
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
                    OppressiveLanguageConstants.LegacyHelloCommandName,  // skip handshake events, should be reconsidered in the scope of CSHARP-3823
                    "hello"
                };

                if (!observeSensitiveCommands.GetValueOrDefault())
                {
                    defaultCommandNamesToSkip.AddRange(new[]
                    {
                        "authenticate",
                        "getnonce",
                        "saslContinue",
                        "saslStart"
                    });
                }

                foreach (var eventsDetails in eventTypesToCapture)
                {
                    var commandNamesNotToCapture = Enumerable.Concat(eventsDetails.CommandNotToCapture ?? Enumerable.Empty<string>(), defaultCommandNamesToSkip);
                    var formatter = _eventFormatters.ContainsKey(eventsDetails.Key) ? _eventFormatters[eventsDetails.Key] : null;
                    var eventCapturer = CreateEventCapturer(eventsDetails.Events, commandNamesNotToCapture, formatter);
                    clientEventCapturers.Add(eventsDetails.Key, eventCapturer);
                }

                var eventCapturers = clientEventCapturers.Select(c => c.Value).ToArray();
                var client = DriverTestConfiguration.CreateDisposableClient(
                    settings =>
                    {
                        settings.ApplicationName = FailPoint.DecorateApplicationName(appName, async);
                        settings.ConnectTimeout = connectTimeout.GetValueOrDefault(defaultValue: settings.ConnectTimeout);
                        settings.LoadBalanced = loadBalanced.GetValueOrDefault(defaultValue: settings.LoadBalanced);
                        settings.MaxConnecting = maxConnecting.GetValueOrDefault(defaultValue: settings.MaxConnecting);
                        settings.MaxConnectionIdleTime = maxIdleTime.GetValueOrDefault(defaultValue: settings.MaxConnectionIdleTime);
                        settings.MaxConnectionPoolSize = maxPoolSize.GetValueOrDefault(defaultValue: settings.MaxConnectionPoolSize);
                        settings.MinConnectionPoolSize = minPoolSize.GetValueOrDefault(defaultValue: settings.MinConnectionPoolSize);
                        settings.RetryReads = retryReads;
                        settings.RetryWrites = retryWrites;
                        settings.ReadConcern = readConcern;
#pragma warning disable CS0618 // Type or member is obsolete
                        settings.WaitQueueSize = waitQueueSize.GetValueOrDefault(defaultValue: settings.WaitQueueSize);
#pragma warning restore CS0618 // Type or member is obsolete
                        settings.WaitQueueTimeout = waitQueueTimeout.GetValueOrDefault(defaultValue: settings.WaitQueueTimeout);
                        settings.WriteConcern = writeConcern;
                        settings.HeartbeatInterval = heartbeatFrequency.GetValueOrDefault(defaultValue: TimeSpan.FromMilliseconds(5)); // 5 ms default value for spec tests
                        settings.ServerApi = serverApi;
                        settings.ServerSelectionTimeout = serverSelectionTimeout.GetValueOrDefault(defaultValue: settings.ServerSelectionTimeout);
                        settings.SocketTimeout = socketTimeout.GetValueOrDefault(defaultValue: settings.SocketTimeout);
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
                    },
                    _loggingSettings,
                    useMultipleShardRouters);

                return (client, clientEventCapturers, loggingComponents);
            }

            private ClientEncryption CreateClientEncryption(Dictionary<string, DisposableMongoClient> clients, BsonDocument entity)
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
                                    default:
                                        throw new FormatException($"Invalid collection option argument name: '{option.Name}'.");
                                }
                            }

                            var tlsOptions = EncryptionTestHelper.CreateTlsOptionsIfAllowed(kmsProviders, ((kmsProviderName) => kmsProviderName == "kmip")); // configure Tls for kmip by default
                            options = new ClientEncryptionOptions(
                                Ensure.IsNotNull(keyVaultClient, nameof(keyVaultClient)),
                                Ensure.IsNotNull(keyVaultCollectionNamespace, nameof(keyVaultCollectionNamespace)),
                                Ensure.IsNotNull(kmsProviders, nameof(kmsProviders)),
                                tlsOptions: tlsOptions);
                            break;
                        default:
                            throw new FormatException($"Invalid {nameof(ClientEncryptionOptions)} argument name: '{element.Name}'.");
                    }
                }

                return new ClientEncryption(options);
            }

            private IMongoCollection<BsonDocument> CreateCollection(BsonDocument entity, Dictionary<string, IMongoDatabase> databases)
            {
                string collectionName = null;
                IMongoDatabase database = null;
                MongoCollectionSettings settings = null;

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
                            settings = new MongoCollectionSettings();
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
                                    case "writeConcern":
                                        settings.WriteConcern = WriteConcern.FromBsonDocument(option.Value.AsBsonDocument);
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

            private IMongoDatabase CreateDatabase(BsonDocument entity, Dictionary<string, DisposableMongoClient> clients)
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
                                    case "readPreference":
                                        databaseSettings.ReadPreference = ReadPreference.FromBsonDocument(option.Value.AsBsonDocument);
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
                        default:
                            throw new FormatException($"Invalid event name: {eventTypeToCapture}.");
                    }
                }

                return eventCapturer;
            }

            private IClientSessionHandle CreateSession(BsonDocument entity, Dictionary<string, DisposableMongoClient> clients)
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
                                    case "defaultTransactionOptions":
                                        ReadConcern readConcern = null;
                                        ReadPreference readPreference = null;
                                        WriteConcern writeConcern = null;
                                        foreach (var transactionOption in option.Value.AsBsonDocument)
                                        {
                                            switch (transactionOption.Name)
                                            {
                                                case "readConcern":
                                                    readConcern = ReadConcern.FromBsonDocument(transactionOption.Value.AsBsonDocument);
                                                    break;
                                                case "readPreference":
                                                    readPreference = ReadPreference.FromBsonDocument(transactionOption.Value.AsBsonDocument);
                                                    break;
                                                case "writeConcern":
                                                    writeConcern = WriteConcern.FromBsonDocument(transactionOption.Value.AsBsonDocument);
                                                    break;
                                                default:
                                                    throw new FormatException($"Invalid session transaction option: '{transactionOption.Name}'.");
                                            }
                                        }
                                        options.DefaultTransactionOptions = new TransactionOptions(readConcern, readPreference, writeConcern);
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

                return session;
            }
        }
    }
}
