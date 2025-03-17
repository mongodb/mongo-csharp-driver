/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection identifier.
    /// </summary>
    public sealed class ConnectionId : IEquatable<ConnectionId>
    {
        // fields
        private readonly ServerId _serverId;
        private readonly long _localValue;
        private readonly long? _serverValue;
        private readonly int _hashCode;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionId"/> class.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        public ConnectionId(ServerId serverId)
            : this(serverId, LongIdGenerator<ConnectionId>.GetNextId())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionId"/> class.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="localValue">The local value.</param>
        public ConnectionId(ServerId serverId, long localValue)
        {
            _serverId = Ensure.IsNotNull(serverId, nameof(serverId));
            _localValue = Ensure.IsGreaterThanOrEqualToZero(localValue, nameof(localValue));
            _hashCode = new Hasher()
                .Hash(_serverId)
                .Hash(_localValue)
                .GetHashCode();
        }

        private ConnectionId(ServerId serverId, long localValue, long serverValue)
            : this(serverId, localValue)
        {
            _serverValue = Ensure.IsGreaterThanOrEqualToZero(serverValue, nameof(serverValue));
        }

        // properties
        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        /// <value>
        /// The server identifier.
        /// </value>
        public ServerId ServerId
        {
            get { return _serverId; }
        }

        /// <summary>
        /// Gets the local value.
        /// </summary>
        /// <value>
        /// The local value.
        /// </value>
        [Obsolete("Use LongLocalValue instead.")]
        public int LocalValue
        {
            get { return (int)_localValue; }
        }

        /// <summary>
        /// Gets the local value.
        /// </summary>
        /// <value>
        /// The local value.
        /// </value>
        public long LongLocalValue
        {
            get { return _localValue; }
        }

        /// <summary>
        /// Gets the server value.
        /// </summary>
        /// <value>
        /// The server value.
        /// </value>
        [Obsolete("Use LongServerValue instead.")]
        public int? ServerValue
        {
            get { return (int?)_serverValue; }
        }

        /// <summary>
        /// Gets the server value.
        /// </summary>
        /// <value>
        /// The server value.
        /// </value>
        public long? LongServerValue
        {
            get { return _serverValue; }
        }

        // methods
        /// <inheritdoc/>
        public bool Equals(ConnectionId other)
        {
            if (other == null)
            {
                return false;
            }

            return
                _serverId.Equals(other._serverId) &&
                _localValue == other._localValue;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ConnectionId);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Compares all fields of two ConnectionId instances (Equals ignores the ServerValue).
        /// </summary>
        /// <param name="other">The other ConnectionId.</param>
        /// <returns>True if both instances are equal.</returns>
        public bool StructurallyEquals(ConnectionId other)
        {
            if (other == null)
            {
                return false;
            }

            return
                _serverId.Equals(other._serverId) &&
                _localValue == other._localValue &&
                _serverValue == other._serverValue;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_serverValue == null)
            {
                return string.Format("{{ ServerId : {0}, LocalValue : {1} }}", _serverId, _localValue);
            }
            else
            {
                return string.Format("{{ ServerId : {0}, LocalValue : {1}, ServerValue : \"{2}\" }}", _serverId, _localValue, _serverValue);
            }
        }

        /// <summary>
        /// Returns a new instance of ConnectionId with a new server value.
        /// </summary>
        /// <param name="serverValue">The server value.</param>
        /// <returns>A ConnectionId.</returns>
        public ConnectionId WithServerValue(long serverValue)
        {
            return new ConnectionId(_serverId, _localValue, serverValue);
        }
    }
}
