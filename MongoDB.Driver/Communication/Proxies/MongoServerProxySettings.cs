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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using MongoDB.Shared;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// The settings used by a MongoServerProxy.
    /// </summary>
    public class MongoServerProxySettings : IEquatable<MongoServerProxySettings>
    {
        // private fields
        private ConnectionMode _connectionMode;
        private TimeSpan _connectTimeout;
        private IEnumerable<MongoCredential> _credentials;
        private bool _ipv6;
        private TimeSpan _maxConnectionIdleTime;
        private TimeSpan _maxConnectionLifeTime;
        private int _maxConnectionPoolSize;
        private int _minConnectionPoolSize;
        private string _replicaSetName;
        private List<MongoServerAddress> _servers;
        private TimeSpan _socketTimeout;
        private SslSettings _sslSettings;
        private bool _useSsl;
        private bool _verifySslCertificate;
        private int _waitQueueSize;
        private TimeSpan _waitQueueTimeout;
        private int _hashCode;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoServerProxySettings.
        /// </summary>
        public MongoServerProxySettings(
            ConnectionMode connectionMode,
            TimeSpan connectTimeout,
            IEnumerable<MongoCredential> credentials,
            bool ipv6,
            TimeSpan maxConnectionIdleTime,
            TimeSpan maxConnectionLifeTime,
            int maxConnectionPoolSize,
            int minConnectionPoolSize,
            string replicaSetName,
            List<MongoServerAddress> servers,
            TimeSpan socketTimeout,
            SslSettings sslSettings,
            bool useSsl,
            bool verifySslCertificate,
            int waitQueueSize,
            TimeSpan waitQueueTimeout
        )
        {
            _connectionMode = connectionMode;
            _connectTimeout = connectTimeout;
            _credentials = credentials;
            _ipv6 = ipv6;
            _maxConnectionIdleTime = maxConnectionIdleTime;
            _maxConnectionLifeTime = maxConnectionLifeTime;
            _maxConnectionPoolSize = maxConnectionPoolSize;
            _minConnectionPoolSize = minConnectionPoolSize;
            _replicaSetName = replicaSetName;
            _servers = servers;
            _socketTimeout = socketTimeout;
            _sslSettings = sslSettings;
            _useSsl = useSsl;
            _verifySslCertificate = verifySslCertificate;
            _waitQueueSize = waitQueueSize;
            _waitQueueTimeout = waitQueueTimeout;
            _hashCode = CalculateHashCode(); // all fields must be assigned before calling CalculateHashCode
        }

        // public properties
        /// <summary>
        /// Gets or sets the connection mode.
        /// </summary>
        public ConnectionMode ConnectionMode
        {
            get { return _connectionMode; }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return _connectTimeout; }
        }

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        public IEnumerable<MongoCredential> Credentials
        {
            get { return _credentials; }
        }

        /// <summary>
        /// Gets or sets whether to use IPv6.
        /// </summary>
        public bool IPv6
        {
            get { return _ipv6; }
        }

        /// <summary>
        /// Gets or sets the max connection idle time.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime
        {
            get { return _maxConnectionIdleTime; }
        }

        /// <summary>
        /// Gets or sets the max connection life time.
        /// </summary>
        public TimeSpan MaxConnectionLifeTime
        {
            get { return _maxConnectionLifeTime; }
        }

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        public int MaxConnectionPoolSize
        {
            get { return _maxConnectionPoolSize; }
        }

        /// <summary>
        /// Gets or sets the min connection pool size.
        /// </summary>
        public int MinConnectionPoolSize
        {
            get { return _minConnectionPoolSize; }
        }

        /// <summary>
        /// Gets or sets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get { return new ReadOnlyCollection<MongoServerAddress>(_servers); }
        }

        /// <summary>
        /// Gets or sets the socket timeout.
        /// </summary>
        public TimeSpan SocketTimeout
        {
            get { return _socketTimeout; }
        }

        /// <summary>
        /// Gets or sets the SSL settings.
        /// </summary>
        public SslSettings SslSettings
        {
            get { return _sslSettings; }
        }

        /// <summary>
        /// Gets or sets whether to use SSL.
        /// </summary>
        public bool UseSsl
        {
            get { return _useSsl; }
        }

        /// <summary>
        /// Gets or sets whether to verify an SSL certificate.
        /// </summary>
        public bool VerifySslCertificate
        {
            get { return _verifySslCertificate; }
        }

        /// <summary>
        /// Gets or sets the wait queue size.
        /// </summary>
        public int WaitQueueSize
        {
            get { return _waitQueueSize; }
        }

        /// <summary>
        /// Gets or sets the wait queue timeout.
        /// </summary>
        public TimeSpan WaitQueueTimeout
        {
            get { return _waitQueueTimeout; }
        }

        // public operators
        /// <summary>
        /// Determines whether two <see cref="MongoServerProxySettings"/> instances are equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(MongoServerProxySettings lhs, MongoServerProxySettings rhs)
        {
            return object.Equals(lhs, rhs); // handles lhs == null correctly
        }

        /// <summary>
        /// Determines whether two <see cref="MongoServerProxySettings"/> instances are not equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is not equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(MongoServerProxySettings lhs, MongoServerProxySettings rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Determines whether the specified <see cref="MongoServerProxySettings" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="MongoServerProxySettings" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="MongoServerProxySettings" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(MongoServerProxySettings obj)
        {
            return Equals((object)obj); // handles obj == null correctly
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || GetType() != obj.GetType()) { return false; }
            var rhs = (MongoServerProxySettings)obj;
            return
               _connectionMode == rhs._connectionMode &&
               _connectTimeout == rhs._connectTimeout &&
               _credentials == rhs._credentials &&
               _ipv6 == rhs._ipv6 &&
               _maxConnectionIdleTime == rhs._maxConnectionIdleTime &&
               _maxConnectionLifeTime == rhs._maxConnectionLifeTime &&
               _maxConnectionPoolSize == rhs._maxConnectionPoolSize &&
               _minConnectionPoolSize == rhs._minConnectionPoolSize &&
               _replicaSetName == rhs._replicaSetName &&
               _servers.SequenceEqual(rhs._servers) &&
               _socketTimeout == rhs._socketTimeout &&
               _sslSettings == rhs._sslSettings &&
               _useSsl == rhs._useSsl &&
               _verifySslCertificate == rhs._verifySslCertificate &&
               _waitQueueSize == rhs._waitQueueSize &&
               _waitQueueTimeout == rhs._waitQueueTimeout;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("ConnectionMode={0};", _connectionMode);
            sb.AppendFormat("ConnectTimeout={0};", _connectTimeout);
            sb.AppendFormat("Credentials={{{0}}};", _credentials);
            sb.AppendFormat("IPv6={0};", _ipv6);
            sb.AppendFormat("MaxConnectionIdleTime={0};", _maxConnectionIdleTime);
            sb.AppendFormat("MaxConnectionLifeTime={0};", _maxConnectionLifeTime);
            sb.AppendFormat("MaxConnectionPoolSize={0};", _maxConnectionPoolSize);
            sb.AppendFormat("MinConnectionPoolSize={0};", _minConnectionPoolSize);
            sb.AppendFormat("ReplicaSetName={0};", _replicaSetName);
            sb.AppendFormat("Servers={0};", string.Join(",", _servers.Select(s => s.ToString()).ToArray()));
            sb.AppendFormat("SocketTimeout={0};", _socketTimeout);
            if (_sslSettings != null)
            {
                sb.AppendFormat("SslSettings={0}", _sslSettings);
            }
            sb.AppendFormat("Ssl={0};", _useSsl);
            sb.AppendFormat("SslVerifyCertificate={0};", _verifySslCertificate);
            sb.AppendFormat("WaitQueueSize={0};", _waitQueueSize);
            sb.AppendFormat("WaitQueueTimeout={0}", _waitQueueTimeout);
            return sb.ToString();
        }

        // private methods
        private int CalculateHashCode()
        {
            return new Hasher()
                .Hash(_connectionMode)
                .Hash(_connectTimeout)
                .Hash(_credentials)
                .Hash(_ipv6)
                .Hash(_maxConnectionIdleTime)
                .Hash(_maxConnectionLifeTime)
                .Hash(_maxConnectionPoolSize)
                .Hash(_minConnectionPoolSize)
                .Hash(_replicaSetName)
                .HashElements(_servers)
                .Hash(_socketTimeout)
                .Hash(_sslSettings)
                .Hash(_useSsl)
                .Hash(_verifySslCertificate)
                .Hash(_waitQueueSize)
                .Hash(_waitQueueTimeout)
                .GetHashCode();
        }
    }
}
