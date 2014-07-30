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
    public sealed class ConnectionId : IEquatable<ConnectionId>
    {
        // fields
        private readonly ServerId _serverId;
        private readonly ConnectionIdSource _source;
        private readonly int _value;

        // constructors
        public ConnectionId(ServerId serverId)
            : this(serverId, IdGenerator<ConnectionId>.GetNextId(), ConnectionIdSource.Driver)
        {
        }

        public ConnectionId(ServerId serverId, int value, ConnectionIdSource source)
        {
            _serverId = Ensure.IsNotNull(serverId, "serverId");
            _value = Ensure.IsGreaterThanOrEqualToZero(value, "value");
            _source = source;
        }

        // properties
        public ServerId ServerId
        {
            get { return _serverId; }
        }

        public ConnectionIdSource Source
        {
            get { return _source; }
        }

        public int Value
        {
            get { return _value; }
        }

        // methods
        public bool Equals(ConnectionId other)
        {
            if(other == null)
            {
                return false;
            }

            return _serverId.Equals(other._serverId) &&
                _source == other._source &&
                _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConnectionId);
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_serverId)
                .Hash(_source)
                .Hash(_value)
                .GetHashCode();
        }

        public override string ToString()
        {
            if (_source == ConnectionIdSource.Server)
            {
                return string.Format("{{ ServerId : {0}, Value : {1} }}", _serverId, _value);
            }
            else
            {
                return string.Format("{{ ServerId : {0}, Value : {1}, Source : \"{2}\" }}", _serverId, _value, _source);
            }
        }
    }
}
