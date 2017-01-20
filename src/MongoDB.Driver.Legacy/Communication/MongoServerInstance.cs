/* Copyright 2010-2016 MongoDB Inc.
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents an instance of a MongoDB server host.
    /// </summary>
    public sealed class MongoServerInstance
    {
        // private static fields
        private static int __nextSequentialId;

        // public events
        /// <summary>
        /// Occurs when the value of the State property changes.
        /// </summary>
        public event EventHandler StateChanged;

        // private fields
        private readonly MongoServerSettings _settings;
        private readonly MongoServerAddress _address;
        private readonly int _sequentialId;
        private readonly ICluster _cluster;
        private readonly EndPoint _endPoint;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoServerInstance" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="address">The address.</param>
        /// <param name="cluster">The cluster.</param>
        /// <param name="endPoint">The end point.</param>
        internal MongoServerInstance(MongoServerSettings settings, MongoServerAddress address, ICluster cluster, EndPoint endPoint)
        {
            _settings = settings;
            _address = address;
            _cluster = cluster;
            _sequentialId = Interlocked.Increment(ref __nextSequentialId);
            _endPoint = endPoint;
        }

        // public properties
        /// <summary>
        /// Gets the instance type.
        /// </summary>
        public MongoServerInstanceType InstanceType
        {
            get
            {
                var serverDescription = GetServerDescription();
                switch (serverDescription.Type)
                {
                    case ServerType.ReplicaSetArbiter:
                    case ServerType.ReplicaSetPrimary:
                    case ServerType.ReplicaSetSecondary:
                    case ServerType.ReplicaSetOther:
                        return MongoServerInstanceType.ReplicaSetMember;
                    case ServerType.ShardRouter:
                        return MongoServerInstanceType.ShardRouter;
                    case ServerType.Standalone:
                        return MongoServerInstanceType.StandAlone;
                    case ServerType.Unknown:
                    default:
                        return MongoServerInstanceType.Unknown;
                }
            }
        }

        // public properties
        /// <summary>
        /// Gets the address of this server instance.
        /// </summary>
        public MongoServerAddress Address
        {
            get { return _address; }
        }

        /// <summary>
        /// Gets the build info of this server instance.
        /// </summary>
        public MongoServerBuildInfo BuildInfo
        {
            get
            {
                var serverDescription = GetServerDescription();
                var versionString = serverDescription.Version.ToString();
                return new MongoServerBuildInfo(versionString);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this server instance is an arbiter instance.
        /// </summary>
        public bool IsArbiter
        {
            get
            {
                var serverDescription = GetServerDescription();
                return serverDescription.Type == ServerType.ReplicaSetArbiter;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this server instance is a passive instance.
        /// </summary>
        [Obsolete("Passives are treated the same as secondaries.")]
        public bool IsPassive
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this server instance is a primary.
        /// </summary>
        public bool IsPrimary
        {
            get
            {
                var serverDescription = GetServerDescription();
                return serverDescription.Type.IsWritable();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this server instance is a secondary.
        /// </summary>
        public bool IsSecondary
        {
            get
            {
                var serverDescription = GetServerDescription();
                return serverDescription.Type == ServerType.ReplicaSetSecondary;
            }
        }

        /// <summary>
        /// Gets the max document size for this server instance.
        /// </summary>
        public int MaxDocumentSize
        {
            get
            {
                var serverDescription = GetServerDescription();
                return serverDescription.MaxDocumentSize;
            }
        }

        /// <summary>
        /// Gets the max message length for this server instance.
        /// </summary>
        public int MaxMessageLength
        {
            get
            {
                var serverDescription = GetServerDescription();
                return serverDescription.MaxMessageSize;
            }
        }

        /// <summary>
        /// Gets the maximum size of a wire document. Normally slightly larger than MaxDocumentSize.
        /// </summary>
        public int MaxWireDocumentSize
        {
            get
            {
                var serverDescription = GetServerDescription();
                return serverDescription.MaxWireDocumentSize;
            }
        }

        /// <summary>
        /// Gets the maximum batch count for write operations.
        /// </summary>
        public int MaxBatchCount
        {
            get
            {
                var serverDescription = GetServerDescription();
                return serverDescription.MaxBatchCount;
            }
        }

        /// <summary>
        /// Gets the unique sequential Id for this server instance.
        /// </summary>
        public int SequentialId
        {
            get { return _sequentialId; }
        }

        /// <summary>
        /// Gets the server for this server instance.
        /// </summary>
        public MongoServerSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>
        /// Gets the state of this server instance.
        /// </summary>
        public MongoServerState State
        {
            get
            {
                var serverDescription = GetServerDescription();
                switch (serverDescription.State)
                {
                    case ServerState.Connected:
                        return MongoServerState.Connected;
                    case ServerState.Disconnected:
                    default:
                        return MongoServerState.Disconnected;
                }
            }
        }

        // internal properties
        internal EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        // public methods
        /// <summary>
        /// Gets the IP end point of this server instance.
        /// </summary>
        /// <returns>The IP end point of this server instance.</returns>
        public IPEndPoint GetIPEndPoint()
        {
#if NETSTANDARD1_5 || NETSTANDARD1_6
            var ipAddresses = Dns.GetHostAddressesAsync(_address.Host).GetAwaiter().GetResult();
#else
            var ipAddresses = Dns.GetHostAddresses(_address.Host);
#endif
            var ipAddress = ipAddresses.FirstOrDefault(a => a.AddressFamily == (_settings.IPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork));
            return new IPEndPoint(ipAddress, _address.Port);
        }

        /// <summary>
        /// Gets the server description.
        /// </summary>
        /// <returns>The server description.</returns>
        public ServerDescription GetServerDescription()
        {
            var serverDescription = _cluster.Description.Servers.FirstOrDefault(s => EndPointHelper.Equals(s.EndPoint, _endPoint));
            if (serverDescription == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Cluster does not contain a server with end point: '{0}'.",
                    _endPoint));
            }
            return serverDescription;
        }

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not).
        /// </summary>
        public void Ping()
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new PingOperation(messageEncoderSettings);

            var server = GetServer();
            using (var binding = new SingleServerReadBinding(server, ReadPreference.PrimaryPreferred))
            {
                operation.Execute(binding, CancellationToken.None);
            }
        }

        /// <summary>
        /// Checks whether this server instance supports a feature.
        /// </summary>
        /// <param name="featureId">The id of the feature.</param>
        /// <returns>True if this server instance supports the feature; otherwise, false.</returns>
        public bool Supports(FeatureId featureId)
        {
            switch (featureId)
            {
                // supported in all versions
                case FeatureId.WriteOpcodes:
                    return true;

                // supported in 2.4.0 and newer
                case FeatureId.GeoJson:
                case FeatureId.TextSearchCommand:
                    return BuildInfo.Version >= new Version(2, 4, 0);

                // supported in 2.6.0 and newer
                case FeatureId.AggregateAllowDiskUse:
                case FeatureId.AggregateCursor:
                case FeatureId.AggregateExplain:
                case FeatureId.AggregateOutputToCollection:
                case FeatureId.CreateIndexCommand:
                case FeatureId.MaxTime:
                case FeatureId.TextSearchQuery:
                case FeatureId.UserManagementCommands:
                case FeatureId.WriteCommands:
                    return BuildInfo.Version >= new Version(2, 6, 0);

                // supported in 2.6.0 and newer but not on mongos
                case FeatureId.ParallelScanCommand:
                    return BuildInfo.Version >= new Version(2, 6, 0) && InstanceType != MongoServerInstanceType.ShardRouter;

                default:
                    return false;
            }
        }

        // private methods
        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict }
            };
        }

        private IServer GetServer()
        {
            var serverSelector = new EndPointServerSelector(_endPoint);
            var server = _cluster.SelectServer(serverSelector, CancellationToken.None);
            return server;
        }

        private void OnStateChanged()
        {
            var handler = StateChanged;
            if (handler != null)
            {
                try
                {
                    handler(this, EventArgs.Empty);
                }
                catch
                {
                    // ignore exceptions
                }
            }
        }
    }
}
