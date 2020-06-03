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
using System.Net;
using System.Text;
using MongoDB.Driver.Core.Clusters;
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
        private readonly EndPoint _canonicalEndPoint;
        private readonly ElectionId _electionId;
        private readonly EndPoint _endPoint;
        private readonly Exception _heartbeatException;
        private readonly TimeSpan _heartbeatInterval;
        private readonly DateTime? _lastHeartbeatTimestamp;
        private readonly DateTime _lastUpdateTimestamp;
        private readonly DateTime? _lastWriteTimestamp;
        private readonly TimeSpan? _logicalSessionTimeout;
        private readonly int _maxBatchCount;
        private readonly int _maxDocumentSize;
        private readonly int _maxMessageSize;
        private readonly int _maxWireDocumentSize;
        private readonly string _reasonChanged;
        private readonly ReplicaSetConfig _replicaSetConfig;
        private readonly ServerId _serverId;
        private readonly ServerState _state;
        private readonly TagSet _tags;
        private readonly TopologyVersion _topologyVersion;
        private readonly ServerType _type;
        private readonly SemanticVersion _version;
        private readonly Range<int> _wireVersionRange;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerDescription" /> class.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="endPoint">The end point.</param>
        /// <param name="reasonChanged">The reason the server description was last changed.</param>
        /// <param name="averageRoundTripTime">The average round trip time.</param>
        /// <param name="canonicalEndPoint">The canonical end point.</param>
        /// <param name="electionId">The election identifier.</param>
        /// <param name="heartbeatException">The heartbeat exception.</param>
        /// <param name="heartbeatInterval">The heartbeat interval.</param>
        /// <param name="lastHeartbeatTimestamp">The last heartbeat timestamp.</param>
        /// <param name="lastUpdateTimestamp">The last update timestamp.</param>
        /// <param name="lastWriteTimestamp">The last write timestamp.</param>
        /// <param name="logicalSessionTimeout">The logical session timeout.</param>
        /// <param name="maxBatchCount">The maximum batch count.</param>
        /// <param name="maxDocumentSize">The maximum size of a document.</param>
        /// <param name="maxMessageSize">The maximum size of a message.</param>
        /// <param name="maxWireDocumentSize">The maximum size of a wire document.</param>
        /// <param name="replicaSetConfig">The replica set configuration.</param>
        /// <param name="state">The server state.</param>
        /// <param name="tags">The replica set tags.</param>
        /// <param name="topologyVersion">The topology version.</param>
        /// <param name="type">The server type.</param>
        /// <param name="version">The server version.</param>
        /// <param name="wireVersionRange">The wire version range.</param>
        /// <exception cref="ArgumentException">EndPoint and ServerId.EndPoint must match.</exception>
        public ServerDescription(
            ServerId serverId,
            EndPoint endPoint,
            Optional<string> reasonChanged = default(Optional<string>),
            Optional<TimeSpan> averageRoundTripTime = default(Optional<TimeSpan>),
            Optional<EndPoint> canonicalEndPoint = default(Optional<EndPoint>),
            Optional<ElectionId> electionId = default(Optional<ElectionId>),
            Optional<Exception> heartbeatException = default(Optional<Exception>),
            Optional<TimeSpan> heartbeatInterval = default(Optional<TimeSpan>),
            Optional<DateTime?> lastHeartbeatTimestamp = default(Optional<DateTime?>),
            Optional<DateTime> lastUpdateTimestamp = default(Optional<DateTime>),
            Optional<DateTime?> lastWriteTimestamp = default(Optional<DateTime?>),
            Optional<TimeSpan?> logicalSessionTimeout = default(Optional<TimeSpan?>),
            Optional<int> maxBatchCount = default(Optional<int>),
            Optional<int> maxDocumentSize = default(Optional<int>),
            Optional<int> maxMessageSize = default(Optional<int>),
            Optional<int> maxWireDocumentSize = default(Optional<int>),
            Optional<ReplicaSetConfig> replicaSetConfig = default(Optional<ReplicaSetConfig>),
            Optional<ServerState> state = default(Optional<ServerState>),
            Optional<TagSet> tags = default(Optional<TagSet>),
            Optional<TopologyVersion> topologyVersion = default(Optional<TopologyVersion>),
            Optional<ServerType> type = default(Optional<ServerType>),
            Optional<SemanticVersion> version = default(Optional<SemanticVersion>),
            Optional<Range<int>> wireVersionRange = default(Optional<Range<int>>))
        {
            Ensure.IsNotNull(endPoint, nameof(endPoint));
            Ensure.IsNotNull(serverId, nameof(serverId));
            if (!EndPointHelper.Equals(endPoint, serverId.EndPoint))
            {
                throw new ArgumentException("EndPoint and ServerId.EndPoint must match.");
            }

            _averageRoundTripTime = averageRoundTripTime.WithDefault(TimeSpan.Zero);
            _canonicalEndPoint = canonicalEndPoint.WithDefault(null);
            _electionId = electionId.WithDefault(null);
            _endPoint = endPoint;
            _heartbeatException = heartbeatException.WithDefault(null);
            _heartbeatInterval = heartbeatInterval.WithDefault(TimeSpan.Zero);
            _lastHeartbeatTimestamp = lastHeartbeatTimestamp.WithDefault(null);
            _lastUpdateTimestamp = lastUpdateTimestamp.WithDefault(DateTime.UtcNow);
            _lastWriteTimestamp = lastWriteTimestamp.WithDefault(null);
            _logicalSessionTimeout = logicalSessionTimeout.WithDefault(null);
            _maxBatchCount = maxBatchCount.WithDefault(1000);
            _maxDocumentSize = maxDocumentSize.WithDefault(4 * 1024 * 1024);
            _maxMessageSize = maxMessageSize.WithDefault(Math.Max(_maxDocumentSize + 1024, 16000000));
            _maxWireDocumentSize = maxWireDocumentSize.WithDefault(_maxDocumentSize + 16 * 1024);
            _reasonChanged = reasonChanged.WithDefault("NotSpecified");
            _replicaSetConfig = replicaSetConfig.WithDefault(null);
            _serverId = serverId;
            _state = state.WithDefault(ServerState.Disconnected);
            _tags = tags.WithDefault(null);
            _topologyVersion = topologyVersion.WithDefault(null);
            _type = type.WithDefault(ServerType.Unknown);
            _version = version.WithDefault(null);
            _wireVersionRange = wireVersionRange.WithDefault(null);
        }

        // properties
        /// <summary>
        /// Gets the average round trip time.
        /// </summary>
        /// <value>
        /// The average round trip time.
        /// </value>
        public TimeSpan AverageRoundTripTime
        {
            get { return _averageRoundTripTime; }
        }

        /// <summary>
        /// Gets the canonical end point. This is the endpoint that the cluster knows this
        /// server by. Currently, it only applies to a replica set config and will match
        /// what is in the replica set configuration.
        /// </summary>
        public EndPoint CanonicalEndPoint
        {
            get { return _canonicalEndPoint; }
        }

        /// <summary>
        /// Gets the election identifier.
        /// </summary>
        public ElectionId ElectionId
        {
            get { return _electionId; }
        }

        /// <summary>
        /// Gets the end point.
        /// </summary>
        /// <value>
        /// The end point.
        /// </value>
        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        /// <summary>
        /// Gets the most recent heartbeat exception.
        /// </summary>
        /// <value>
        /// The the most recent heartbeat exception (null if the most recent heartbeat succeeded).
        /// </value>
        public Exception HeartbeatException
        {
            get { return _heartbeatException; }
        }

        /// <summary>
        /// Gets the heartbeat interval.
        /// </summary>
        /// <value>
        /// The heartbeat interval.
        /// </value>
        public TimeSpan HeartbeatInterval
        {
            get { return _heartbeatInterval; }
        }

        /// <summary>
        /// Gets a value indicating whether this server is compatible with the driver.
        /// </summary>
        /// <value>
        /// <c>true</c> if this server is compatible with the driver; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompatibleWithDriver =>
            _type == ServerType.Unknown ||
            _wireVersionRange == null ||
            _wireVersionRange.Overlaps(Cluster.SupportedWireVersionRange);

        /// <summary>
        /// Gets a value indicating whether this instance is a data bearing server.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is a data bearing server; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataBearing
        {
            get
            {
                switch (_type)
                {
                    case ServerType.Standalone:
                    case ServerType.ReplicaSetPrimary:
                    case ServerType.ReplicaSetSecondary:
                    case ServerType.ShardRouter:
                        return true;

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Gets the last heartbeat timestamp.
        /// </summary>
        /// <value>
        /// The last heartbeat timestamp.
        /// </value>
        public DateTime? LastHeartbeatTimestamp
        {
            get { return _lastHeartbeatTimestamp; }
        }

        /// <summary>
        /// Gets the last update timestamp (when the ServerDescription itself was last updated).
        /// </summary>
        /// <value>
        /// The last update timestamp.
        /// </value>
        public DateTime LastUpdateTimestamp
        {
            get { return _lastUpdateTimestamp; }
        }

        /// <summary>
        /// Gets the last write timestamp (from the lastWrite field of the isMaster result).
        /// </summary>
        /// <value>
        /// The last write timestamp.
        /// </value>
        public DateTime? LastWriteTimestamp
        {
            get { return _lastWriteTimestamp; }
        }

        /// <summary>
        /// Gets the logical session timeout.
        /// </summary>
        /// <value>
        /// The logical session timeout.
        /// </value>
        public TimeSpan? LogicalSessionTimeout
        {
            get { return _logicalSessionTimeout; }
        }

        /// <summary>
        /// Gets the maximum number of documents in a batch.
        /// </summary>
        /// <value>
        /// The maximum number of documents in a batch.
        /// </value>
        public int MaxBatchCount
        {
            get { return _maxBatchCount; }
        }

        /// <summary>
        /// Gets the maximum size of a document.
        /// </summary>
        /// <value>
        /// The maximum size of a document.
        /// </value>
        public int MaxDocumentSize
        {
            get { return _maxDocumentSize; }
        }

        /// <summary>
        /// Gets the maximum size of a message.
        /// </summary>
        /// <value>
        /// The maximum size of a message.
        /// </value>
        public int MaxMessageSize
        {
            get { return _maxMessageSize; }
        }

        /// <summary>
        /// Gets the maximum size of a wire document.
        /// </summary>
        /// <value>
        /// The maximum size of a wire document.
        /// </value>
        public int MaxWireDocumentSize
        {
            get { return _maxWireDocumentSize; }
        }

        /// <summary>
        /// The reason the server description was last changed.
        /// </summary>
        /// <value>The reason the server description was last changed.</value>
        public string ReasonChanged => _reasonChanged;

        /// <summary>
        /// Gets the replica set configuration.
        /// </summary>
        /// <value>
        /// The replica set configuration.
        /// </value>
        public ReplicaSetConfig ReplicaSetConfig
        {
            get { return _replicaSetConfig; }
        }

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
        /// Gets the server state.
        /// </summary>
        /// <value>
        /// The server state.
        /// </value>
        public ServerState State
        {
            get { return _state; }
        }

        /// <summary>
        /// Gets the replica set tags.
        /// </summary>
        /// <value>
        /// The replica set tags (null if not a replica set or if the replica set has no tags).
        /// </value>
        public TagSet Tags
        {
            get { return _tags; }
        }

        /// <summary>
        /// Gets the server type.
        /// </summary>
        /// <value>
        /// The server type.
        /// </value>
        public ServerType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the topology version.
        /// </summary>
        /// <value>
        /// The server topology version.
        /// </value>
        public TopologyVersion TopologyVersion
        {
            get { return _topologyVersion; }
        }

        /// <summary>
        /// Gets the server version.
        /// </summary>
        /// <value>
        /// The server version.
        /// </value>
        public SemanticVersion Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Gets the wire version range.
        /// </summary>
        /// <value>
        /// The wire version range.
        /// </value>
        public Range<int> WireVersionRange
        {
            get { return _wireVersionRange; }
        }

        // methods
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ServerDescription);
        }

        /// <inheritdoc/>
        public bool Equals(ServerDescription other)
        {
            if (object.ReferenceEquals(other, null) || other.GetType() != typeof(ServerDescription))
            {
                return false;
            }

            return
                _averageRoundTripTime == other._averageRoundTripTime &&
                object.Equals(_canonicalEndPoint, other._canonicalEndPoint) &&
                object.Equals(_electionId, other._electionId) &&
                EndPointHelper.Equals(_endPoint, other._endPoint) &&
                object.Equals(_heartbeatException, other._heartbeatException) &&
                _heartbeatInterval == other._heartbeatInterval &&
                _lastHeartbeatTimestamp == other.LastHeartbeatTimestamp &&
                _lastUpdateTimestamp == other._lastUpdateTimestamp &&
                _lastWriteTimestamp == other._lastWriteTimestamp &&
                _logicalSessionTimeout == other._logicalSessionTimeout &&
                _maxBatchCount == other._maxBatchCount &&
                _maxDocumentSize == other._maxDocumentSize &&
                _maxMessageSize == other._maxMessageSize &&
                _maxWireDocumentSize == other._maxWireDocumentSize &&
                _reasonChanged.Equals(other._reasonChanged, StringComparison.Ordinal) &&
                object.Equals(_replicaSetConfig, other._replicaSetConfig) &&
                _serverId.Equals(other._serverId) &&
                _state == other._state &&
                object.Equals(_tags, other._tags) &&
                _type == other._type &&
                object.Equals(_version, other._version) &&
                object.Equals(_wireVersionRange, other._wireVersionRange);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // revision is ignored
            return new Hasher()
                .Hash(_averageRoundTripTime)
                .Hash(_canonicalEndPoint)
                .Hash(_electionId)
                .Hash(_endPoint)
                .Hash(_heartbeatException)
                .Hash(_heartbeatInterval)
                .Hash(_lastHeartbeatTimestamp)
                .Hash(_lastUpdateTimestamp)
                .Hash(_lastWriteTimestamp)
                .Hash(_logicalSessionTimeout)
                .Hash(_maxBatchCount)
                .Hash(_maxDocumentSize)
                .Hash(_maxMessageSize)
                .Hash(_maxWireDocumentSize)
                .Hash(_reasonChanged)
                .Hash(_replicaSetConfig)
                .Hash(_serverId)
                .Hash(_state)
                .Hash(_tags)
                .Hash(_topologyVersion)
                .Hash(_type)
                .Hash(_version)
                .Hash(_wireVersionRange)
                .GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="ServerDescription" /> can be considered as equal to decide should we publish sdam events or not.
        /// </summary>
        /// <param name="other">The other server description.</param>
        /// <returns><c>true</c>, if sdam events should be suppressed, otherwise <c>false</c>.</returns>
        public bool SdamEquals(ServerDescription other)
        {
            return
                EndPointHelper.Equals(_endPoint, other._endPoint) &&
                _type == other.Type &&
                object.Equals(_wireVersionRange, other._wireVersionRange) &&
                EndPointHelper.Equals(_canonicalEndPoint, other._canonicalEndPoint) && // me
                EndPointHelper.SequenceEquals(_replicaSetConfig?.Members, other._replicaSetConfig?.Members) && // hosts, passives, arbiters
                object.Equals(_tags, other._tags) &&
                _replicaSetConfig?.Name == other._replicaSetConfig?.Name && // setName
                _replicaSetConfig?.Version == other._replicaSetConfig?.Version && // setVersion
                object.Equals(_electionId, other._electionId) &&
                EndPointHelper.Equals(_replicaSetConfig?.Primary, other._replicaSetConfig?.Primary) && // primary
                _logicalSessionTimeout == other._logicalSessionTimeout;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return new StringBuilder()
                .Append("{ ")
                .AppendFormat("ServerId: \"{0}\"", _serverId)
                .AppendFormat(", EndPoint: \"{0}\"", _endPoint)
                .AppendFormat(", ReasonChanged: \"{0}\"", _reasonChanged)
                .AppendFormat(", State: \"{0}\"", _state)
                .Append($", TopologyVersion: {_topologyVersion}")
                .AppendFormat(", Type: \"{0}\"", _type)
                .AppendFormatIf(_tags != null && !_tags.IsEmpty, ", Tags: \"{0}\"", _tags)
                .AppendFormatIf(_state == ServerState.Connected, ", WireVersionRange: \"{0}\"", _wireVersionRange)
                .AppendFormatIf(_electionId != null, ", ElectionId: \"{0}\"", _electionId)
                .AppendFormatIf(_heartbeatException != null, ", HeartbeatException: \"{0}\"", _heartbeatException)
                .AppendFormat(", LastHeartbeatTimestamp: {0}", _lastHeartbeatTimestamp.HasValue ? "\"" + LastHeartbeatTimestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK") + "\"" : "null")
                .AppendFormat(", LastUpdateTimestamp: \"{0:yyyy-MM-ddTHH:mm:ss.fffffffK}\"", _lastUpdateTimestamp)
                .Append(" }")
                .ToString();
        }

        /// <summary>
        /// Returns a new instance of ServerDescription with some values changed.
        /// </summary>
        /// <param name="reasonChanged">The reason the server description changed.</param>
        /// <param name="averageRoundTripTime">The average round trip time.</param>
        /// <param name="canonicalEndPoint">The canonical end point.</param>
        /// <param name="electionId">The election identifier.</param>
        /// <param name="heartbeatException">The heartbeat exception.</param>
        /// <param name="heartbeatInterval">The heartbeat interval.</param>
        /// <param name="lastHeartbeatTimestamp">The last heartbeat timestamp.</param>
        /// <param name="lastUpdateTimestamp">The last update timestamp.</param>
        /// <param name="lastWriteTimestamp">The last write timestamp.</param>
        /// <param name="logicalSessionTimeout">The logical session timeout.</param>
        /// <param name="maxBatchCount">The maximum batch count.</param>
        /// <param name="maxDocumentSize">The maximum size of a document.</param>
        /// <param name="maxMessageSize">The maximum size of a message.</param>
        /// <param name="maxWireDocumentSize">The maximum size of a wire document.</param>
        /// <param name="replicaSetConfig">The replica set configuration.</param>
        /// <param name="state">The server state.</param>
        /// <param name="tags">The replica set tags.</param>
        /// <param name="topologyVersion">The topology version.</param>
        /// <param name="type">The server type.</param>
        /// <param name="version">The server version.</param>
        /// <param name="wireVersionRange">The wire version range.</param>
        /// <returns>
        /// A new instance of ServerDescription.
        /// </returns>
        public ServerDescription With(
            Optional<string> reasonChanged = default(Optional<string>),
            Optional<TimeSpan> averageRoundTripTime = default(Optional<TimeSpan>),
            Optional<EndPoint> canonicalEndPoint = default(Optional<EndPoint>),
            Optional<ElectionId> electionId = default(Optional<ElectionId>),
            Optional<Exception> heartbeatException = default(Optional<Exception>),
            Optional<TimeSpan> heartbeatInterval = default(Optional<TimeSpan>),
            Optional<DateTime?> lastHeartbeatTimestamp = default(Optional<DateTime?>),
            Optional<DateTime> lastUpdateTimestamp = default(Optional<DateTime>),
            Optional<DateTime?> lastWriteTimestamp = default(Optional<DateTime?>),
            Optional<TimeSpan?> logicalSessionTimeout = default(Optional<TimeSpan?>),
            Optional<int> maxBatchCount = default(Optional<int>),
            Optional<int> maxDocumentSize = default(Optional<int>),
            Optional<int> maxMessageSize = default(Optional<int>),
            Optional<int> maxWireDocumentSize = default(Optional<int>),
            Optional<ReplicaSetConfig> replicaSetConfig = default(Optional<ReplicaSetConfig>),
            Optional<ServerState> state = default(Optional<ServerState>),
            Optional<TagSet> tags = default(Optional<TagSet>),
            Optional<TopologyVersion> topologyVersion = default(Optional<TopologyVersion>),
            Optional<ServerType> type = default(Optional<ServerType>),
            Optional<SemanticVersion> version = default(Optional<SemanticVersion>),
            Optional<Range<int>> wireVersionRange = default(Optional<Range<int>>))
        {
            return new ServerDescription(
                _serverId,
                _endPoint,
                reasonChanged,
                averageRoundTripTime: averageRoundTripTime.WithDefault(_averageRoundTripTime),
                canonicalEndPoint: canonicalEndPoint.WithDefault(_canonicalEndPoint),
                electionId: electionId.WithDefault(_electionId),
                heartbeatException: heartbeatException.WithDefault(_heartbeatException),
                heartbeatInterval: heartbeatInterval.WithDefault(_heartbeatInterval),
                lastHeartbeatTimestamp: lastHeartbeatTimestamp.WithDefault(_lastHeartbeatTimestamp),
                lastUpdateTimestamp: lastUpdateTimestamp.WithDefault(DateTime.UtcNow),
                lastWriteTimestamp: lastWriteTimestamp.WithDefault(_lastWriteTimestamp),
                logicalSessionTimeout: logicalSessionTimeout.WithDefault(_logicalSessionTimeout),
                maxBatchCount: maxBatchCount.WithDefault(_maxBatchCount),
                maxDocumentSize: maxDocumentSize.WithDefault(_maxDocumentSize),
                maxMessageSize: maxMessageSize.WithDefault(_maxMessageSize),
                maxWireDocumentSize: maxWireDocumentSize.WithDefault(_maxWireDocumentSize),
                replicaSetConfig: replicaSetConfig.WithDefault(_replicaSetConfig),
                state: state.WithDefault(_state),
                tags: tags.WithDefault(_tags),
                topologyVersion: topologyVersion.WithDefault(_topologyVersion),
                type: type.WithDefault(_type),
                version: version.WithDefault(_version),
                wireVersionRange: wireVersionRange.WithDefault(_wireVersionRange));
        }

        /// <summary>
        /// Returns a new ServerDescription with a new HeartbeatException.
        /// </summary>
        /// <param name="heartbeatException">The heartbeat exception.</param>
        /// <returns>
        /// A new instance of ServerDescription.
        /// </returns>
        public ServerDescription WithHeartbeatException(Exception heartbeatException)
        {
            return new ServerDescription(
                _serverId,
                _endPoint,
                reasonChanged: "HeartbeatFailed",
                averageRoundTripTime: _averageRoundTripTime,
                canonicalEndPoint: _canonicalEndPoint,
                electionId: _electionId,
                heartbeatException: heartbeatException,
                heartbeatInterval: _heartbeatInterval,
                lastHeartbeatTimestamp: DateTime.UtcNow,
                lastUpdateTimestamp: DateTime.UtcNow,
                lastWriteTimestamp: _lastWriteTimestamp,
                logicalSessionTimeout: _logicalSessionTimeout,
                maxBatchCount: _maxBatchCount,
                maxDocumentSize: _maxDocumentSize,
                maxMessageSize: _maxMessageSize,
                maxWireDocumentSize: _maxWireDocumentSize,
                replicaSetConfig: _replicaSetConfig,
                state: _state,
                tags: _tags,
                topologyVersion: _topologyVersion,
                type: _type,
                version: _version,
                wireVersionRange: _wireVersionRange);
        }
    }
}
