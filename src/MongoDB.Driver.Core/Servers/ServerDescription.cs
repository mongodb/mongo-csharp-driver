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
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Represents information about a server.
    /// </summary>
    public sealed class ServerDescription : IEquatable<ServerDescription>
    {
        // fields
        private readonly TimeSpan _averageRoundTripTime;
        private readonly DnsEndPoint _endPoint;
        private readonly ReplicaSetConfig _replicaSetConfig;
        private readonly int _revision;
        private readonly ServerState _state;
        private readonly TagSet _tags;
        private readonly ServerType _type;
        private readonly SemanticVersion _version;

        // constructors
        public ServerDescription(DnsEndPoint endPoint)
            : this(
                TimeSpan.Zero,
                Ensure.IsNotNull(endPoint, "endPoint"),
                null,
                0,
                ServerState.Disconnected,
                null,
                ServerType.Unknown,
                null)
        {
        }

        public ServerDescription(
            DnsEndPoint endPoint,
            ServerState state,
            ServerType type,
            TimeSpan averageRoundTripTime,
            ReplicaSetConfig replicaSetConfig,
            TagSet tags,
            SemanticVersion version)
            : this(
                averageRoundTripTime,
                endPoint,
                replicaSetConfig,
                0,
                state,
                tags,
                type,
                version)
        {
        }

        private ServerDescription(
            TimeSpan averageRoundTripTime,
            DnsEndPoint endPoint,
            ReplicaSetConfig replicaSetConfig,
            int revision,
            ServerState state,
            TagSet tags,
            ServerType type,
            SemanticVersion version)
        {
            _averageRoundTripTime = averageRoundTripTime;
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _replicaSetConfig = replicaSetConfig;
            _revision = revision;
            _state = state;
            _tags = tags;
            _type = type;
            _version = version;
        }

        // properties
        public TimeSpan AverageRoundTripTime
        {
            get { return _averageRoundTripTime; }
        }

        public DnsEndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public ReplicaSetConfig ReplicaSetConfig
        {
            get { return _replicaSetConfig; }
        }

        public int Revision
        {
            get { return _revision; }
        }

        public ServerState State
        {
            get { return _state; }
        }

        public TagSet Tags
        {
            get { return _tags; }
        }

        public ServerType Type
        {
            get { return _type; }
        }

        public SemanticVersion Version
        {
            get { return _version; }
        }

        // methods
        public override bool Equals(object obj)
        {
            return Equals(obj as ServerDescription);
        }

        public bool Equals(ServerDescription rhs)
        {
            if (object.ReferenceEquals(rhs, null) || rhs.GetType() != typeof(ServerDescription))
            {
                return false;
            }

            // revision is ignored
            return
                _averageRoundTripTime == rhs._averageRoundTripTime &&
                _endPoint.Equals(rhs._endPoint) &&
                object.Equals(_replicaSetConfig, rhs._replicaSetConfig) &&
                _state == rhs._state &&
                object.Equals(_tags, rhs._tags) &&
                _type == rhs._type &&
                object.Equals(_version, rhs._version);
        }

        public override int GetHashCode()
        {
            // revision is ignored
            return new Hasher()
                .Hash(_averageRoundTripTime)
                .Hash(_endPoint)
                .Hash(_replicaSetConfig)
                .Hash(_state)
                .Hash(_tags)
                .Hash(_type)
                .Hash(_version)
                .GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(
                "{{ EndPoint : {0}, State : {1}, Type : {2}, Tags : {3}, Revision : {4} }}",
                DnsEndPointParser.ToString(_endPoint),
                _state,
                _type,
                _tags,
                _revision);
        }

        public ServerDescription WithHeartbeatInfo(
            TimeSpan averageRoundTripTime,
            ReplicaSetConfig replicaSetConfig,
            TagSet tags,
            ServerType type,
            SemanticVersion version)
        {
            if (_state == ServerState.Connected && 
                _averageRoundTripTime == averageRoundTripTime &&
                object.Equals(_replicaSetConfig, replicaSetConfig) &&                
                object.Equals(_tags, tags) &&
                _type == type &&
                object.Equals(_version, version))
            {
                return this;
            }
            else
            {
                return new ServerDescription(
                    averageRoundTripTime,
                    _endPoint,
                    replicaSetConfig,
                    0,
                    ServerState.Connected,
                    tags,
                    type,
                    version);
            }
        }

        public ServerDescription WithRevision(int value)
        {
            return _revision == value ? this : new ServerDescription(
                _averageRoundTripTime,
                _endPoint,
                _replicaSetConfig,
                value,
                _state,
                _tags,
                _type,
                _version);
        }
    }
}
