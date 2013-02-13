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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents URL style connection strings. This is the recommended connection string style, but see also
    /// MongoConnectionStringBuilder if you wish to use .NET style connection strings.
    /// </summary>
    [Serializable]
    public class MongoUrlBuilder
    {
        // private fields
        private string _authenticationMechanism;
        private string _authenticationSource;
        private ConnectionMode _connectionMode;
        private TimeSpan _connectTimeout;
        private string _databaseName;
        private bool? _fsync;
        private GuidRepresentation _guidRepresentation;
        private bool _ipv6;
        private bool? _journal;
        private TimeSpan _maxConnectionIdleTime;
        private TimeSpan _maxConnectionLifeTime;
        private int _maxConnectionPoolSize;
        private int _minConnectionPoolSize;
        private string _password;
        private ReadPreference _readPreference;
        private string _replicaSetName;
        private IEnumerable<MongoServerAddress> _servers;
        private bool? _slaveOk;
        private TimeSpan _socketTimeout;
        private string _username;
        private bool _useSsl;
        private bool _verifySslCertificate;
        private WriteConcern.WValue _w;
        private double _waitQueueMultiple;
        private int _waitQueueSize;
        private TimeSpan _waitQueueTimeout;
        private TimeSpan? _wTimeout;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoUrlBuilder.
        /// </summary>
        public MongoUrlBuilder()
        {
            _authenticationMechanism = MongoDefaults.AuthenticationMechanism;
            _authenticationSource = null;
            _connectionMode = ConnectionMode.Automatic;
            _connectTimeout = MongoDefaults.ConnectTimeout;
            _databaseName = null;
            _fsync = null;
            _guidRepresentation = MongoDefaults.GuidRepresentation;
            _ipv6 = false;
            _journal = null;
            _maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            _maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            _maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            _minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            _password = null;
            _readPreference = null;
            _replicaSetName = null;
            _servers = null;
            _slaveOk = null;
            _socketTimeout = MongoDefaults.SocketTimeout;
            _username = null;
            _useSsl = false;
            _verifySslCertificate = true;
            _w = null;
            _waitQueueMultiple = MongoDefaults.WaitQueueMultiple;
            _waitQueueSize = MongoDefaults.WaitQueueSize;
            _waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
            _wTimeout = null;
        }

        /// <summary>
        /// Creates a new instance of MongoUrlBuilder.
        /// </summary>
        /// <param name="url">The initial settings.</param>
        public MongoUrlBuilder(string url)
            : this()
        {
            Parse(url);
        }

        // public properties
        /// <summary>
        /// Gets or sets the authentication mechanism.
        /// </summary>
        public string AuthenticationMechanism
        {
            get { return _authenticationMechanism; }
            set { _authenticationMechanism = value; }
        }

        /// <summary>
        /// Gets or sets the authentication source.
        /// </summary>
        public string AuthenticationSource
        {
            get { return _authenticationSource; }
            set { _authenticationSource = value; }
        }

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
            set { _connectionMode = value; }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return _connectTimeout; }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "ConnectTimeout must be greater than or equal to zero.");
                }
                _connectTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the optional database name.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = value; }
        }

        /// <summary>
        /// Gets or sets the FSync component of the write concern.
        /// </summary>
        public bool? FSync
        {
            get { return _fsync; }
            set
            {
                _fsync = value;
            }
        }

        /// <summary>
        /// Gets or sets the representation to use for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return _guidRepresentation; }
            set { _guidRepresentation = value; }
        }

        /// <summary>
        /// Gets or sets whether to use IPv6.
        /// </summary>
        public bool IPv6
        {
            get { return _ipv6; }
            set { _ipv6 = value; }
        }

        /// <summary>
        /// Gets or sets the Journal component of the write concern.
        /// </summary>
        public bool? Journal
        {
            get { return _journal; }
            set
            {
                _journal = value;
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "MaxConnectionIdleTime must be greater than or equal to zero.");
                }
                _maxConnectionIdleTime = value;
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "MaxConnectionLifeTime must be greater than or equal to zero.");
                }
                _maxConnectionLifeTime = value;
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
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "MaxConnectionPoolSize must be greater than zero.");
                }
                _maxConnectionPoolSize = value;
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
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "MinConnectionPoolSize must be greater than or equal to zero.");
                }
                _minConnectionPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set { _password = value; }
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
                if (value != null && _slaveOk.HasValue)
                {
                    throw new InvalidOperationException("ReadPreference cannot be set because SlaveOk already has a value.");
                }
                _readPreference = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
            set { _replicaSetName = value; }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
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
            set
            {
                if (value == null)
                {
                    FSync = null;
                    Journal = null;
                    W = null;
                    WTimeout = null;
                }
                else
                {
                    var writeConcern = value.WriteConcern;
                    FSync = writeConcern.FSync;
                    Journal = writeConcern.Journal;
                    W = writeConcern.W ?? (writeConcern.Enabled ? 1 : 0);
                    WTimeout = writeConcern.WTimeout;
                }
            }
        }

        /// <summary>
        /// Gets or sets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server
        {
            get { return (_servers == null) ? null : _servers.Single(); }
            set { _servers = (value == null) ? null : new[] { value }; }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get { return _servers; }
            set { _servers = value; }
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "SocketTimeout must be greater than or equal to zero.");
                }
                _socketTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        /// <summary>
        /// Gets or sets whether to use SSL.
        /// </summary>
        public bool UseSsl
        {
            get { return _useSsl; }
            set { _useSsl = value; }
        }

        /// <summary>
        /// Gets or sets whether to verify an SSL certificate.
        /// </summary>
        public bool VerifySslCertificate
        {
            get { return _verifySslCertificate; }
            set { _verifySslCertificate = value; }
        }

        /// <summary>
        /// Gets or sets the W component of the write concern.
        /// </summary>
        public WriteConcern.WValue W
        {
            get { return _w; }
            set
            {
                _w = value;
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
                if (value <= 0.0)
                {
                    throw new ArgumentOutOfRangeException("value", "WaitQueueMultiple must be greater than zero.");
                }
                _waitQueueMultiple = value;
                _waitQueueSize = 0;
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
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "WaitQueueSize must be greater than zero.");
                }
                _waitQueueSize = value;
                _waitQueueMultiple = 0.0;
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "WaitQueueTimeout must be greater than or equal to zero.");
                }
                _waitQueueTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the WTimeout component of the write concern.
        /// </summary>
        public TimeSpan? WTimeout
        {
            get { return _wTimeout; }
            set
            {
                if (value != null && value.Value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "WTimeout must be greater than or equal to zero.");
                }
                _wTimeout = value;
            }
        }

        // internal static methods
        // these helper methods are shared with MongoConnectionStringBuilder
        internal static string FormatTimeSpan(TimeSpan value)
        {
            const int msInOneSecond = 1000; // milliseconds
            const int msInOneMinute = 60 * msInOneSecond;
            const int msInOneHour = 60 * msInOneMinute;

            var ms = (int)value.TotalMilliseconds;
            if ((ms % msInOneHour) == 0)
            {
                return string.Format("{0}h", ms / msInOneHour);
            }
            else if ((ms % msInOneMinute) == 0 && ms < msInOneHour)
            {
                return string.Format("{0}m", ms / msInOneMinute);
            }
            else if ((ms % msInOneSecond) == 0 && ms < msInOneMinute)
            {
                return string.Format("{0}s", ms / msInOneSecond);
            }
            else if (ms < 1000)
            {
                return string.Format("{0}ms", ms);
            }
            else
            {
                return value.ToString();
            }
        }

        internal static ConnectionMode ParseConnectionMode(string name, string s)
        {
            try
            {
                return (ConnectionMode)Enum.Parse(typeof(ConnectionMode), s, true); // ignoreCase
            }
            catch (ArgumentException)
            {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static bool ParseBoolean(string name, string s)
        {
            try
            {
                return XmlConvert.ToBoolean(s.ToLower());
            }
            catch (FormatException)
            {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static double ParseDouble(string name, string s)
        {
            try
            {
                return XmlConvert.ToDouble(s);
            }
            catch (FormatException)
            {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static int ParseInt32(string name, string s)
        {
            try
            {
                return XmlConvert.ToInt32(s);
            }
            catch (FormatException)
            {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static ReadPreferenceMode ParseReadPreferenceMode(string name, string s)
        {
            try
            {
                return (ReadPreferenceMode)Enum.Parse(typeof(ReadPreferenceMode), s, true); // ignoreCase
            }
            catch (ArgumentException)
            {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static ReplicaSetTagSet ParseReplicaSetTagSet(string name, string s)
        {
            var tagSet = new ReplicaSetTagSet();
            foreach (var tagString in s.Split(','))
            {
                var parts = tagString.Split(':');
                if (parts.Length != 2)
                {
                    throw new FormatException(FormatMessage(name, s));
                }
                var tag = new ReplicaSetTag(parts[0].Trim(), parts[1].Trim());
                tagSet.Add(tag);
            }
            return tagSet;
        }

        internal static TimeSpan ParseTimeSpan(string name, string s)
        {
            TimeSpan result;
            if (TryParseTimeSpan(name, s, out result))
            {
                return result;
            }
            else
            {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static bool TryParseTimeSpan(string name, string s, out TimeSpan result)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(s))
            {
                name = name.ToLower();
                s = s.ToLower();
                var end = s.Length - 1;

                var multiplier = 1000; // default units are seconds
                if (name.EndsWith("ms", StringComparison.Ordinal))
                {
                    multiplier = 1;
                }
                else if (s.EndsWith("ms", StringComparison.Ordinal))
                {
                    s = s.Substring(0, s.Length - 2);
                    multiplier = 1;
                }
                else if (s[end] == 's')
                {
                    s = s.Substring(0, s.Length - 1);
                    multiplier = 1000;
                }
                else if (s[end] == 'm')
                {
                    s = s.Substring(0, s.Length - 1);
                    multiplier = 60 * 1000;
                }
                else if (s[end] == 'h')
                {
                    s = s.Substring(0, s.Length - 1);
                    multiplier = 60 * 60 * 1000;
                }
                else if (s.IndexOf(':') != -1)
                {
                    return TimeSpan.TryParse(s, out result);
                }

                try
                {
                    result = TimeSpan.FromMilliseconds(multiplier * XmlConvert.ToDouble(s));
                    return true;
                }
                catch (FormatException)
                {
                    result = default(TimeSpan);
                    return false;
                }
            }

            result = default(TimeSpan);
            return false;
        }

        // private static methods
        private static string FormatMessage(string name, string value)
        {
            return string.Format("Invalid key value pair in connection string. {0}='{1}'.", name, value);
        }

        // public methods
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
        /// Parses a URL and sets all settings to match the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        public void Parse(string url)
        {
            const string serverPattern = @"((\[[^]]+?\]|[^:,/?#]+)(:\d+)?)";
            const string pattern =
                @"^mongodb://" +
                @"((?<username>[^:]+)(:(?<password>.*?))?@)?" +
                @"(?<servers>" + serverPattern + @"(," + serverPattern + ")*)?" +
                @"(/(?<database>[^?]+)?(\?(?<query>.*))?)?$";
            Match match = Regex.Match(url, pattern);
            if (match.Success)
            {
                var usernameGroup = match.Groups["username"];
                if (usernameGroup.Success)
                {
                    _username = Uri.UnescapeDataString(usernameGroup.Value);
                }
                var passwordGroup = match.Groups["password"];
                if (passwordGroup.Success)
                {
                    _password = Uri.UnescapeDataString(passwordGroup.Value);
                }
                string servers = match.Groups["servers"].Value;
                var databaseGroup = match.Groups["database"];
                if (databaseGroup.Success)
                {
                    _databaseName = databaseGroup.Value;
                }
                string query = match.Groups["query"].Value;

                if (servers != "")
                {
                    List<MongoServerAddress> addresses = new List<MongoServerAddress>();
                    foreach (string server in servers.Split(','))
                    {
                        var address = MongoServerAddress.Parse(server);
                        addresses.Add(address);
                    }
                    _servers = addresses;
                }

                if (!string.IsNullOrEmpty(query))
                {
                    foreach (var pair in query.Split('&', ';'))
                    {
                        var parts = pair.Split('=');
                        if (parts.Length != 2)
                        {
                            throw new FormatException(string.Format("Invalid connection string '{0}'.", parts));
                        }
                        var name = parts[0];
                        var value = parts[1];

                        ReadPreference readPreference;
                        switch (name.ToLower())
                        {
                            case "authmechanism":
                                _authenticationMechanism = value;
                                break;
                            case "authsource":
                                _authenticationSource = value;
                                break;
                            case "connect":
                                ConnectionMode = ParseConnectionMode(name, value);
                                break;
                            case "connecttimeout":
                            case "connecttimeoutms":
                                ConnectTimeout = ParseTimeSpan(name, value);
                                break;
                            case "fsync":
                                FSync = ParseBoolean(name, value);
                                break;
                            case "guids":
                            case "uuidrepresentation":
                                GuidRepresentation = (GuidRepresentation)Enum.Parse(typeof(GuidRepresentation), value, true); // ignoreCase
                                break;
                            case "ipv6":
                                IPv6 = ParseBoolean(name, value);
                                break;
                            case "j":
                            case "journal":
                                Journal = ParseBoolean(name, value);
                                break;
                            case "maxidletime":
                            case "maxidletimems":
                                MaxConnectionIdleTime = ParseTimeSpan(name, value);
                                break;
                            case "maxlifetime":
                            case "maxlifetimems":
                                MaxConnectionLifeTime = ParseTimeSpan(name, value);
                                break;
                            case "maxpoolsize":
                                MaxConnectionPoolSize = ParseInt32(name, value);
                                break;
                            case "minpoolsize":
                                MinConnectionPoolSize = ParseInt32(name, value);
                                break;
                            case "readpreference":
                                readPreference = _readPreference ?? new ReadPreference();
                                readPreference.ReadPreferenceMode = ParseReadPreferenceMode(name, value);
                                ReadPreference = readPreference;
                                break;
                            case "readpreferencetags":
                                readPreference = _readPreference ?? new ReadPreference();
                                readPreference.AddTagSet(ParseReplicaSetTagSet(name, value));
                                ReadPreference = readPreference;
                                break;
                            case "replicaset":
                                ReplicaSetName = value;
                                break;
                            case "safe":
                                var safe = Convert.ToBoolean(value);
                                if (_w == null)
                                {
                                    W = safe ? 1 : 0;
                                }
                                else
                                {
                                    if (safe)
                                    {
                                        // don't overwrite existing W value unless it's 0
                                        var wCount = _w as WriteConcern.WCount;
                                        if (wCount != null && wCount.Value == 0)
                                        {
                                            W = 1;
                                        }
                                    }
                                    else
                                    {
                                        W = 0;
                                    }
                                }
                                break;
                            case "secondaryacceptablelatency":
                            case "secondaryacceptablelatencyms":
                                readPreference = _readPreference ?? new ReadPreference();
                                readPreference.SecondaryAcceptableLatency = ParseTimeSpan(name, value);
                                ReadPreference = readPreference;
                                break;
                            case "slaveok":
#pragma warning disable 618
                                SlaveOk = ParseBoolean(name, value);
#pragma warning restore
                                break;
                            case "sockettimeout":
                            case "sockettimeoutms":
                                SocketTimeout = ParseTimeSpan(name, value);
                                break;
                            case "ssl":
                                UseSsl = ParseBoolean(name, value);
                                break;
                            case "sslverifycertificate":
                                VerifySslCertificate = ParseBoolean(name, value);
                                break;
                            case "w":
                                W = WriteConcern.WValue.Parse(value);
                                break;
                            case "waitqueuemultiple":
                                WaitQueueMultiple = ParseDouble(name, value);
                                break;
                            case "waitqueuesize":
                                WaitQueueSize = ParseInt32(name, value);
                                break;
                            case "waitqueuetimeout":
                            case "waitqueuetimeoutms":
                                WaitQueueTimeout = ParseTimeSpan(name, value);
                                break;
                            case "wtimeout":
                            case "wtimeoutms":
                                WTimeout = ParseTimeSpan(name, value);
                                break;
                            default:
                                var message = string.Format("Invalid option '{0}'.", name);
                                throw new ArgumentException(message, "url");
                        }
                    }
                }
            }
            else
            {
                throw new FormatException(string.Format("Invalid connection string '{0}'.", url));
            }
        }

        /// <summary>
        /// Creates a new instance of MongoUrl based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>A new instance of MongoUrl.</returns>
        public MongoUrl ToMongoUrl()
        {
            return MongoUrl.Create(ToString());
        }

        /// <summary>
        /// Creates a new instance of MongoServerSettings based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>A new instance of MongoServerSettings.</returns>
        [Obsolete("Use ToMongoUrl and MongoServerSettings.FromUrl instead.")]
        public MongoServerSettings ToServerSettings()
        {
            return MongoServerSettings.FromUrl(this.ToMongoUrl());
        }

        /// <summary>
        /// Returns the canonical URL based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>The canonical URL.</returns>
        public override string ToString()
        {
            StringBuilder url = new StringBuilder();
            url.Append("mongodb://");
            if (!string.IsNullOrEmpty(_username))
            {
                url.Append(Uri.EscapeDataString(_username));
                if (_password != null)
                {
                    url.AppendFormat(":{0}", Uri.EscapeDataString(_password));
                }
                url.Append("@");
            }
            else if (_password != null)
            {
                // this would be weird and we really shouldn't be here...
                url.AppendFormat(":{0}@", _password);
            }
            if (_servers != null)
            {
                bool firstServer = true;
                foreach (MongoServerAddress server in _servers)
                {
                    if (!firstServer) { url.Append(","); }
                    if (server.Port == 27017)
                    {
                        url.Append(server.Host);
                    }
                    else
                    {
                        url.AppendFormat("{0}:{1}", server.Host, server.Port);
                    }
                    firstServer = false;
                }
            }
            if (_databaseName != null)
            {
                url.Append("/");
                url.Append(_databaseName);
            }
            var query = new StringBuilder();
            if (!_authenticationMechanism.Equals("MONGO-CR", StringComparison.InvariantCultureIgnoreCase))
            {
                query.AppendFormat("authMechanism={0};", _authenticationMechanism);
            }
            if (_authenticationSource != null)
            {
                query.AppendFormat("authSource={0};", _authenticationSource);
            }
            if (_ipv6)
            {
                query.AppendFormat("ipv6=true;");
            }
            if (_useSsl)
            {
                query.AppendFormat("ssl=true;");
            }
            if (!_verifySslCertificate)
            {
                query.AppendFormat("sslVerifyCertificate=false;");
            }
            if (_connectionMode != ConnectionMode.Automatic)
            {
                query.AppendFormat("connect={0};", MongoUtils.ToCamelCase(_connectionMode.ToString()));
            }
            if (!string.IsNullOrEmpty(_replicaSetName))
            {
                query.AppendFormat("replicaSet={0};", _replicaSetName);
            }
            if (_slaveOk.HasValue)
            {
                query.AppendFormat("slaveOk={0};", _slaveOk.Value ? "true" : "false"); // note: bool.ToString() returns "True" and "False"
            }
            if (_readPreference != null)
            {
                query.AppendFormat("readPreference={0};", MongoUtils.ToCamelCase(_readPreference.ReadPreferenceMode.ToString()));
                if (_readPreference.SecondaryAcceptableLatency != MongoDefaults.SecondaryAcceptableLatency)
                {
                    query.AppendFormat("secondaryAcceptableLatency={0};", FormatTimeSpan(_readPreference.SecondaryAcceptableLatency));
                }
                if (_readPreference.TagSets != null)
                {
                    foreach (var tagSet in _readPreference.TagSets)
                    {
                        query.AppendFormat("readPreferenceTags={0};", string.Join(",", tagSet.Select(t => string.Format("{0}:{1}", t.Name, t.Value)).ToArray()));
                    }
                }
            }
            if (_fsync != null)
            {
                query.AppendFormat("fsync={0};", XmlConvert.ToString(_fsync.Value));
            }
            if (_journal != null)
            {
                query.AppendFormat("journal={0};", XmlConvert.ToString(_journal.Value));
            }
            if (_w != null)
            {
                query.AppendFormat("w={0};", _w);
            }
            if (_wTimeout != null)
            {
                query.AppendFormat("wtimeout={0};", FormatTimeSpan(_wTimeout.Value));
            }
            if (_connectTimeout != MongoDefaults.ConnectTimeout)
            {
                query.AppendFormat("connectTimeout={0};", FormatTimeSpan(_connectTimeout));
            }
            if (_maxConnectionIdleTime != MongoDefaults.MaxConnectionIdleTime)
            {
                query.AppendFormat("maxIdleTime={0};", FormatTimeSpan(_maxConnectionIdleTime));
            }
            if (_maxConnectionLifeTime != MongoDefaults.MaxConnectionLifeTime)
            {
                query.AppendFormat("maxLifeTime={0};", FormatTimeSpan(_maxConnectionLifeTime));
            }
            if (_maxConnectionPoolSize != MongoDefaults.MaxConnectionPoolSize)
            {
                query.AppendFormat("maxPoolSize={0};", _maxConnectionPoolSize);
            }
            if (_minConnectionPoolSize != MongoDefaults.MinConnectionPoolSize)
            {
                query.AppendFormat("minPoolSize={0};", _minConnectionPoolSize);
            }
            if (_socketTimeout != MongoDefaults.SocketTimeout)
            {
                query.AppendFormat("socketTimeout={0};", FormatTimeSpan(_socketTimeout));
            }
            if (_waitQueueMultiple != 0.0 && _waitQueueMultiple != MongoDefaults.WaitQueueMultiple)
            {
                query.AppendFormat("waitQueueMultiple={0};", _waitQueueMultiple);
            }
            if (_waitQueueSize != 0 && _waitQueueSize != MongoDefaults.WaitQueueSize)
            {
                query.AppendFormat("waitQueueSize={0};", _waitQueueSize);
            }
            if (_waitQueueTimeout != MongoDefaults.WaitQueueTimeout)
            {
                query.AppendFormat("waitQueueTimeout={0};", FormatTimeSpan(WaitQueueTimeout));
            }
            if (_guidRepresentation != MongoDefaults.GuidRepresentation)
            {
                query.AppendFormat("uuidRepresentation={0};", (_guidRepresentation == GuidRepresentation.CSharpLegacy) ? "csharpLegacy" : MongoUtils.ToCamelCase(_guidRepresentation.ToString()));
            }
            if (query.Length != 0)
            {
                query.Length = query.Length - 1; // remove trailing ";"
                if (_databaseName == null)
                {
                    url.Append("/");
                }
                url.Append("?");
                url.Append(query.ToString());
            }
            return url.ToString();
        }

        // private methods
        private bool AnyWriteConcernSettingsAreSet()
        {
            return _fsync != null || _journal != null || _w != null || _wTimeout != null;
        }
    }
}
