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
        private readonly EndPoint _endPoint;
        private readonly int _maxBatchCount;
        private readonly int _maxDocumentSize;
        private readonly int _maxMessageSize;
        private readonly int _maxWireDocumentSize;
        private readonly ReplicaSetConfig _replicaSetConfig;
        private readonly ServerId _serverId;
        private readonly ServerState _state;
        private readonly TagSet _tags;
        private readonly ServerType _type;
        private readonly SemanticVersion _version;
        private readonly Range<int> _wireVersionRange;

        // constructors
        public ServerDescription(
            ServerId serverId,
            EndPoint endPoint,
            Optional<TimeSpan> averageRoundTripTime = default(Optional<TimeSpan>),
            Optional<int> maxBatchCount = default(Optional<int>),
            Optional<int> maxDocumentSize = default(Optional<int>),
            Optional<int> maxMessageSize = default(Optional<int>),
            Optional<int> maxWireDocumentSize = default(Optional<int>),
            Optional<ReplicaSetConfig> replicaSetConfig = default(Optional<ReplicaSetConfig>),
            Optional<ServerState> state = default(Optional<ServerState>),
            Optional<TagSet> tags = default(Optional<TagSet>),
            Optional<ServerType> type = default(Optional<ServerType>),
            Optional<SemanticVersion> version = default(Optional<SemanticVersion>),
            Optional<Range<int>> wireVersionRange = default(Optional<Range<int>>))
        {
            Ensure.IsNotNull(endPoint, "endPoint");
            Ensure.IsNotNull(serverId, "serverId");
            if (!EndPointHelper.Equals(endPoint, serverId.EndPoint))
            {
                throw new ArgumentException("EndPoint and ServerId.EndPoint must match.");
            }

            _averageRoundTripTime = averageRoundTripTime.WithDefault(TimeSpan.Zero);
            _endPoint = endPoint;
            _maxBatchCount = maxBatchCount.WithDefault(1000);
            _maxDocumentSize = maxDocumentSize.WithDefault(4 * 1024 * 1024);
            _maxMessageSize = maxMessageSize.WithDefault(Math.Max(_maxDocumentSize + 1024, 16000000));
            _maxWireDocumentSize = maxWireDocumentSize.WithDefault(_maxDocumentSize + 16 * 1024);
            _replicaSetConfig = replicaSetConfig.WithDefault(null);
            _serverId = serverId;
            _state = state.WithDefault(ServerState.Disconnected);
            _tags = tags.WithDefault(null);
            _type = type.WithDefault(ServerType.Unknown);
            _version = version.WithDefault(null);
            _wireVersionRange = wireVersionRange.WithDefault(null);
        }

        // properties
        public TimeSpan AverageRoundTripTime
        {
            get { return _averageRoundTripTime; }
        }

        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public int MaxBatchCount
        {
            get { return _maxBatchCount; }
        }

        public int MaxDocumentSize
        {
            get { return _maxDocumentSize; }
        }

        public int MaxMessageSize
        {
            get { return _maxMessageSize; }
        }

        public int MaxWireDocumentSize
        {
            get { return _maxWireDocumentSize; }
        }

        public ReplicaSetConfig ReplicaSetConfig
        {
            get { return _replicaSetConfig; }
        }

        public ServerId ServerId
        {
            get { return _serverId; }
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

        public Range<int> WireVersionRange
        {
            get { return _wireVersionRange; }
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

            return
                _averageRoundTripTime == rhs._averageRoundTripTime &&
                EndPointHelper.Equals(_endPoint, rhs._endPoint) &&
                _maxBatchCount == rhs._maxBatchCount &&
                _maxDocumentSize == rhs._maxDocumentSize &&
                _maxMessageSize == rhs._maxMessageSize &&
                _maxWireDocumentSize == rhs._maxWireDocumentSize &&
                object.Equals(_replicaSetConfig, rhs._replicaSetConfig) &&
                _serverId.Equals(rhs._serverId) &&
                _state == rhs._state &&
                object.Equals(_tags, rhs._tags) &&
                _type == rhs._type &&
                object.Equals(_version, rhs._version) &&
                object.Equals(_wireVersionRange, rhs._wireVersionRange);
        }

        public override int GetHashCode()
        {
            // revision is ignored
            return new Hasher()
                .Hash(_averageRoundTripTime)
                .Hash(_endPoint)
                .Hash(_maxBatchCount)
                .Hash(_maxDocumentSize)
                .Hash(_maxMessageSize)
                .Hash(_maxWireDocumentSize)
                .Hash(_replicaSetConfig)
                .Hash(_serverId)
                .Hash(_state)
                .Hash(_tags)
                .Hash(_type)
                .Hash(_version)
                .Hash(_wireVersionRange)
                .GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(
                "{{ ServerId : {0}, EndPoint : {1}, State : {2}, Type : {3}, Tags : {4}, WireVersionRange : {5} }}",
                _serverId,
                _endPoint,
                _state,
                _type,
                _tags,
                _wireVersionRange);
        }

        public ServerDescription With(
            Optional<TimeSpan> averageRoundTripTime = default(Optional<TimeSpan>),
            Optional<int> maxBatchCount = default(Optional<int>),
            Optional<int> maxDocumentSize = default(Optional<int>),
            Optional<int> maxMessageSize = default(Optional<int>),
            Optional<int> maxWireDocumentSize = default(Optional<int>),
            Optional<ReplicaSetConfig> replicaSetConfig = default(Optional<ReplicaSetConfig>),
            Optional<ServerState> state = default(Optional<ServerState>),
            Optional<TagSet> tags = default(Optional<TagSet>),
            Optional<ServerType> type = default(Optional<ServerType>),
            Optional<SemanticVersion> version = default(Optional<SemanticVersion>),
            Optional<Range<int>> wireVersionRange = default(Optional<Range<int>>))
        {
            if (
                averageRoundTripTime.Replaces(_averageRoundTripTime) ||
                maxBatchCount.Replaces(_maxBatchCount) ||
                maxDocumentSize.Replaces(_maxDocumentSize) ||
                maxMessageSize.Replaces(_maxMessageSize) ||
                maxWireDocumentSize.Replaces(_maxWireDocumentSize) ||
                replicaSetConfig.Replaces(_replicaSetConfig) ||
                state.Replaces(_state) ||
                tags.Replaces(_tags) ||
                type.Replaces(_type) ||
                version.Replaces(_version) ||
                wireVersionRange.Replaces(_wireVersionRange))
            {
                return new ServerDescription(
                    _serverId,
                    _endPoint,
                    averageRoundTripTime: averageRoundTripTime.WithDefault(_averageRoundTripTime),
                    maxBatchCount: maxBatchCount.WithDefault(_maxBatchCount),
                    maxDocumentSize: maxDocumentSize.WithDefault(_maxDocumentSize),
                    maxMessageSize: maxMessageSize.WithDefault(_maxMessageSize),
                    maxWireDocumentSize: maxWireDocumentSize.WithDefault(_maxWireDocumentSize),
                    replicaSetConfig: replicaSetConfig.WithDefault(_replicaSetConfig),
                    state: state.WithDefault(_state),
                    tags: tags.WithDefault(_tags),
                    type: type.WithDefault(_type),
                    version: version.WithDefault(_version),
                    wireVersionRange: wireVersionRange.WithDefault(_wireVersionRange));
            }
            else
            {
                return this;
            }
        }
    }
}
