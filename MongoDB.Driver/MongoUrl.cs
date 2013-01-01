/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Server connection mode.
    /// </summary>
    [Serializable]
    public enum ConnectionMode
    {
        /// <summary>
        /// Automatically determine how to connect.
        /// </summary>
        Automatic,
        /// <summary>
        /// Connect directly to a server.
        /// </summary>
        Direct,
        /// <summary>
        /// Connect to a replica set.
        /// </summary>
        ReplicaSet,
        /// <summary>
        /// Connect to one or more shard routers.
        /// </summary>
        ShardRouter
    }

    /// <summary>
    /// Represents an immutable URL style connection string. See also MongoUrlBuilder.
    /// </summary>
    [Serializable]
    public class MongoUrl : IEquatable<MongoUrl>
    {
        // private static fields
        private static object __staticLock = new object();
        private static Dictionary<string, MongoUrl> __cache = new Dictionary<string, MongoUrl>();

        // private fields
        private readonly ConnectionMode _connectionMode;
        private readonly TimeSpan _connectTimeout;
        private readonly string _databaseName;
        private readonly MongoCredentials _defaultCredentials;
        private readonly bool? _fsync;
        private readonly GuidRepresentation _guidRepresentation;
        private readonly bool _ipv6;
        private readonly bool? _journal;
        private readonly TimeSpan _maxConnectionIdleTime;
        private readonly TimeSpan _maxConnectionLifeTime;
        private readonly int _maxConnectionPoolSize;
        private readonly int _minConnectionPoolSize;
        private readonly ReadPreference _readPreference;
        private readonly string _replicaSetName;
        private readonly TimeSpan _secondaryAcceptableLatency;
        private readonly IEnumerable<MongoServerAddress> _servers;
        private readonly bool _slaveOk;
        private readonly TimeSpan _socketTimeout;
        private readonly bool _useSsl;
        private readonly bool _verifySslCertificate;
        private readonly WriteConcern.WValue _w;
        private readonly double _waitQueueMultiple;
        private readonly int _waitQueueSize;
        private readonly TimeSpan _waitQueueTimeout;
        private readonly TimeSpan? _wTimeout;
        private readonly string _url;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoUrl.
        /// </summary>
        /// <param name="url">The URL containing the settings.</param>
        public MongoUrl(string url)
        {
            var builder = new MongoUrlBuilder(url); // parses url
            _connectionMode = builder.ConnectionMode;
            _connectTimeout = builder.ConnectTimeout;
            _databaseName = builder.DatabaseName;
            _defaultCredentials = builder.DefaultCredentials;
            _fsync = builder.FSync;
            _guidRepresentation = builder.GuidRepresentation;
            _ipv6 = builder.IPv6;
            _journal = builder.Journal;
            _maxConnectionIdleTime = builder.MaxConnectionIdleTime;
            _maxConnectionLifeTime = builder.MaxConnectionLifeTime;
            _maxConnectionPoolSize = builder.MaxConnectionPoolSize;
            _minConnectionPoolSize = builder.MinConnectionPoolSize;
            _readPreference = builder.ReadPreference;
            _replicaSetName = builder.ReplicaSetName;
            _secondaryAcceptableLatency = builder.SecondaryAcceptableLatency;
            _servers = builder.Servers;
#pragma warning disable 618
            _slaveOk = builder.SlaveOk;
#pragma warning restore
            _socketTimeout = builder.SocketTimeout;
            _useSsl = builder.UseSsl;
            _verifySslCertificate = builder.VerifySslCertificate;
            _w = builder.W;
            _waitQueueMultiple = builder.WaitQueueMultiple;
            _waitQueueSize = builder.WaitQueueSize;
            _waitQueueTimeout = builder.WaitQueueTimeout;
            _wTimeout = builder.WTimeout;
            _url = builder.ToString(); // keep canonical form
        }

        // public properties
        /// <summary>
        /// Gets the actual wait queue size (either WaitQueueSize or WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public int ComputedWaitQueueSize
        {
            get
            {
                if (_waitQueueMultiple == 0.0)
                {
                    return _waitQueueSize;
                }
                else
                {
                    return (int)(_waitQueueMultiple * _maxConnectionPoolSize);
                }
            }
        }

        /// <summary>
        /// Gets the connection mode.
        /// </summary>
        public ConnectionMode ConnectionMode
        {
            get { return _connectionMode; }
        }

        /// <summary>
        /// Gets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return _connectTimeout; }
        }

        /// <summary>
        /// Gets the optional database name.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
        }

        /// <summary>
        /// Gets the default credentials.
        /// </summary>
        public MongoCredentials DefaultCredentials
        {
            get { return _defaultCredentials; }
        }

        /// <summary>
        /// Gets the FSync component of the write concern.
        /// </summary>
        public bool? FSync
        {
            get { return _fsync; }
        }

        /// <summary>
        /// Gets the representation to use for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return _guidRepresentation; }
        }

        /// <summary>
        /// Gets whether to use IPv6.
        /// </summary>
        public bool IPv6
        {
            get { return _ipv6; }
        }

        /// <summary>
        /// Gets the Journal component of the write concern.
        /// </summary>
        public bool? Journal
        {
            get { return _journal; }
        }

        /// <summary>
        /// Gets the max connection idle time.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime
        {
            get { return _maxConnectionIdleTime; }
        }

        /// <summary>
        /// Gets the max connection life time.
        /// </summary>
        public TimeSpan MaxConnectionLifeTime
        {
            get { return _maxConnectionLifeTime; }
        }

        /// <summary>
        /// Gets the max connection pool size.
        /// </summary>
        public int MaxConnectionPoolSize
        {
            get { return _maxConnectionPoolSize; }
        }

        /// <summary>
        /// Gets the min connection pool size.
        /// </summary>
        public int MinConnectionPoolSize
        {
            get { return _minConnectionPoolSize; }
        }

        /// <summary>
        /// Gets the read preference.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
        }

        /// <summary>
        /// Gets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
        }

        /// <summary>
        /// Gets the SafeMode to use.
        /// </summary>
        [Obsolete("Use FSync, Journal, W and WTimeout instead.")]
        public SafeMode SafeMode
        {
            get
            {
                if (AnyWriteConcernSettingsAreSet())
                {
#pragma warning disable 618
                    return new SafeMode(GetWriteConcern(false));
#pragma warning restore
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the acceptable latency for considering a replica set member for inclusion in load balancing
        /// when using a read preference of Secondary, SecondaryPreferred, and Nearest.
        /// </summary>
        public TimeSpan SecondaryAcceptableLatency
        {
            get { return _secondaryAcceptableLatency; }
        }

        /// <summary>
        /// Gets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server
        {
            get { return (_servers == null) ? null : _servers.Single(); }
        }

        /// <summary>
        /// Gets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get { return _servers; }
        }

        /// <summary>
        /// Gets whether queries can be sent to secondary servers.
        /// </summary>
        [Obsolete("Use ReadPreference instead.")]
        public bool SlaveOk
        {
#pragma warning disable 618
            get { return _slaveOk; }
#pragma warning restore
        }

        /// <summary>
        /// Gets the socket timeout.
        /// </summary>
        public TimeSpan SocketTimeout
        {
            get { return _socketTimeout; }
        }

        /// <summary>
        /// Gets the URL (in canonical form).
        /// </summary>
        public string Url
        {
            get { return _url; }
        }

        /// <summary>
        /// Gets whether to use SSL.
        /// </summary>
        public bool UseSsl
        {
            get { return _useSsl; }
        }

        /// <summary>
        /// Gets whether to verify an SSL certificate.
        /// </summary>
        public bool VerifySslCertificate
        {
            get { return _verifySslCertificate; }
        }

        /// <summary>
        /// Gets the W component of the write concern.
        /// </summary>
        public WriteConcern.WValue W
        {
            get { return _w; }
        }

        /// <summary>
        /// Gets the wait queue multiple (the actual wait queue size will be WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public double WaitQueueMultiple
        {
            get { return _waitQueueMultiple; }
        }

        /// <summary>
        /// Gets the wait queue size.
        /// </summary>
        public int WaitQueueSize
        {
            get { return _waitQueueSize; }
        }

        /// <summary>
        /// Gets the wait queue timeout.
        /// </summary>
        public TimeSpan WaitQueueTimeout
        {
            get { return _waitQueueTimeout; }
        }

        /// <summary>
        /// Gets the WTimeout component of the write concern.
        /// </summary>
        public TimeSpan? WTimeout
        {
            get { return _wTimeout; }
        }

        // public operators
        /// <summary>
        /// Compares two MongoUrls.
        /// </summary>
        /// <param name="lhs">The first URL.</param>
        /// <param name="rhs">The other URL.</param>
        /// <returns>True if the two URLs are equal (or both null).</returns>
        public static bool operator ==(MongoUrl lhs, MongoUrl rhs)
        {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Compares two MongoUrls.
        /// </summary>
        /// <param name="lhs">The first URL.</param>
        /// <param name="rhs">The other URL.</param>
        /// <returns>True if the two URLs are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoUrl lhs, MongoUrl rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Clears the URL cache. When a URL is parsed it is stored in the cache so that it doesn't have to be
        /// parsed again. There is rarely a need to call this method.
        /// </summary>
        public static void ClearCache()
        {
            __cache.Clear();
        }

        /// <summary>
        /// Creates an instance of MongoUrl (might be an existing existence if the same URL has been used before).
        /// </summary>
        /// <param name="url">The URL containing the settings.</param>
        /// <returns>An instance of MongoUrl.</returns>
        public static MongoUrl Create(string url)
        {
            // cache previously seen urls to avoid repeated parsing
            lock (__staticLock)
            {
                MongoUrl mongoUrl;
                if (!__cache.TryGetValue(url, out mongoUrl))
                {
                    mongoUrl = new MongoUrl(url);
                    var canonicalUrl = mongoUrl.ToString();
                    if (canonicalUrl != url)
                    {
                        if (__cache.ContainsKey(canonicalUrl))
                        {
                            mongoUrl = __cache[canonicalUrl]; // use existing MongoUrl
                        }
                        else
                        {
                            __cache[canonicalUrl] = mongoUrl; // cache under canonicalUrl also
                        }
                    }
                    __cache[url] = mongoUrl;
                }
                return mongoUrl;
            }
        }

        // public methods
        /// <summary>
        /// Compares two MongoUrls.
        /// </summary>
        /// <param name="rhs">The other URL.</param>
        /// <returns>True if the two URLs are equal.</returns>
        public bool Equals(MongoUrl rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return _url == rhs._url; // this works because URL is in canonical form
        }

        /// <summary>
        /// Compares two MongoUrls.
        /// </summary>
        /// <param name="obj">The other URL.</param>
        /// <returns>True if the two URLs are equal.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoUrl); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _url.GetHashCode(); // this works because URL is in canonical form
        }

        /// <summary>
        /// Returns a WriteConcern value based on this instance's settings and a default enabled value.
        /// </summary>
        /// <param name="enabledDefault">The default enabled value.</param>
        /// <returns>A WriteConcern.</returns>
        public WriteConcern GetWriteConcern(bool enabledDefault)
        {
            return new WriteConcern(enabledDefault)
            {
                FSync = _fsync,
                Journal = _journal,
                W = _w,
                WTimeout = _wTimeout
            };
        }

        /// <summary>
        /// Creates a new instance of MongoServerSettings based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>A new instance of MongoServerSettings.</returns>
        [Obsolete("Use MongoServerSettings.FromUrl instead.")]
        public MongoServerSettings ToServerSettings()
        {
            return MongoServerSettings.FromUrl(this);
        }

        /// <summary>
        /// Returns the canonical URL based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>The canonical URL.</returns>
        public override string ToString()
        {
            return _url;
        }

        // private methods
        private bool AnyWriteConcernSettingsAreSet()
        {
            return _fsync != null || _journal != null || _w != null || _wTimeout != null;
        }
    }
}
