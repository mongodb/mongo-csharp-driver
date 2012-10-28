/* Copyright 2010-2012 10gen Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

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

        // private fields
        private readonly object _serverLock = new object();
        private readonly IMongoServerProxy _serverProxy;
        private readonly MongoServerSettings _settings;
        private readonly Dictionary<MongoDatabaseSettings, MongoDatabase> _databases = new Dictionary<MongoDatabaseSettings, MongoDatabase>();
        private readonly Dictionary<int, Request> _requests = new Dictionary<int, Request>(); // tracks threads that have called RequestStart
        private readonly IndexCache _indexCache = new IndexCache();
        private int _sequentialId;

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

            _serverProxy = new MongoServerProxyFactory().Create(this);
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
        /// <param name="builder">Server settings in the form of a MongoConnectionStringBuilder.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        [Obsolete("Use MongoClient.GetServer instead.")]
        public static MongoServer Create(MongoConnectionStringBuilder builder)
        {
            return Create(MongoServerSettings.FromConnectionStringBuilder(builder));
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
            if (connectionString.StartsWith("mongodb://", StringComparison.Ordinal))
            {
                var url = MongoUrl.Create(connectionString);
                return Create(url);
            }
            else
            {
                var builder = new MongoConnectionStringBuilder(connectionString);
                return Create(builder);
            }
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
                return _serverProxy.Instances.Where(i => i.IsArbiter).ToArray();
            }
        }

        /// <summary>
        /// Gets the build info of the server.
        /// </summary>
        public virtual MongoServerBuildInfo BuildInfo
        {
            get { return _serverProxy.BuildInfo; }
        }

        /// <summary>
        /// Gets the most recent connection attempt number.
        /// </summary>
        public virtual int ConnectionAttempt
        {
            get { return _serverProxy.ConnectionAttempt; }
        }

        /// <summary>
        /// Gets the index cache (used by EnsureIndex) for this server.
        /// </summary>
        public virtual IndexCache IndexCache
        {
            get { return _indexCache; }
        }

        /// <summary>
        /// Gets the one and only instance for this server.
        /// </summary>
        public virtual MongoServerInstance Instance
        {
            get
            {
                var instances = _serverProxy.Instances;
                switch (instances.Count)
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
                return _serverProxy.Instances.ToArray();
            }
        }

        /// <summary>
        /// Gets the passive instances.
        /// </summary>
        public virtual MongoServerInstance[] Passives
        {
            get
            {
                return _serverProxy.Instances.Where(i => i.IsPassive).ToArray();
            }
        }

        /// <summary>
        /// Gets the primary instance (null if there is no primary).
        /// </summary>
        public virtual MongoServerInstance Primary
        {
            get
            {
                return _serverProxy.Instances.SingleOrDefault(x => x.IsPrimary);
            }
        }

        /// <summary>
        /// Gets the name of the replica set (null if not connected to a replica set).
        /// </summary>
        public virtual string ReplicaSetName
        {
            get 
            {
                var instanceManager = _serverProxy as ReplicaSetMongoServerProxy;
                if (instanceManager != null)
                {
                    return instanceManager.ReplicaSetName;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the connection reserved by the current RequestStart scope (null if not in the scope of a RequestStart).
        /// </summary>
        public virtual MongoConnection RequestConnection
        {
            get
            {
                lock (_serverLock)
                {
                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    Request request;
                    if (_requests.TryGetValue(threadId, out request))
                    {
                        return request.Connection;
                    }
                    else
                    {
                        return null;
                    }
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
                lock (_serverLock)
                {
                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    Request request;
                    if (_requests.TryGetValue(threadId, out request))
                    {
                        return request.NestingLevel;
                    }
                    else
                    {
                        return 0;
                    }
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
                return _serverProxy.Instances.Where(i => i.IsSecondary).ToArray();
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
            get { return _serverProxy.State; }
        }

        // public indexers
        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase this[string databaseName]
        {
            get { return GetDatabase(databaseName); }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase this[string databaseName, MongoCredentials credentials]
        {
            get { return GetDatabase(databaseName, credentials); }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseSettings">The settings to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase this[MongoDatabaseSettings databaseSettings]
        {
            get { return GetDatabase(databaseSettings); }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <param name="writeConcern">The write concern to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase this[string databaseName, MongoCredentials credentials, WriteConcern writeConcern]
        {
            get { return GetDatabase(databaseName, credentials, writeConcern); }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="writeConcern">The write concern to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
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
            _serverProxy.Connect(timeout, _settings.ReadPreference);
        }

        // TODO: fromHost parameter?
        /// <summary>
        /// Copies a database.
        /// </summary>
        /// <param name="from">The name of an existing database.</param>
        /// <param name="to">The name of the new database.</param>
        public virtual void CopyDatabase(string from, string to)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an instance of MongoDatabaseSettings for the named database with the rest of the settings inherited.
        /// You can override some of these settings before calling GetDatabase.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>An instance of MongoDatabase for <paramref name="databaseName"/>.</returns>
        public virtual MongoDatabaseSettings CreateDatabaseSettings(string databaseName)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            return new MongoDatabaseSettings(this, databaseName);
        }

        /// <summary>
        /// Tests whether a database exists.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>True if the database exists.</returns>
        public virtual bool DatabaseExists(string databaseName)
        {
            var adminCredentials = _settings.GetCredentials("admin");
            return DatabaseExists(databaseName, adminCredentials);
        }

        /// <summary>
        /// Tests whether a database exists.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        /// <returns>True if the database exists.</returns>
        public virtual bool DatabaseExists(string databaseName, MongoCredentials adminCredentials)
        {
            var databaseNames = GetDatabaseNames(adminCredentials);
            return databaseNames.Contains(databaseName);
        }

        /// <summary>
        /// Disconnects from the server. Normally there is no need to call this method so
        /// you should be sure to have a good reason to call it.
        /// </summary>
        public virtual void Disconnect()
        {
            _serverProxy.Disconnect();
        }

        /// <summary>
        /// Drops a database.
        /// </summary>
        /// <param name="databaseName">The name of the database to be dropped.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropDatabase(string databaseName)
        {
            var credentials = _settings.GetCredentials(databaseName);
            return DropDatabase(databaseName, credentials);
        }

        /// <summary>
        /// Drops a database.
        /// </summary>
        /// <param name="databaseName">The name of the database to be dropped.</param>
        /// <param name="credentials">Credentials for the database to be dropped (or admin credentials).</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropDatabase(string databaseName, MongoCredentials credentials)
        {
            MongoDatabase database = GetDatabase(databaseName, credentials);
            var command = new CommandDocument("dropDatabase", 1);
            var result = database.RunCommand(command);
            _indexCache.Reset(databaseName);
            return result;
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
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseSettings">The settings to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(MongoDatabaseSettings databaseSettings)
        {
            lock (_serverLock)
            {
                MongoDatabase database;
                if (!_databases.TryGetValue(databaseSettings, out database))
                {
                    database = new MongoDatabase(this, databaseSettings);
                    _databases.Add(databaseSettings, database);
                }
                return database;
            }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(string databaseName)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            var databaseSettings = new MongoDatabaseSettings(this, databaseName);
            return GetDatabase(databaseSettings);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(string databaseName, MongoCredentials credentials)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            var databaseSettings = new MongoDatabaseSettings(this, databaseName)
            {
                Credentials = credentials
            };
            return GetDatabase(databaseSettings);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <param name="writeConcern">The write concern to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(
            string databaseName,
            MongoCredentials credentials,
            WriteConcern writeConcern)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            var databaseSettings = new MongoDatabaseSettings(this, databaseName)
            {
                Credentials = credentials,
                WriteConcern = writeConcern
            };
            return GetDatabase(databaseSettings);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="writeConcern">The write concern to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(string databaseName, WriteConcern writeConcern)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            var databaseSettings = new MongoDatabaseSettings(this, databaseName)
            {
                WriteConcern = writeConcern
            };
            return GetDatabase(databaseSettings);
        }

        /// <summary>
        /// Gets the names of the databases on this server.
        /// </summary>
        /// <returns>A list of database names.</returns>
        public virtual IEnumerable<string> GetDatabaseNames()
        {
            var adminCredentials = _settings.GetCredentials("admin");
            return GetDatabaseNames(adminCredentials);
        }

        /// <summary>
        /// Gets the names of the databases on this server.
        /// </summary>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        /// <returns>A list of database names.</returns>
        public virtual IEnumerable<string> GetDatabaseNames(MongoCredentials adminCredentials)
        {
            var adminDatabase = GetDatabase("admin", adminCredentials);
            var result = adminDatabase.RunCommand("listDatabases");
            var databaseNames = new List<string>();
            foreach (BsonDocument database in result.Response["databases"].AsBsonArray.Values)
            {
                string databaseName = database["name"].AsString;
                databaseNames.Add(databaseName);
            }
            databaseNames.Sort();
            return databaseNames;
        }

        /// <summary>
        /// Gets the last error (if any) that occurred on this connection. You MUST be within a RequestStart to call this method.
        /// </summary>
        /// <returns>The last error (<see cref=" GetLastErrorResult"/>)</returns>
        public virtual GetLastErrorResult GetLastError()
        {
            var adminCredentials = _settings.GetCredentials("admin");
            return GetLastError(adminCredentials);
        }

        /// <summary>
        /// Gets the last error (if any) that occurred on this connection. You MUST be within a RequestStart to call this method.
        /// </summary>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        /// <returns>The last error (<see cref=" GetLastErrorResult"/>)</returns>
        public virtual GetLastErrorResult GetLastError(MongoCredentials adminCredentials)
        {
            var adminDatabase = GetDatabase("admin", adminCredentials);
            return adminDatabase.GetLastError();
        }

        /// <summary>
        /// Checks whether a given database name is valid on this server.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="message">An error message if the database name is not valid.</param>
        /// <returns>True if the database name is valid; otherwise, false.</returns>
        public virtual bool IsDatabaseNameValid(string databaseName, out string message)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }

            if (databaseName == "")
            {
                message = "Database name is empty.";
                return false;
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

            message = null;
            return true;
        }

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not). If server is a replica set, pings all members one at a time.
        /// </summary>
        public virtual void Ping()
        {
            _serverProxy.Ping();
        }

        /// <summary>
        /// Reconnects to the server. Normally there is no need to call this method. All connections
        /// are closed and new connections will be opened as needed. Calling
        /// this method frequently will result in connection thrashing.
        /// </summary>
        public virtual void Reconnect()
        {
            lock (_serverLock)
            {
                Disconnect();
                Connect();
            }
        }

        /// <summary>
        /// Lets the server know that this thread is done with a series of related operations. Instead of calling this method it is better
        /// to put the return value of RequestStart in a using statement.
        /// </summary>
        public virtual void RequestDone()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            MongoConnection connectionToRelease = null;

            lock (_serverLock)
            {
                Request request;
                if (_requests.TryGetValue(threadId, out request))
                {
                    if (--request.NestingLevel == 0)
                    {
                        _requests.Remove(threadId);
                        connectionToRelease = request.Connection;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Thread is not in a request (did you call RequestStart?).");
                }
            }

            if (connectionToRelease != null)
            {
                connectionToRelease.ServerInstance.ReleaseConnection(connectionToRelease);
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
        /// <param name="slaveOk">Whether a secondary is acceptable.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="RequestDone"/> from the Dispose method.</returns>
        [Obsolete("Use the overload of RequestStart that has a ReadPreference parameter instead.")]
        public virtual IDisposable RequestStart(MongoDatabase initialDatabase, bool slaveOk)
        {
            var readPreference = ReadPreference.FromSlaveOk(slaveOk);
            return RequestStart(initialDatabase, readPreference);
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
            int threadId = Thread.CurrentThread.ManagedThreadId;

            lock (_serverLock)
            {
                Request request;
                if (_requests.TryGetValue(threadId, out request))
                {
                    if (!readPreference.MatchesInstance(request.Connection.ServerInstance))
                    {
                        throw new InvalidOperationException("A nested call to RequestStart was made and the current instance does not match the nested read preference.");
                    }
                    request.NestingLevel++;
                    return new RequestStartResult(this);
                }

            }

            var serverInstance = _serverProxy.ChooseServerInstance(readPreference);
            var connection = serverInstance.AcquireConnection(initialDatabase);

            lock (_serverLock)
            {
                var request = new Request(connection);
                _requests.Add(threadId, request);
                return new RequestStartResult(this);
            }
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
            int threadId = Thread.CurrentThread.ManagedThreadId;

            lock (_serverLock)
            {
                Request request;
                if (_requests.TryGetValue(threadId, out request))
                {
                    if (serverInstance != request.Connection.ServerInstance)
                    {
                        throw new InvalidOperationException("The server instance passed to a nested call to RequestStart does not match the server instance of the current Request.");
                    }
                    request.NestingLevel++;
                    return new RequestStartResult(this);
                }
            }

            var connection = serverInstance.AcquireConnection(initialDatabase);

            lock (_serverLock)
            {
                var request = new Request(connection);
                _requests.Add(threadId, request);
                return new RequestStartResult(this);
            }
        }

        /// <summary>
        /// Removes all entries in the index cache used by EnsureIndex. Call this method
        /// when you know (or suspect) that a process other than this one may have dropped one or
        /// more indexes.
        /// </summary>
        public virtual void ResetIndexCache()
        {
            _indexCache.Reset();
        }

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        public virtual void Shutdown()
        {
            var adminCredentials = _settings.GetCredentials("admin");
            Shutdown(adminCredentials);
        }

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        public virtual void Shutdown(MongoCredentials adminCredentials)
        {
            lock (_serverLock)
            {
                try
                {
                    var adminDatabase = GetDatabase("admin", adminCredentials);
                    adminDatabase.RunCommand("shutdown");
                }
                catch (EndOfStreamException)
                {
                    // we expect an EndOfStreamException when the server shuts down so we ignore it
                }
            }
        }

        /// <summary>
        /// Verifies the state of the server (in the case of a replica set all members are contacted one at a time).
        /// </summary>
        public virtual void VerifyState()
        {
            _serverProxy.VerifyState();
        }

        // internal methods
        internal MongoConnection AcquireConnection(MongoDatabase database, ReadPreference readPreference)
        {
            MongoConnection requestConnection = null;
            lock (_serverLock)
            {
                // if a thread has called RequestStart it wants all operations to take place on the same connection
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Request request;
                if (_requests.TryGetValue(threadId, out request))
                {
                    if (!readPreference.MatchesInstance(request.Connection.ServerInstance))
                    {
                        throw new InvalidOperationException("The thread is in a RequestStart and the current server instance is not a match for the supplied read preference.");
                    }
                    requestConnection = request.Connection;
                }
            }

            // check authentication outside of lock
            if (requestConnection != null)
            {
                requestConnection.CheckAuthentication(database); // will throw exception if authentication fails
                return requestConnection;
            }

            var serverInstance = _serverProxy.ChooseServerInstance(readPreference);
            return serverInstance.AcquireConnection(database);
        }

        internal MongoConnection AcquireConnection(MongoDatabase database, MongoServerInstance serverInstance)
        {
            MongoConnection requestConnection = null;
            lock (_serverLock)
            {
                // if a thread has called RequestStart it wants all operations to take place on the same connection
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Request request;
                if (_requests.TryGetValue(threadId, out request))
                {
                    if (request.Connection.ServerInstance != serverInstance)
                    {
                        var message = string.Format(
                            "AcquireConnection called for server instance '{0}' but thread is in a RequestStart for server instance '{1}'.",
                            serverInstance.Address, request.Connection.ServerInstance.Address);
                        throw new MongoConnectionException(message);
                    }
                    requestConnection = request.Connection;
                }
            }

            // check authentication outside of lock
            if (requestConnection != null)
            {
                requestConnection.CheckAuthentication(database); // will throw exception if authentication fails
                return requestConnection;
            }

            return serverInstance.AcquireConnection(database);
        }

        internal void ReleaseConnection(MongoConnection connection)
        {
            lock (_serverLock)
            {
                // if the thread has called RequestStart just verify that the connection it is releasing is the right one
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Request request;
                if (_requests.TryGetValue(threadId, out request))
                {
                    if (connection != request.Connection)
                    {
                        throw new ArgumentException("Connection being released is not the one assigned to the thread by RequestStart.", "connection");
                    }
                    return; // hold on to the connection until RequestDone is called
                }
            }

            connection.ServerInstance.ReleaseConnection(connection);
        }

        // private nested classes
        private class Request
        {
            // private fields
            private MongoConnection _connection;
            private int _nestingLevel;

            // constructors
            public Request(MongoConnection connection)
            {
                _connection = connection;
                _nestingLevel = 1;
            }

            // public properties
            public MongoConnection Connection
            {
                get { return _connection; }
            }

            public int NestingLevel
            {
                get { return _nestingLevel; }
                set { _nestingLevel = value; }
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
