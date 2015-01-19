/* Copyright 2013-2014 MongoDB Inc.
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
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Connections
{
    [Serializable]
    public sealed class ConnectionId : IEquatable<ConnectionId>
    {
        // fields
        private readonly ServerId _serverId;
        private readonly int _localValue;
        private readonly int? _serverValue;
        private readonly int _hashCode;

        // constructors
        public ConnectionId(ServerId serverId)
            : this(serverId, IdGenerator<ConnectionId>.GetNextId())
        {
        }

        public ConnectionId(ServerId serverId, int localValue)
        {
            _serverId = Ensure.IsNotNull(serverId, "serverId");
            _localValue = Ensure.IsGreaterThanOrEqualToZero(localValue, "localValue");
            _hashCode = new Hasher()
                .Hash(_serverId)
                .Hash(_localValue)
                .GetHashCode();
        }

        private ConnectionId(ServerId serverId, int localValue, int serverValue)
            : this(serverId, localValue)
        {
            _serverValue = Ensure.IsGreaterThanOrEqualToZero(serverValue, "serverValue");
        }

        // properties
        public ServerId ServerId
        {
            get { return _serverId; }
        }

        public int LocalValue
        {
            get { return _localValue; }
        }

        public int? ServerValue
        {
            get { return _serverValue; }
        }

        // methods
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

        public override bool Equals(object obj)
        {
            return Equals(obj as ConnectionId);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

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

        public ConnectionId WithServerValue(int serverValue)
        {
            return new ConnectionId(_serverId, _localValue, serverValue);
        }
    }
}