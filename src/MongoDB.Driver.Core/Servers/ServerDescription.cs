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
    public class ServerDescription
    {
        // fields
        private readonly TimeSpan _averageRoundTripTime;
        private readonly BuildInfoResult _buildInfoResult;
        private readonly DnsEndPoint _endPoint;
        private readonly IsMasterResult _isMasterResult;
        private readonly ReplicaSetConfig _replicaSetConfig;
        private readonly int _revision;
        private readonly ServerState _state;
        private readonly TagSet _tags;
        private readonly ServerType _type;

        // constructors
        public ServerDescription(DnsEndPoint endPoint)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _state = ServerState.Uninitialized;
            _type = ServerType.Unknown;
        }

        private ServerDescription(
            TimeSpan averageRoundTripTime,
            BuildInfoResult buildInfoResult,
            DnsEndPoint endPoint,
            IsMasterResult isMasterResult,
            ReplicaSetConfig replicaSetConfig,
            int revision,
            ServerState state,
            TagSet tags,
            ServerType type)
        {
            _averageRoundTripTime = averageRoundTripTime;
            _buildInfoResult = buildInfoResult;
            _endPoint = endPoint;
            _isMasterResult = isMasterResult;
            _replicaSetConfig = replicaSetConfig;
            _revision = revision;
            _state = state;
            _tags = tags;
            _type = type;
        }

        // properties
        public TimeSpan AverageRoundTripTime
        {
            get { return _averageRoundTripTime; }
        }

        public BuildInfoResult BuildInfoResult
        {
            get { return _buildInfoResult; }
        }

        public DnsEndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public IsMasterResult IsMasterResult
        {
            get { return _isMasterResult; }
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

        // methods
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(ServerDescription)) { return false; }
            var rhs = (ServerDescription)obj;
            return
                _averageRoundTripTime == rhs._averageRoundTripTime &&
                object.Equals(_buildInfoResult, rhs._buildInfoResult) &&
                _endPoint.Equals(rhs._endPoint) &&
                object.Equals(_isMasterResult, rhs._isMasterResult) &&
                object.Equals(_replicaSetConfig, rhs._replicaSetConfig) &&
                _state == rhs._state &&
                object.Equals(_tags, rhs._tags) &&
                _type == rhs._type; // don't include revision in comparison
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_averageRoundTripTime)
                .Hash(_buildInfoResult)
                .Hash(_endPoint)
                .Hash(_isMasterResult)
                .Hash(_replicaSetConfig)
                .Hash(_state)
                .Hash(_tags)
                .Hash(_type)
                .GetHashCode(); // don't include revision in hash code
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

        public ServerDescription WithHeartbeatInfo(IsMasterResult isMasterResult, BuildInfoResult buildInfoResult, TimeSpan averageRoundTripTime)
        {
            var state = ServerState.Connected;
            var type = isMasterResult.ServerType;
            var replicaSetConfig = isMasterResult.GetReplicaSetConfig(_endPoint.AddressFamily);
            var tags = isMasterResult.Tags;

            if (
                _state == state && 
                _type == type && 
                _averageRoundTripTime == averageRoundTripTime &&
                object.Equals(_replicaSetConfig, replicaSetConfig) && object.Equals(_tags, tags))
            {
                return this;
            }
            else
            {
                return new ServerDescription(
                    averageRoundTripTime,
                    buildInfoResult,
                    _endPoint,
                    isMasterResult,
                    replicaSetConfig,
                    0,
                    state,
                    tags,
                    type);
            }
        }

        public ServerDescription WithRevision(int value)
        {
            return _revision == value ? this : new Builder(this) { _revision = value }.Build();
        }

        public ServerDescription WithState(ServerState value)
        {
            return _state == value ? this : new Builder(this) { _state = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            private TimeSpan _averageRoundTripTime;
            private BuildInfoResult _buildInfoResult;
            private readonly DnsEndPoint _endPoint;
            private IsMasterResult _isMasterResult;
            public ReplicaSetConfig _replicaSetConfig;
            public int _revision;
            public ServerState _state;
            public TagSet _tags;
            public ServerType _type;

            // constructors
            public Builder(ServerDescription other)
            {
                _averageRoundTripTime = other._averageRoundTripTime;
                _buildInfoResult = other._buildInfoResult;
                _endPoint = other._endPoint;
                _isMasterResult = other._isMasterResult;
                _replicaSetConfig = other._replicaSetConfig;
                _revision = 0; // not copied from other
                _state = other._state;
                _tags = other._tags;
                _type = other._type;
            }

            // methods
            public ServerDescription Build()
            {
                return new ServerDescription(
                    _averageRoundTripTime,
                    _buildInfoResult,
                    _endPoint,
                    _isMasterResult,
                    _replicaSetConfig,
                    _revision,
                    _state,
                    _tags,
                    _type);
            }
        }
    }
}
