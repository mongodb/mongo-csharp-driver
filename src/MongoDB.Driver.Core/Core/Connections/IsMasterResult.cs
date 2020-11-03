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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents the result of an isMaster command.
    /// </summary>
    public sealed class IsMasterResult : IEquatable<IsMasterResult>
    {
        // fields
        private readonly BsonDocument _wrapped;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="IsMasterResult"/> class.
        /// </summary>
        /// <param name="wrapped">The wrapped result document.</param>
        public IsMasterResult(BsonDocument wrapped)
        {
            _wrapped = Ensure.IsNotNull(wrapped, nameof(wrapped));
        }

        // properties
        /// <summary>
        /// Gets the compressor types.
        /// </summary>
        public IReadOnlyList<CompressorType> Compressions
        {
            get
            {
                if (_wrapped.TryGetValue("compression", out var value))
                {
                    return value
                        .AsBsonArray
                        .Select(x =>
                        {
                            return CompressorTypeMapper.TryFromServerName(x.AsString, out var compressorType)
                                ? compressorType
                                // we can have such a case only due to the server bug
                                : throw new NotSupportedException($"The unsupported compressor name: '{x}'.");
                        })
                        .ToList();
                }

                return new CompressorType[0];
            }
        }

        /// <summary>
        /// Gets the connection id server value.
        /// </summary>
        public int? ConnectionIdServerValue
        {
            get
            {
                if (_wrapped.TryGetValue("connectionId", out var connectionIdBsonValue))
                {
                    return connectionIdBsonValue.ToInt32();
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the election identifier.
        /// </summary>
        public ElectionId ElectionId
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("electionId", out value))
                {
                    return new ElectionId((ObjectId)value);
                }

                return null;
            }
        }

        /// <summary>
        /// Get whether SaslSupportedMechs was part of the isMaster response.
        /// </summary>
        /// <value>
        /// Whether SaslSupportedMechs was part of the isMaster response.
        /// </value>
        public bool HasSaslSupportedMechs
        {
            get { return _wrapped.Contains("saslSupportedMechs"); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is an arbiter.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is an arbiter; otherwise, <c>false</c>.
        /// </value>
        public bool IsArbiter
        {
            get { return ServerType == ServerType.ReplicaSetArbiter; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a replica set member.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is a replica set member; otherwise, <c>false</c>.
        /// </value>
        public bool IsReplicaSetMember
        {
            get { return ServerType.IsReplicaSetMember(); }
        }

        /// <summary>
        /// Gets the last write timestamp.
        /// </summary>
        /// <value>
        /// The last write timestamp.
        /// </value>
        public DateTime? LastWriteTimestamp
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("lastWrite", out value))
                {
                    return value["lastWriteDate"].ToUniversalTime();
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the logical session timeout.
        /// </summary>
        /// <value>
        /// The logical session timeout.
        /// </value>
        public TimeSpan? LogicalSessionTimeout
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("logicalSessionTimeoutMinutes", out value))
                {
                    if (value.BsonType == BsonType.Null)
                    {
                        return null;
                    }
                    else
                    {
                        return TimeSpan.FromMinutes(value.ToDouble());
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the maximum number of documents in a batch.
        /// </summary>
        /// <value>
        /// The maximum number of documents in a batch.
        /// </value>
        public int MaxBatchCount
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("maxWriteBatchSize", out value))
                {
                    return value.ToInt32();
                }

                return 1000;
            }
        }

        /// <summary>
        /// Gets the maximum size of a document.
        /// </summary>
        /// <value>
        /// The maximum size of a document.
        /// </value>
        public int MaxDocumentSize
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("maxBsonObjectSize", out value))
                {
                    return value.ToInt32();
                }

                return 4 * 1024 * 1024;
            }
        }

        /// <summary>
        /// Gets the maximum size of a message.
        /// </summary>
        /// <value>
        /// The maximum size of a message.
        /// </value>
        public int MaxMessageSize
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("maxMessageSizeBytes", out value))
                {
                    return value.ToInt32();
                }

                return Math.Max(MaxDocumentSize + 1024, 16000000);
            }
        }

        /// <summary>
        /// Gets the endpoint the server is claiming it is known as.
        /// </summary>
        public EndPoint Me
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("me", out value))
                {
                    return EndPointHelper.Parse((string)value);
                }

                return null;
            }
        }

        /// <summary>
        /// Get the SaslSupportedMechs.
        /// </summary>
        /// <value>
        /// The SaslSupportedMechs. Empty if saslSupportedMechs was an empty list or if saslSupportedMechs was not
        /// included in the isMaster response.
        /// </value>
        public IEnumerable<string> SaslSupportedMechs
        {
            get { return _wrapped.GetValue("saslSupportedMechs", new BsonArray()).AsBsonArray.Select(s => s.ToString()); }
        }

        /// <summary>
        /// Gets the type of the server.
        /// </summary>
        /// <value>
        /// The type of the server.
        /// </value>
        public ServerType ServerType
        {
            get
            {
                if (!_wrapped.GetValue("ok", false).ToBoolean())
                {
                    return ServerType.Unknown;
                }

                if (_wrapped.GetValue("isreplicaset", false).ToBoolean())
                {
                    return ServerType.ReplicaSetGhost;
                }

                if (_wrapped.Contains("setName"))
                {
                    if (_wrapped.GetValue("ismaster", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetPrimary;
                    }
                    if (_wrapped.GetValue("hidden", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetOther;
                    }
                    if (_wrapped.GetValue("secondary", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetSecondary;
                    }
                    if (_wrapped.GetValue("arbiterOnly", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetArbiter;
                    }

                    return ServerType.ReplicaSetOther;
                }

                if ((string)_wrapped.GetValue("msg", null) == "isdbgrid")
                {
                    return ServerType.ShardRouter;
                }

                return ServerType.Standalone;
            }
        }

        /// <summary>
        /// Get the SpeculativeAuthenticate reply.
        /// </summary>
        /// <value>
        /// Null if isMaster["ok"] != 1 or if the SpeculativeAuthenticate reply was not included in the isMaster response.
        /// </value>
        public BsonDocument SpeculativeAuthenticate
        {
            get
            {
                if (_wrapped.TryGetValue("ok", out var ok) && ok.ToBoolean() &&
                    _wrapped.TryGetValue("speculativeAuthenticate", out var speculativeAuthenticate))
                {
                    return (BsonDocument)speculativeAuthenticate;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the replica set tags.
        /// </summary>
        /// <value>
        /// The replica set tags.
        /// </value>
        public TagSet Tags
        {
            get
            {
                BsonValue tags;
                if (_wrapped.TryGetValue("tags", out tags))
                {
                    return new TagSet(tags.AsBsonDocument.Select(e => new Tag(e.Name, (string)e.Value)));
                }

                return null;
            }
        }

        /// <summary>
        /// Get the TopologyVersion.
        /// </summary>
        /// <value>
        /// Null if TopologyVersion was not included in the isMaster response.
        /// </value>
        public TopologyVersion TopologyVersion
        {
            get
            {
                if (_wrapped.TryGetValue("topologyVersion", out var topologyVersionValue) &&
                    topologyVersionValue is BsonDocument topologyVersion)
                {
                    return Servers.TopologyVersion.FromBsonDocument(topologyVersion);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the maximum wire version.
        /// </summary>
        /// <value>
        /// The maximum wire version.
        /// </value>
        public int MaxWireVersion
        {
            get { return _wrapped.GetValue("maxWireVersion", 0).ToInt32(); }
        }

        /// <summary>
        /// Gets the minimum wire version.
        /// </summary>
        /// <value>
        /// The minimum wire version.
        /// </value>
        public int MinWireVersion
        {
            get { return _wrapped.GetValue("minWireVersion", 0).ToInt32(); }
        }

        /// <summary>
        /// Gets the wrapped result document.
        /// </summary>
        /// <value>
        /// The wrapped result document.
        /// </value>
        public BsonDocument Wrapped
        {
            get { return _wrapped; }
        }

        // methods
        /// <inheritdoc/>
        public bool Equals(IsMasterResult other)
        {
            if (other == null)
            {
                return false;
            }

            return _wrapped.Equals(other._wrapped);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as IsMasterResult);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _wrapped.GetHashCode();
        }

        private List<EndPoint> GetMembers()
        {
            var hosts = GetMembers("hosts");
            var passives = GetMembers("passives");
            var arbiters = GetMembers("arbiters");
            return hosts.Concat(passives).Concat(arbiters).ToList();
        }

        private IEnumerable<EndPoint> GetMembers(string elementName)
        {
            if (!_wrapped.Contains(elementName))
            {
                return new EndPoint[0];
            }

            return ((BsonArray)_wrapped[elementName]).Select(v => EndPointHelper.Parse((string)v));
        }

        private EndPoint GetPrimary()
        {
            BsonValue primary;
            if (_wrapped.TryGetValue("primary", out primary))
            {
                // TODO: what does primary look like when there is no current primary (null, empty string)?
                return EndPointHelper.Parse((string)primary);
            }

            return null;
        }

        /// <summary>
        /// Gets the replica set configuration.
        /// </summary>
        /// <returns>The replica set configuration.</returns>
        public ReplicaSetConfig GetReplicaSetConfig()
        {
            if (!IsReplicaSetMember)
            {
                return null;
            }

            var members = GetMembers();
            var name = (string)_wrapped.GetValue("setName", null);
            var primary = GetPrimary();
            var version = _wrapped.Contains("setVersion") ? (int?)_wrapped["setVersion"].ToInt32() : null;

            return new ReplicaSetConfig(members, name, primary, version);
        }
    }
}
