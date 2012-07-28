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
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents .NET style connection strings. We recommend you use URL style connection strings
    /// (see MongoUrl and MongoUrlBuilder).
    /// </summary>
    public class MongoConnectionStringBuilder : DbConnectionStringBuilder
    {
        // private static fields
        private static Dictionary<string, string> __canonicalKeywords = new Dictionary<string, string>
        {
            { "connect", "connect" },
            { "connecttimeout", "connectTimeout" },
            { "connecttimeoutms", "connectTimeout" },
            { "database", "database" },
            { "fsync", "fsync" },
            { "guids", "uuidRepresentation" },
            { "ipv6", "ipv6" },
            { "j", "journal" },
            { "journal", "journal" },
            { "maxidletime", "maxIdleTime" },
            { "maxidletimems", "maxIdleTime" },
            { "maxlifetime", "maxLifeTime" },
            { "maxlifetimems", "maxLifeTime" },
            { "maxpoolsize", "maxPoolSize" },
            { "minpoolsize", "minPoolSize" },
            { "password", "password" },
            { "readpreference", "readPreference" },
            { "readpreferencetags", "readPreferenceTags" },
            { "replicaset", "replicaSet" },
            { "safe", "safe" },
            { "server", "server" },
            { "servers", "server" },
            { "slaveok", "slaveOk" },
            { "sockettimeout", "socketTimeout" },
            { "sockettimeoutms", "socketTimeout" },
            { "ssl", "ssl" },
            { "sslverifycertificate", "sslVerifyCertificate" },
            { "username", "username" },
            { "uuidrepresentation", "uuidRepresentation" },
            { "w", "w" },
            { "waitqueuemultiple", "waitQueueMultiple" },
            { "waitqueuesize", "waitQueueSize" },
            { "waitqueuetimeout", "waitQueueTimeout" },
            { "waitqueuetimeoutms", "waitQueueTimeout" },
            { "wtimeout", "wtimeout" },
            { "wtimeoutms", "wtimeout" }
        };

        // private fields
        // default values are set in ResetValues
        private ConnectionMode _connectionMode;
        private TimeSpan _connectTimeout;
        private string _databaseName;
        private GuidRepresentation _guidRepresentation;
        private bool _ipv6;
        private TimeSpan _maxConnectionIdleTime;
        private TimeSpan _maxConnectionLifeTime;
        private int _maxConnectionPoolSize;
        private int _minConnectionPoolSize;
        private string _password;
        private ReadPreference _readPreference;
        private string _replicaSetName;
        private SafeMode _safeMode;
        private IEnumerable<MongoServerAddress> _servers;
        private bool? _slaveOk;
        private TimeSpan _socketTimeout;
        private string _username;
        private bool _useSsl;
        private bool _verifySslCertificate;
        private double _waitQueueMultiple;
        private int _waitQueueSize;
        private TimeSpan _waitQueueTimeout;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoConnectionStringBuilder.
        /// </summary>
        public MongoConnectionStringBuilder()
            : base()
        {
            ResetValues();
        }

        /// <summary>
        /// Creates a new instance of MongoConnectionStringBuilder.
        /// </summary>
        /// <param name="connectionString">The initial settings.</param>
        public MongoConnectionStringBuilder(string connectionString)
        {
            ConnectionString = connectionString; // base class calls Clear which calls ResetValues
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
        /// Gets or sets the connection mode.
        /// </summary>
        public ConnectionMode ConnectionMode
        {
            get { return _connectionMode; }
            set
            {
                _connectionMode = value;
                base["connect"] = MongoUtils.ToCamelCase(value.ToString());
            }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return _connectTimeout; }
            set
            {
                _connectTimeout = value;
                base["connectTimeout"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        /// <summary>
        /// Gets or sets the optional database name.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
            set
            {
                base["database"] = _databaseName = value;
            }
        }

        /// <summary>
        /// Gets or sets the representation for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return _guidRepresentation; }
            set
            {
                base["uuidRepresentation"] = _guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to use IPv6.
        /// </summary>
        public bool IPv6
        {
            get { return _ipv6; }
            set
            {
                _ipv6 = value;
                base["ipv6"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets the max connection idle time.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime
        {
            get { return _maxConnectionIdleTime; }
            set
            {
                _maxConnectionIdleTime = value;
                base["maxIdleTime"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        /// <summary>
        /// Gets or sets the max connection life time.
        /// </summary>
        public TimeSpan MaxConnectionLifeTime
        {
            get { return _maxConnectionLifeTime; }
            set
            {
                _maxConnectionLifeTime = value;
                base["maxLifeTime"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        public int MaxConnectionPoolSize
        {
            get { return _maxConnectionPoolSize; }
            set
            {
                _maxConnectionPoolSize = value;
                base["maxPoolSize"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets the min connection pool size.
        /// </summary>
        public int MinConnectionPoolSize
        {
            get { return _minConnectionPoolSize; }
            set
            {
                _minConnectionPoolSize = value;
                base["minPoolSize"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets the default password.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set
            {
                base["password"] = _password = value;
            }
        }

        /// <summary>
        /// Gets or sets the read preference.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get
            {
                if (_readPreference != null)
                {
                    return _readPreference;
                }
                else if (_slaveOk.HasValue)
                {
                    return ReadPreference.FromSlaveOk(_slaveOk.Value);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (_slaveOk.HasValue)
                {
                    throw new InvalidOperationException("ReadPreference cannot be set because SlaveOk already has a value.");
                }
                _readPreference = value;

                base["readPreference"] = MongoUtils.ToCamelCase(_readPreference.ReadPreferenceMode.ToString());
                if (_readPreference.TagSets == null)
                {
                    base["readPreferenceTags"] = null;
                }
                else
                {
                    var readPreferenceTagsString = string.Join(
                        "|",
                        _readPreference.TagSets.Select(ts => string.Join(
                            ",",
                            ts.Tags.Select(t => string.Format("{0}:{1}", t.Name, t.Value)).ToArray()
                        )).ToArray()
                    );
                    base["readPreferenceTags"] = readPreferenceTagsString;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
            set
            {
                base["replicaSet"] = _replicaSetName = value;
            }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode
        {
            get { return _safeMode; }
            set
            {
                _safeMode = value;

                if (value == null)
                {
                    base["safe"] = null;
                    base["fsync"] = null;
                    base["journal"] = null;
                    base["w"] = null;
                    base["wtimeout"] = null;
                }
                else
                {
                    if (value.Enabled)
                    {
                        base["safe"] = "true";
                        base["fsync"] = (value.FSync) ? "true" : null;
                        base["journal"] = (value.Journal) ? "true" : null;
                        base["w"] = (value.W != 0) ? value.W.ToString() : (value.WMode != null) ? value.WMode : null;
                        base["wtimeout"] = (value.W != 0 && value.WTimeout != TimeSpan.Zero) ? MongoUrlBuilder.FormatTimeSpan(value.WTimeout) : null;
                    }
                    else
                    {
                        base["safe"] = "false";
                        base["fsync"] = null;
                        base["journal"] = null;
                        base["w"] = null;
                        base["wtimeout"] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server
        {
            get { return (_servers == null) ? null : _servers.Single(); }
            set
            {
                Servers = new[] { value };
            }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get { return _servers; }
            set
            {
                _servers = value;
                base["server"] = GetServersString();
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        [Obsolete("Use ReadPreference instead.")]
        public bool SlaveOk
        {
            get
            {
                if (_slaveOk.HasValue)
                {
                    return _slaveOk.Value;
                }
                else if (_readPreference != null)
                {
                    return _readPreference.ToSlaveOk();
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (_readPreference != null)
                {
                    throw new InvalidOperationException("SlaveOk cannot be set because ReadPreference already has a value.");
                }
                _slaveOk = value;
                base["slaveOk"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets the socket timeout.
        /// </summary>
        public TimeSpan SocketTimeout
        {
            get { return _socketTimeout; }
            set
            {
                _socketTimeout = value;
                base["socketTimeout"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        /// <summary>
        /// Gets or sets the default username.
        /// </summary>
        public string Username
        {
            get { return _username; }
            set
            {
                base["username"] = _username = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to use SSL.
        /// </summary>
        public bool UseSsl
        {
            get { return _useSsl; }
            set
            {
                _useSsl = value;
                base["ssl"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets whether to verify an SSL certificate.
        /// </summary>
        public bool VerifySslCertificate
        {
            get { return _verifySslCertificate; }
            set
            {
                _verifySslCertificate = value;
                base["sslVerifyCertificate"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets the wait queue multiple (the actual wait queue size will be WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public double WaitQueueMultiple
        {
            get { return _waitQueueMultiple; }
            set
            {
                _waitQueueMultiple = value;
                _waitQueueSize = 0;
                base["waitQueueMultiple"] = (value != 0.0) ? XmlConvert.ToString(value) : null;
                base["waitQueueSize"] = null;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue size.
        /// </summary>
        public int WaitQueueSize
        {
            get { return _waitQueueSize; }
            set
            {
                _waitQueueSize = value;
                _waitQueueMultiple = 0.0;
                base["waitQueueSize"] = (value != 0) ? XmlConvert.ToString(value) : null;
                base["waitQueueMultiple"] = null;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue timeout.
        /// </summary>
        public TimeSpan WaitQueueTimeout
        {
            get { return _waitQueueTimeout; }
            set
            {
                _waitQueueTimeout = value;
                base["waitQueueTimeout"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        // public indexers
        /// <summary>
        /// Gets or sets individual settings by keyword.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns>The value of the setting.</returns>
        public override object this[string keyword]
        {
            get
            {
                if (keyword == null) { throw new ArgumentNullException("keyword"); }
                return base[__canonicalKeywords[keyword.ToLower()]];
            }
            set
            {
                if (keyword == null) { throw new ArgumentNullException("keyword"); }
                ReadPreference newReadPreference;
                SafeMode newSafeMode;
                switch (keyword.ToLower())
                {
                    case "connect":
                        if (value is string)
                        {
                            ConnectionMode = (ConnectionMode)Enum.Parse(typeof(ConnectionMode), (string)value, true); // ignoreCase
                        }
                        else
                        {
                            ConnectionMode = (ConnectionMode)value;
                        }
                        break;
                    case "connecttimeout":
                    case "connecttimeoutms":
                        ConnectTimeout = ToTimeSpan(keyword, value);
                        break;
                    case "database":
                        DatabaseName = (string)value;
                        break;
                    case "fsync":
                        newSafeMode = _safeMode ?? new SafeMode(false);
                        newSafeMode.FSync = Convert.ToBoolean(value);
                        SafeMode = newSafeMode;
                        break;
                    case "guids":
                    case "uuidrepresentation":
                        GuidRepresentation = (GuidRepresentation)Enum.Parse(typeof(GuidRepresentation), (string)value, true); // ignoreCase
                        break;
                    case "ipv6":
                        IPv6 = Convert.ToBoolean(value);
                        break;
                    case "j":
                    case "journal":
                        newSafeMode = _safeMode ?? new SafeMode(false);
                        newSafeMode.Journal = Convert.ToBoolean(value);
                        SafeMode = newSafeMode;
                        break;
                    case "maxidletime":
                    case "maxidletimems":
                        MaxConnectionIdleTime = ToTimeSpan(keyword, value);
                        break;
                    case "maxlifetime":
                    case "maxlifetimems":
                        MaxConnectionLifeTime = ToTimeSpan(keyword, value);
                        break;
                    case "maxpoolsize":
                        MaxConnectionPoolSize = Convert.ToInt32(value);
                        break;
                    case "minpoolsize":
                        MinConnectionPoolSize = Convert.ToInt32(value);
                        break;
                    case "password":
                        Password = (string)value;
                        break;
                    case "replicaset":
                        ReplicaSetName = (string)value;
                        break;
                    case "readpreference":
                        newReadPreference = _readPreference ?? new ReadPreference();
                        if (value is string)
                        {
                            newReadPreference.ReadPreferenceMode = (ReadPreferenceMode)Enum.Parse(typeof(ReadPreferenceMode), (string)value, true); // ignoreCase
                        }
                        else
                        {
                            newReadPreference.ReadPreferenceMode = (ReadPreferenceMode)value;
                        }
                        ReadPreference = newReadPreference;
                        break;
                    case "readpreferencetags":
                        newReadPreference = _readPreference ?? new ReadPreference();
                        if (value is string)
                        {
                            newReadPreference.TagSets = ParseReplicaSetTagSets((string)value);
                        }
                        else
                        {
                            newReadPreference.TagSets = (IEnumerable<ReplicaSetTagSet>)value;
                        }
                        ReadPreference = newReadPreference;
                        break;
                    case "safe":
                        newSafeMode = _safeMode ?? new SafeMode(false);
                        newSafeMode.Enabled = Convert.ToBoolean(value);
                        SafeMode = newSafeMode;
                        break;
                    case "server":
                    case "servers":
                        Servers = ParseServersString((string)value);
                        break;
                    case "slaveok":
#pragma warning disable 618
                        SlaveOk = Convert.ToBoolean(value);
#pragma warning restore
                        break;
                    case "sockettimeout":
                    case "sockettimeoutms":
                        SocketTimeout = ToTimeSpan(keyword, value);
                        break;
                    case "ssl":
                        UseSsl = Convert.ToBoolean(value);
                        break;
                    case "sslverifycertificate":
                        VerifySslCertificate = Convert.ToBoolean(value);
                        break;
                    case "username":
                        Username = (string)value;
                        break;
                    case "w":
                        newSafeMode = _safeMode ?? new SafeMode(false);
                        try
                        {
                            newSafeMode.W = Convert.ToInt32(value);
                        }
                        catch (FormatException)
                        {
                            newSafeMode.WMode = (string)value;
                        }
                        SafeMode = newSafeMode;
                        break;
                    case "waitqueuemultiple":
                        WaitQueueMultiple = Convert.ToDouble(value);
                        break;
                    case "waitqueuesize":
                        WaitQueueSize = Convert.ToInt32(value);
                        break;
                    case "waitqueuetimeout":
                    case "waitqueuetimeoutms":
                        WaitQueueTimeout = ToTimeSpan(keyword, value);
                        break;
                    case "wtimeout":
                    case "wtimeoutms":
                        newSafeMode = _safeMode ?? new SafeMode(false);
                        newSafeMode.WTimeout = ToTimeSpan(keyword, value);
                        SafeMode = newSafeMode;
                        break;
                    default:
                        var message = string.Format("Invalid keyword '{0}'.", keyword);
                        throw new ArgumentException(message);
                }
            }
        }

        // public methods
        /// <summary>
        /// Clears all settings to their default values.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            ResetValues();
        }

        /// <summary>
        /// Tests whether a keyword is valid.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns>True if the keyword is valid.</returns>
        public override bool ContainsKey(string keyword)
        {
            return __canonicalKeywords.ContainsKey(keyword.ToLower());
        }

        /// <summary>
        /// Creates a new instance of MongoServerSettings based on the settings in this MongoConnectionStringBuilder.
        /// </summary>
        /// <returns>A new instance of MongoServerSettings.</returns>
        public MongoServerSettings ToServerSettings()
        {
            var defaultCredentials = MongoCredentials.Create(_username, _password);
            var readPreference = ReadPreference ?? ReadPreference.Primary;
            return new MongoServerSettings(_connectionMode, _connectTimeout, null, defaultCredentials, _guidRepresentation, _ipv6,
                _maxConnectionIdleTime, _maxConnectionLifeTime, _maxConnectionPoolSize, _minConnectionPoolSize, readPreference, _replicaSetName,
                _safeMode ?? MongoDefaults.SafeMode, _servers, _socketTimeout, _useSsl, _verifySslCertificate, ComputedWaitQueueSize, _waitQueueTimeout);
        }

        // private methods
        private string GetServersString()
        {
            var sb = new StringBuilder();
            foreach (var server in _servers)
            {
                if (sb.Length > 0) { sb.Append(","); }
                if (server.Port == 27017)
                {
                    sb.Append(server.Host);
                }
                else
                {
                    sb.AppendFormat("{0}:{1}", server.Host, server.Port);
                }
            }
            return sb.ToString();
        }

        private IEnumerable<ReplicaSetTagSet> ParseReplicaSetTagSets(string value)
        {
            var tagSets = new List<ReplicaSetTagSet>();
            foreach (var tagSetString in value.Split('|'))
            {
                var tagSet = new ReplicaSetTagSet();
                foreach (var tagString in tagSetString.Split(','))
                {
                    var parts = tagString.Split(':');
                    if (parts.Length != 2)
                    {
                        var message = string.Format("Invalid tag: {0}.", tagString);
                    }
                    var tag = new ReplicaSetTag(parts[0], parts[1]);
                    tagSet.Add(tag);
                }
                tagSets.Add(tagSet);
            }
            return tagSets;
        }

        private IEnumerable<MongoServerAddress> ParseServersString(string value)
        {
            var servers = new List<MongoServerAddress>();
            foreach (var server in value.Split(','))
            {
                servers.Add(MongoServerAddress.Parse(server));
            }
            return servers;
        }

        private void ResetValues()
        {
            // set fields and not properties so base class items aren't set
            _connectionMode = ConnectionMode.Automatic;
            _connectTimeout = MongoDefaults.ConnectTimeout;
            _databaseName = null;
            _guidRepresentation = MongoDefaults.GuidRepresentation;
            _ipv6 = false;
            _maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            _maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            _maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            _minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            _password = null;
            _readPreference = null;
            _replicaSetName = null;
            _safeMode = null;
            _servers = null;
            _slaveOk = null;
            _socketTimeout = MongoDefaults.SocketTimeout;
            _username = null;
            _useSsl = false;
            _verifySslCertificate = true;
            _waitQueueMultiple = MongoDefaults.WaitQueueMultiple;
            _waitQueueSize = MongoDefaults.WaitQueueSize;
            _waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
        }

        private TimeSpan ToTimeSpan(string keyword, object value)
        {
            if (value is TimeSpan)
            {
                return (TimeSpan)value;
            }
            else if (value is string)
            {
                return MongoUrlBuilder.ParseTimeSpan(keyword, (string)value);
            }
            else
            {
                return TimeSpan.FromSeconds(Convert.ToDouble(value));
            }
        }
    }
}
