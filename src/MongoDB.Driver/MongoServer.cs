/* Copyright 2010-2014 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Communication;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.SyncExtensionMethods;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB server (either a single instance or a replica set) and the settings used to access it. This class is thread-safe.
    /// </summary>
    public class MongoServer
    {
        // private static fields
        private readonly static object __staticLock = new object();
        private readonly static Dictionary<MongoServerSettings, MongoServer> __servers = new Dictionary<MongoServerSettings, MongoServer>();
        private static int __nextSequentialId;
        private static int __maxServerCount = 100;
        private static HashSet<char> __invalidDatabaseNameChars;
        [ThreadStatic]
        private static Request __threadStaticRequest;

        // private fields
        private ICluster _cluster;
        private readonly object _serverLock = new object();
        private readonly MongoServerSettings _settings;
        private readonly int _sequentialId;
        private readonly List<MongoServerInstance> _serverInstances = new List<MongoServerInstance>();

        // static constructor
        static MongoServer()
        {
            // MongoDB itself prohibits some characters and the rest are prohibited by the Windows restrictions on filenames
            // the C# driver checks that the database name is valid on any of the supported platforms
            __invalidDatabaseNameChars = new HashSet<char>() { '\0', ' ', '.', '$', '/', '\\' };
            foreach (var c in Path.GetInvalidPathChars()) { __invalidDatabaseNameChars.Add(c); }
            foreach (var c in Path.GetInvalidFileNameChars()) { __invalidDatabaseNameChars.Add(c); }
        }

        // constructors
        /// <summary>
        /// Creates a new instance of MongoServer. Normally you will use one of the Create methods instead
        /// of the constructor to create instances of this class.
        /// </summary>
        /// <param name="settings">The settings for this instance of MongoServer.</param>
        public MongoServer(MongoServerSettings settings)
        {
            _settings = settings.FrozenCopy();
            _sequentialId = Interlocked.Increment(ref __nextSequentialId);
            // Console.WriteLine("MongoServer[{0}]: {1}", sequentialId, settings);

            _cluster = ClusterRegistry.Instance.GetOrCreateCluster(_settings);
            StartTrackingServerInstances();
        }

        // factory methods
        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        [Obsolete("Use MongoClient.GetServer instead.")]
        public static MongoServer Create()
        {
            return Create("mongodb://localhost");
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <param name="settings">Server settings.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        [Obsolete("Use MongoClient.GetServer instead.")]
        public static MongoServer Create(MongoServerSettings settings)
        {
            lock (__staticLock)
            {
                MongoServer server;
                if (!__servers.TryGetValue(settings, out server))
                {
                    if (__servers.Count >= __maxServerCount)
                    {
                        var message = string.Format("MongoServer.Create has already created {0} servers which is the maximum number of servers allowed.", __maxServerCount);
                        throw new MongoException(message);
                    }
                    server = new MongoServer(settings);
                    __servers.Add(settings, server);
                }
                return server;
            }
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <param name="url">Server settings in the form of a MongoUrl.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        [Obsolete("Use MongoClient.GetServer instead.")]
        public static MongoServer Create(MongoUrl url)
        {
            return Create(MongoServerSettings.FromUrl(url));
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <param name="connectionString">Server settings in the form of a connection string.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        [Obsolete("Use MongoClient.GetServer instead.")]
        public static MongoServer Create(string connectionString)
        {
            var url = MongoUrl.Create(connectionString);
            return Create(url);
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <param name="uri">Server settings in the form of a Uri.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        [Obsolete("Use MongoClient.GetServer instead.")]
        public static MongoServer Create(Uri uri)
        {
            var url = MongoUrl.Create(uri.ToString());
            return Create(url);
        }

        // public static properties
        /// <summary>
        /// Gets or sets the maximum number of instances of MongoServer that will be allowed to be created.
        /// </summary>
        public static int MaxServerCount
        {
            get { return __maxServerCount; }
            set { __maxServerCount = value; }
        }

        /// <summary>
        /// Gets the number of instances of MongoServer that have been created.
        /// </summary>
        public static int ServerCount
        {
            get
            {
                lock (__staticLock)
                {
                    return __servers.Count;
                }
            }
        }

        // public properties
        /// <summary>
        /// Gets the arbiter instances.
        /// </summary>
        public virtual MongoServerInstance[] Arbiters
        {
            get
            {
                lock (_serverLock)
                {
                    return _serverInstances.Where(i => i.IsArbiter).ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the build info of the server.
        /// </summary>
        public virtual MongoServerBuildInfo BuildInfo
        {
            get
            {
                return Primary.BuildInfo;
            }
        }

        /// <summary>
        /// Gets the cluster.
        /// </summary>
        /// <value>
        /// The cluster.
        /// </value>
        internal ICluster Cluster
        {
            get { return _cluster; }
        }

        /// <summary>
        /// Gets the one and only instance for this server.
        /// </summary>
        public virtual MongoServerInstance Instance
        {
            get
            {
                var instances = Instances;
                switch (instances.Length)
                {
                    case 0: return null;
                    case 1: return instances[0];
                    default:
                        throw new InvalidOperationException("Instance property cannot be used when there is more than one instance.");
                }
            }
        }

        /// <summary>
        /// Gets the instances for this server.
        /// </summary>
        public virtual MongoServerInstance[] Instances
        {
            get
            {
                lock (_serverLock)
                {
                    return _serverInstances.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the passive instances.
        /// </summary>
        public virtual MongoServerInstance[] Passives
        {
            get
            {
                lock (_serverLock)
                {
                    return _serverInstances.Where(i => i.IsPassive).ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the primary instance (null if there is no primary).
        /// </summary>
        public virtual MongoServerInstance Primary
        {
            get
            {
                lock (_serverLock)
                {
                    switch (_cluster.Description.Type)
                    {
                        case ClusterType.Standalone:
                            return _serverInstances.First();
                        case ClusterType.ReplicaSet:
                            return _serverInstances.Where(i => i.IsPrimary).SingleOrDefault();
                        case ClusterType.Sharded:
                            return _serverInstances.Where(i => i.State == MongoServerState.Connected).FirstOrDefault();
                        default:
                            return null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the name of the replica set (null if not connected to a replica set).
        /// </summary>
        public virtual string ReplicaSetName
        {
            get 
            {
                var replicaSetName = _cluster.Settings.ReplicaSetName;
                if (replicaSetName != null)
                {
                    return replicaSetName;
                }

                var primary = _cluster.Description.Servers.FirstOrDefault(s => s.Type == ServerType.ReplicaSetPrimary);
                if (primary != null)
                {
                    var replicaSetConfig = primary.ReplicaSetConfig;
                    if (replicaSetConfig != null)
                    {
                        return replicaSetConfig.Name;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the server instance of the connection reserved by the current RequestStart scope (null if not in the scope of a RequestStart).
        /// </summary>
        public virtual MongoServerInstance RequestServerInstance
        {
            get
            {
                var request = __threadStaticRequest;
                if (request != null)
                {
                    return request.ServerInstance;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the RequestStart nesting level for the current thread.
        /// </summary>
        public virtual int RequestNestingLevel
        {
            get
            {
                var request = __threadStaticRequest;
                if (request != null)
                {
                    return request.NestingLevel;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the secondary instances.
        /// </summary>
        public virtual MongoServerInstance[] Secondaries
        {
            get
            {
                lock (_serverLock)
                {
                    return _serverInstances.Where(i => i.IsSecondary).ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the unique sequential Id for this server.
        /// </summary>
        public virtual int SequentialId
        {
            get { return _sequentialId; }
        }

        /// <summary>
        /// Gets the settings for this server.
        /// </summary>
        public virtual MongoServerSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>
        /// Gets the current state of this server (as of the last operation, not updated until another operation is performed).
        /// </summary>
        public virtual MongoServerState State
        {
            get
            {
                switch (_cluster.Description.State)
                {
                    case ClusterState.Connected:
                        return MongoServerState.Connected;

                    case ClusterState.Disconnected:
                        return MongoServerState.Disconnected;

                    default:
                        throw new MongoInternalException("Invalid ClusterState.");
                }
            }
        }

        // public indexers
        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        [Obsolete("Use GetDatabase instead.")]
        public virtual MongoDatabase this[string databaseName]
        {
            get { return GetDatabase(databaseName); }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="writeConcern">The write concern to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        [Obsolete("Use GetDatabase instead.")]
        public virtual MongoDatabase this[string databaseName, WriteConcern writeConcern]
        {
            get { return GetDatabase(databaseName, writeConcern); }
        }

        // public static methods
        /// <summary>
        /// Gets an array containing a snapshot of the set of all servers that have been created so far.
        /// </summary>
        /// <returns>An array containing a snapshot of the set of all servers that have been created so far.</returns>
        public static MongoServer[] GetAllServers()
        {
            lock (__staticLock)
            {
                return __servers.Values.ToArray();
            }
        }

        /// <summary>
        /// Unregisters all servers from the dictionary used by Create to remember which servers have already been created.
        /// </summary>
        public static void UnregisterAllServers()
        {
            lock (__staticLock)
            {
                var serverList = __servers.Values.ToList();
                foreach (var server in serverList)
                {
                    UnregisterServer(server);
                }
            }
        }

        /// <summary>
        /// Unregisters a server from the dictionary used by Create to remember which servers have already been created.
        /// </summary>
        /// <param name="server">The server to unregister.</param>
        public static void UnregisterServer(MongoServer server)
        {
            lock (__staticLock)
            {
                try { server.Disconnect(); }
                catch { } // ignore exceptions
                __servers.Remove(server._settings);
            }
        }

        // public methods
        /// <summary>
        /// Connects to the server. Normally there is no need to call this method as
        /// the driver will connect to the server automatically when needed.
        /// </summary>
        public virtual void Connect()
        {
            Connect(_settings.ConnectTimeout);
        }

        /// <summary>
        /// Connects to the server. Normally there is no need to call this method as
        /// the driver will connect to the server automatically when needed.
        /// </summary>
        /// <param name="timeout">How long to wait before timing out.</param>
        public virtual void Connect(TimeSpan timeout)
        {
            var readPreference = _settings.ReadPreference;
            var readPreferenceServerSelector = new ReadPreferenceServerSelector(readPreference);
            _cluster.SelectServerAsync(readPreferenceServerSelector, timeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Tests whether a database exists.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>True if the database exists.</returns>
        public virtual bool DatabaseExists(string databaseName)
        {
            var databaseNames = GetDatabaseNames();
            return databaseNames.Contains(databaseName);
        }

        /// <summary>
        /// Disconnects from the server. Normally there is no need to call this method so
        /// you should be sure to have a good reason to call it.
        /// </summary>
        public virtual void Disconnect()
        {
            // do nothing
        }

        /// <summary>
        /// Drops a database.
        /// </summary>
        /// <param name="databaseName">The name of the database to be dropped.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropDatabase(string databaseName)
        {
            var databaseNamespace = new DatabaseNamespace(databaseName);
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new DropDatabaseOperation(databaseNamespace, messageEncoderSettings);
            var response = ExecuteWriteOperation(operation);
            return new CommandResult(response);
        }

        /// <summary>
        /// Fetches the document referred to by the DBRef.
        /// </summary>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>A BsonDocument (or null if the document was not found).</returns>
        public virtual BsonDocument FetchDBRef(MongoDBRef dbRef)
        {
            return FetchDBRefAs<BsonDocument>(dbRef);
        }

        /// <summary>
        /// Fetches the document referred to by the DBRef, deserialized as a <typeparamref name="TDocument"/>.
        /// </summary>
        /// <typeparam name="TDocument">The nominal type of the document to fetch.</typeparam>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>A <typeparamref name="TDocument"/> (or null if the document was not found).</returns>
        public virtual TDocument FetchDBRefAs<TDocument>(MongoDBRef dbRef)
        {
            return (TDocument)FetchDBRefAs(typeof(TDocument), dbRef);
        }

        /// <summary>
        /// Fetches the document referred to by the DBRef.
        /// </summary>
        /// <param name="documentType">The nominal type of the document to fetch.</param>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>The document (or null if the document was not found).</returns>
        public virtual object FetchDBRefAs(Type documentType, MongoDBRef dbRef)
        {
            if (dbRef.DatabaseName == null)
            {
                throw new ArgumentException("MongoDBRef DatabaseName missing.");
            }

            var database = GetDatabase(dbRef.DatabaseName);
            return database.FetchDBRefAs(documentType, dbRef);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(string databaseName)
        {
            var databaseSettings = new MongoDatabaseSettings();
            return GetDatabase(databaseName, databaseSettings);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="writeConcern">The write concern to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(string databaseName, WriteConcern writeConcern)
        {
            if (writeConcern == null)
            {
                throw new ArgumentNullException("writeConcern");
            }
            var databaseSettings = new MongoDatabaseSettings { WriteConcern = writeConcern };
            return GetDatabase(databaseName, databaseSettings);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="databaseSettings">The settings to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(string databaseName, MongoDatabaseSettings databaseSettings)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            if (databaseSettings == null)
            {
                throw new ArgumentNullException("databaseSettings");
            }
            return new MongoDatabase(this, databaseName, databaseSettings);
        }

        /// <summary>
        /// Gets the names of the databases on this server.
        /// </summary>
        /// <returns>A list of database names.</returns>
        public virtual IEnumerable<string> GetDatabaseNames()
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new ListDatabaseNamesOperation(messageEncoderSettings);
            return ExecuteReadOperation(operation).OrderBy(name => name);
        }

        /// <summary>
        /// Gets the last error (if any) that occurred on this connection. You MUST be within a RequestStart to call this method.
        /// </summary>
        /// <returns>The last error (<see cref=" GetLastErrorResult"/>)</returns>
        public virtual GetLastErrorResult GetLastError()
        {
            var adminDatabase = GetDatabase("admin");
            return adminDatabase.GetLastError();
        }

        /// <summary>
        /// Gets the server instance.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>The server instance.</returns>
        public virtual MongoServerInstance GetServerInstance(MongoServerAddress address)
        {
            lock (_serverLock)
            {
                return _serverInstances.FirstOrDefault(i => i.Address.Equals(address));
            }
        }

        /// <summary>
        /// Checks whether a given database name is valid on this server.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="message">An error message if the database name is not valid.</param>
        /// <returns>True if the database name is valid; otherwise, false.</returns>
        public virtual bool IsDatabaseNameValid(string databaseName, out string message)
        {
            message = null;

            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }

            if (databaseName == "")
            {
                message = "Database name is empty.";
                return false;
            }

            // make an exception for $external
            if (databaseName == "$external")
            {
                return true;
            }

            foreach (var c in databaseName)
            {
                if (__invalidDatabaseNameChars.Contains(c))
                {
                    var bytes = new byte[] { (byte)((int)c >> 8), (byte)((int)c & 255) };
                    var hex = BsonUtils.ToHexString(bytes);
                    message = string.Format("Database name '{0}' is not valid. The character 0x{1} '{2}' is not allowed in database names.", databaseName, hex, c);
                    return false;
                }
            }

            if (Encoding.UTF8.GetBytes(databaseName).Length > 64)
            {
                message = string.Format("Database name '{0}' exceeds 64 bytes (after encoding to UTF8).", databaseName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not). If server is a replica set, pings all members one at a time.
        /// </summary>
        public virtual void Ping()
        {
            var primary = Primary;
            if (primary == null)
            {
                throw new InvalidOperationException("There is no current primary.");
            }
            primary.Ping();
        }

        /// <summary>
        /// Reconnects to the server. Normally there is no need to call this method. All connections
        /// are closed and new connections will be opened as needed. Calling
        /// this method frequently will result in connection thrashing.
        /// </summary>
        public virtual void Reconnect()
        {
            // do nothing
        }

        /// <summary>
        /// Lets the server know that this thread is done with a series of related operations. Instead of calling this method it is better
        /// to put the return value of RequestStart in a using statement.
        /// </summary>
        public virtual void RequestDone()
        {
            var request = __threadStaticRequest;
            if (request != null)
            {
                if (--request.NestingLevel == 0)
                {
                    request.Binding.Dispose();
                    __threadStaticRequest = null;
                }
            }
            else
            {
                throw new InvalidOperationException("Thread is not in a request (did you call RequestStart?).");
            }
        }

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="initialDatabase">One of the databases involved in the related operations.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="RequestDone"/> from the Dispose method.</returns>
        public virtual IDisposable RequestStart(MongoDatabase initialDatabase)
        {
            return RequestStart(initialDatabase, ReadPreference.Primary);
        }

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="initialDatabase">One of the databases involved in the related operations.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="RequestDone"/> from the Dispose method.</returns>
        public virtual IDisposable RequestStart(MongoDatabase initialDatabase, ReadPreference readPreference)
        {
            var serverSelector = new ReadPreferenceServerSelector(readPreference);
            return RequestStart(serverSelector, readPreference);
        }

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="initialDatabase">One of the databases involved in the related operations.</param>
        /// <param name="serverInstance">The server instance this request should be tied to.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="RequestDone"/> from the Dispose method.</returns>
        public virtual IDisposable RequestStart(MongoDatabase initialDatabase, MongoServerInstance serverInstance)
        {
            var endPoint = serverInstance.EndPoint;
            var serverSelector = new EndPointServerSelector(endPoint);
            var coreReadPreference = serverInstance.GetServerDescription().Type.IsWritable() ? ReadPreference.Primary : ReadPreference.Secondary;
            return RequestStart(serverSelector, coreReadPreference);
        }

        // internal methods
        internal IReadBindingHandle GetReadBinding(ReadPreference readPreference)
        {
            var request = __threadStaticRequest;
            if (request != null)
            {
                return request.Binding.Fork();
            }

            if (readPreference.ReadPreferenceMode == ReadPreferenceMode.Primary)
            {
                return new ReadWriteBindingHandle(new WritableServerBinding(_cluster));
            }
            else
            {
                return new ReadBindingHandle(new ReadPreferenceBinding(_cluster, readPreference));
            }

        }

        internal MongoServerInstance GetServerInstance(EndPoint endPoint)
        {
            lock (_serverLock)
            {
                return _serverInstances.FirstOrDefault(i => EndPointHelper.Equals(i.EndPoint, endPoint));
            }
        }

        internal IWriteBindingHandle GetWriteBinding()
        {
            var request = __threadStaticRequest;
            if (request != null)
            {
                return ToWriteBinding(request.Binding).Fork();
            }

            return new ReadWriteBindingHandle(new WritableServerBinding(_cluster));
        }

        // private methods
        private TResult ExecuteReadOperation<TResult>(IReadOperation<TResult> operation, ReadPreference readPreference = null)
        {
            readPreference = readPreference ?? _settings.ReadPreference ?? ReadPreference.Primary;
            using (var binding = GetReadBinding(readPreference))
            {
                return operation.Execute(binding);
            }
        }

        private TResult ExecuteWriteOperation<TResult>(IWriteOperation<TResult> operation)
        {
            using (var binding = GetWriteBinding())
            {
                return operation.Execute(binding);
            }
        }

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Helper.StrictUtf8Encoding },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Helper.StrictUtf8Encoding }
            };
        }

        private void OnClusterDescriptionChanged(object sender, EventArgs args)
        {
            var clusterDescription = _cluster.Description;
            var endPoints = clusterDescription.Servers.Select(s => s.EndPoint).ToList();

            lock (_serverLock)
            {
                _serverInstances.RemoveAll(instance => !EndPointHelper.Contains(endPoints, instance.EndPoint));
                var newEndPoints = endPoints.Where(endPoint => !_serverInstances.Any(i => i.EndPoint.Equals(endPoint))).ToList();
                if (newEndPoints.Count > 0)
                {
                    _serverInstances.AddRange(endPoints.Select(endPoint => new MongoServerInstance(_settings, ToMongoServerAddress(endPoint), _cluster, endPoint)));
                    _serverInstances.Sort(ServerInstanceAddressComparer.Instance);
                }
            }
        }

        private IDisposable RequestStart(IServerSelector serverSelector, ReadPreference readPreference)
        {
            var request = __threadStaticRequest;
            if (request != null)
            {
                var selected = serverSelector.SelectServers(_cluster.Description, new[] { request.ServerDescription }).ToList();
                if (selected.Count == 0)
                {
                    throw new InvalidOperationException("A nested call to RequestStart was made that is not compatible with the existing request.");
                }
                request.NestingLevel++;
                return new RequestStartResult(this);
            }

            IReadBindingHandle connectionBinding;
            var server = _cluster.SelectServer(serverSelector);
            using (var connection = server.GetConnection())
            {
                if (readPreference.ReadPreferenceMode == ReadPreferenceMode.Primary)
                {
                    connectionBinding = new ReadWriteBindingHandle(new ConnectionReadWriteBinding(server, connection.Fork()));
                }
                else
                {
                    connectionBinding = new ReadBindingHandle(new ConnectionReadBinding(server, connection.Fork(), readPreference));
                }
            }

            var serverDescription = server.Description;
            var serverInstance = _serverInstances.Single(i => EndPointHelper.Equals(i.EndPoint, serverDescription.EndPoint));
            __threadStaticRequest = new Request(serverDescription, serverInstance, connectionBinding);

            return new RequestStartResult(this);
        }

        private void StartTrackingServerInstances()
        {
            _serverInstances.AddRange(_cluster.Description.Servers.Select(serverDescription =>
            {
                var endPoint = serverDescription.EndPoint;
                return new MongoServerInstance(_settings, ToMongoServerAddress(endPoint), _cluster, endPoint);
            }));
            _serverInstances.Sort(ServerInstanceAddressComparer.Instance);
            _cluster.DescriptionChanged += OnClusterDescriptionChanged;
        }

        private MongoServerAddress ToMongoServerAddress(EndPoint endPoint)
        {
            DnsEndPoint dnsEndPoint;
            IPEndPoint ipEndPoint;

            if ((dnsEndPoint = endPoint as DnsEndPoint) != null)
            {
                return new MongoServerAddress(dnsEndPoint.Host, dnsEndPoint.Port);
            }
            else if ((ipEndPoint = endPoint as IPEndPoint) != null)
            {
                return new MongoServerAddress(ipEndPoint.Address.ToString(), ipEndPoint.Port);
            }
            else
            {
                var message = string.Format("MongoServer does not support end points of type '{0}'.", endPoint.GetType().Name);
                throw new ArgumentException(message, "endPoint");
            }
        }

        private IWriteBindingHandle ToWriteBinding(IReadBindingHandle binding)
        {
            var writeBinding = binding as IWriteBindingHandle;
            if (writeBinding == null)
            {
                throw new InvalidOperationException("The current binding cannot be used for writing.");
            }
            return writeBinding;
        }

        // private nested classes
        private class ServerInstanceAddressComparer : IComparer<MongoServerInstance>
        {
            public static readonly ServerInstanceAddressComparer Instance = new ServerInstanceAddressComparer();

            public int Compare(MongoServerInstance x, MongoServerInstance y)
            {
                var result = x.Address.Host.CompareTo(y.Address.Host);
                if (result != 0)
                {
                    return result;
                }
                return x.Address.Port.CompareTo(y.Address.Port);
            }
        }

        private class Request
        {
            // private fields
            private readonly IReadBindingHandle _binding;
            private int _nestingLevel;
            private readonly ServerDescription _serverDescription;
            private readonly MongoServerInstance _serverInstance;

            // constructors
            public Request(ServerDescription serverDescription, MongoServerInstance serverInstance, IReadBindingHandle binding)
            {
                _serverDescription = serverDescription;
                _serverInstance = serverInstance;
                _binding = binding;
                _nestingLevel = 1;
            }

            // public properties
            public IReadBindingHandle Binding
            {
                get { return _binding; }
            }

            public MongoServerInstance ServerInstance
            {
                get { return _serverInstance; }
            }

            public int NestingLevel
            {
                get { return _nestingLevel; }
                set { _nestingLevel = value; }
            }

            public ServerDescription ServerDescription
            {
                get { return _serverDescription; }
            }
        }

        private class RequestStartResult : IDisposable
        {
            // private fields
            private MongoServer _server;

            // constructors
            public RequestStartResult(MongoServer server)
            {
                _server = server;
            }

            // public methods
            public void Dispose()
            {
                _server.RequestDone();
            }
        }
    }
}
