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
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents settings for a cluster.
    /// </summary>
    public class ClusterSettings
    {
        #region static
        // static fields
        private static readonly IReadOnlyList<EndPoint> __defaultEndPoints = new EndPoint[] { new DnsEndPoint("localhost", 27017) };
        #endregion

        // fields
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly ClusterConnectionMode _connectionMode;
        private readonly ConnectionModeSwitch _connectionModeSwitch;
#pragma warning restore CS0618 // Type or member is obsolete
        private readonly bool? _directConnection;
        private readonly IReadOnlyList<EndPoint> _endPoints;
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> _kmsProviders;
        private readonly TimeSpan _localThreshold;
        private readonly int _maxServerSelectionWaitQueueSize;
        private readonly string _replicaSetName;
        private readonly IReadOnlyDictionary<string, BsonDocument> _schemaMap;
        private readonly ConnectionStringScheme _scheme;
        private readonly TimeSpan _serverSelectionTimeout;
        private readonly IServerSelector _preServerSelector;
        private readonly IServerSelector _postServerSelector;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterSettings"/> class.
        /// </summary>
        /// <param name="connectionMode">The connection mode.</param>
        /// <param name="connectionModeSwitch">The connection mode switch.</param>
        /// <param name="directConnection">The directConnection.</param>
        /// <param name="endPoints">The end points.</param>
        /// <param name="kmsProviders">The kms providers.</param>
        /// <param name="localThreshold">The local threshold.</param>
        /// <param name="maxServerSelectionWaitQueueSize">Maximum size of the server selection wait queue.</param>
        /// <param name="replicaSetName">Name of the replica set.</param>
        /// <param name="serverSelectionTimeout">The server selection timeout.</param>
        /// <param name="preServerSelector">The pre server selector.</param>
        /// <param name="postServerSelector">The post server selector.</param>
        /// <param name="schemaMap">The schema map.</param>
        /// <param name="scheme">The connection string scheme.</param>
        public ClusterSettings(
#pragma warning disable CS0618 // Type or member is obsolete
            Optional<ClusterConnectionMode> connectionMode = default(Optional<ClusterConnectionMode>),
            Optional<ConnectionModeSwitch> connectionModeSwitch = default,
#pragma warning restore CS0618 // Type or member is obsolete
            Optional<bool?> directConnection = default,
            Optional<IEnumerable<EndPoint>> endPoints = default(Optional<IEnumerable<EndPoint>>),
            Optional<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>> kmsProviders = default(Optional<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>>),
            Optional<TimeSpan> localThreshold = default,
            Optional<int> maxServerSelectionWaitQueueSize = default(Optional<int>),
            Optional<string> replicaSetName = default(Optional<string>),
            Optional<TimeSpan> serverSelectionTimeout = default(Optional<TimeSpan>),
            Optional<IServerSelector> preServerSelector = default(Optional<IServerSelector>),
            Optional<IServerSelector> postServerSelector = default(Optional<IServerSelector>),
            Optional<IReadOnlyDictionary<string, BsonDocument>> schemaMap = default(Optional<IReadOnlyDictionary<string, BsonDocument>>),
            Optional<ConnectionStringScheme> scheme = default(Optional<ConnectionStringScheme>))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _connectionMode = connectionMode.WithDefault(ClusterConnectionMode.Automatic);
            _connectionModeSwitch = connectionModeSwitch.WithDefault(ConnectionModeSwitch.NotSet);
#pragma warning restore CS0618 // Type or member is obsolete
            _directConnection = directConnection.WithDefault(null);
            _endPoints = Ensure.IsNotNull(endPoints.WithDefault(__defaultEndPoints), "endPoints").ToList();
            _kmsProviders = kmsProviders.WithDefault(null);
            _localThreshold = Ensure.IsGreaterThanOrEqualToZero(localThreshold.WithDefault(TimeSpan.FromMilliseconds(15)), "localThreshold");
            _maxServerSelectionWaitQueueSize = Ensure.IsGreaterThanOrEqualToZero(maxServerSelectionWaitQueueSize.WithDefault(500), "maxServerSelectionWaitQueueSize");
            _replicaSetName = replicaSetName.WithDefault(null);
            _serverSelectionTimeout = Ensure.IsGreaterThanOrEqualToZero(serverSelectionTimeout.WithDefault(TimeSpan.FromSeconds(30)), "serverSelectionTimeout");
            _preServerSelector = preServerSelector.WithDefault(null);
            _postServerSelector = postServerSelector.WithDefault(null);
            _scheme = scheme.WithDefault(ConnectionStringScheme.MongoDB);
            _schemaMap = schemaMap.WithDefault(null);

            ClusterConnectionModeHelper.EnsureConnectionModeValuesAreValid(_connectionMode, _connectionModeSwitch, _directConnection);
        }

        // properties
        /// <summary>
        /// Gets the connection mode.
        /// </summary>
        /// <value>
        /// The connection mode.
        /// </value>
        [Obsolete("Use DirectConnection instead.")]
        public ClusterConnectionMode ConnectionMode
        {
            get
            {
                if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    throw new InvalidOperationException("ConnectionMode cannot be used when ConnectionModeSwitch is set to UseDirectConnection.");
                }
                return _connectionMode;
            }
        }

        /// <summary>
        /// Gets the connection mode switch.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public ConnectionModeSwitch ConnectionModeSwitch
        {
            get { return _connectionModeSwitch; }
        }

        /// <summary>
        /// Gets the DirectConnection.
        /// </summary>
        public bool? DirectConnection
        {
            get
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (_connectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    throw new InvalidOperationException("DirectConnection cannot be used when ConnectionModeSwitch is set to UseConnectionMode.");
                }
                return _directConnection;
            }
        }

        /// <summary>
        /// Gets the end points.
        /// </summary>
        /// <value>
        /// The end points.
        /// </value>
        public IReadOnlyList<EndPoint> EndPoints
        {
            get { return _endPoints; }
        }

        /// <summary>
        /// Gets the kms providers.
        /// </summary>
        /// <value>
        /// The kms providers.
        /// </value>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> KmsProviders
        {
            get { return _kmsProviders; }
        }

        /// <summary>
        /// Gets the local threshold.
        /// </summary>
        /// <value>
        /// The local threshold.
        /// </value>
        public TimeSpan LocalThreshold
        {
            get { return _localThreshold; }
        }

        /// <summary>
        /// Gets the maximum size of the server selection wait queue.
        /// </summary>
        /// <value>
        /// The maximum size of the server selection wait queue.
        /// </value>
        public int MaxServerSelectionWaitQueueSize
        {
            get { return _maxServerSelectionWaitQueueSize; }
        }

        /// <summary>
        /// Gets the name of the replica set.
        /// </summary>
        /// <value>
        /// The name of the replica set.
        /// </value>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
        }

        /// <summary>
        /// Gets the schema map.
        /// </summary>
        /// <value>
        /// The schema map.
        /// </value>
        public IReadOnlyDictionary<string, BsonDocument> SchemaMap
        {
            get { return _schemaMap; }
        }

        /// <summary>
        /// Gets the connection string scheme.
        /// </summary>
        /// <value>
        /// The connection string scheme.
        /// </value>
        public ConnectionStringScheme Scheme
        {
            get { return _scheme; }
        }

        /// <summary>
        /// Gets the server selection timeout.
        /// </summary>
        /// <value>
        /// The server selection timeout.
        /// </value>
        public TimeSpan ServerSelectionTimeout
        {
            get { return _serverSelectionTimeout; }
        }

        /// <summary>
        /// Gets the pre server selector.
        /// </summary>
        /// <value>
        /// The pre server selector.
        /// </value>
        public IServerSelector PreServerSelector
        {
            get { return _preServerSelector; }
        }

        /// <summary>
        /// Gets the post server selector.
        /// </summary>
        /// <value>
        /// The post server selector.
        /// </value>
        public IServerSelector PostServerSelector
        {
            get { return _postServerSelector; }
        }

        // methods
        /// <summary>
        /// Returns a new ClusterSettings instance with some settings changed.
        /// </summary>
        /// <param name="connectionMode">The connection mode.</param>
        /// <param name="connectionModeSwitch">The connection mode switch.</param>
        /// <param name="directConnection">The directConnection.</param>
        /// <param name="endPoints">The end points.</param>
        /// <param name="kmsProviders">The kms providers.</param>
        /// <param name="localThreshold">The local threshold.</param>
        /// <param name="maxServerSelectionWaitQueueSize">Maximum size of the server selection wait queue.</param>
        /// <param name="replicaSetName">Name of the replica set.</param>
        /// <param name="serverSelectionTimeout">The server selection timeout.</param>
        /// <param name="preServerSelector">The pre server selector.</param>
        /// <param name="postServerSelector">The post server selector.</param>
        /// <param name="schemaMap">The schema map.</param>
        /// <param name="scheme">The connection string scheme.</param>
        /// <returns>A new ClusterSettings instance.</returns>
        public ClusterSettings With(
#pragma warning disable CS0618 // Type or member is obsolete
            Optional<ClusterConnectionMode> connectionMode = default(Optional<ClusterConnectionMode>),
            Optional<ConnectionModeSwitch> connectionModeSwitch = default,
#pragma warning restore CS0618 // Type or member is obsolete
            Optional<bool?> directConnection = default,
            Optional<IEnumerable<EndPoint>> endPoints = default(Optional<IEnumerable<EndPoint>>),
            Optional<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>> kmsProviders = default(Optional<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>>),
            Optional<TimeSpan> localThreshold = default(Optional<TimeSpan>),
            Optional<int> maxServerSelectionWaitQueueSize = default(Optional<int>),
            Optional<string> replicaSetName = default(Optional<string>),
            Optional<TimeSpan> serverSelectionTimeout = default(Optional<TimeSpan>),
            Optional<IServerSelector> preServerSelector = default(Optional<IServerSelector>),
            Optional<IServerSelector> postServerSelector = default(Optional<IServerSelector>),
            Optional<IReadOnlyDictionary<string, BsonDocument>> schemaMap = default(Optional<IReadOnlyDictionary<string, BsonDocument>>),
            Optional<ConnectionStringScheme> scheme = default(Optional<ConnectionStringScheme>))
        {
            return new ClusterSettings(
                connectionMode: connectionMode.WithDefault(_connectionMode),
                connectionModeSwitch: connectionModeSwitch.WithDefault(_connectionModeSwitch),
                directConnection: directConnection.WithDefault(_directConnection),
                endPoints: Optional.Enumerable(endPoints.WithDefault(_endPoints)),
                kmsProviders: Optional.Create(kmsProviders.WithDefault(_kmsProviders)),
                localThreshold: localThreshold.WithDefault(_localThreshold),
                maxServerSelectionWaitQueueSize: maxServerSelectionWaitQueueSize.WithDefault(_maxServerSelectionWaitQueueSize),
                replicaSetName: replicaSetName.WithDefault(_replicaSetName),
                serverSelectionTimeout: serverSelectionTimeout.WithDefault(_serverSelectionTimeout),
                preServerSelector: Optional.Create(preServerSelector.WithDefault(_preServerSelector)),
                postServerSelector: Optional.Create(postServerSelector.WithDefault(_postServerSelector)),
                schemaMap: Optional.Create(schemaMap.WithDefault(_schemaMap)),
                scheme: scheme.WithDefault(_scheme));
        }

        // internal methods
        internal ClusterType GetInitialClusterType()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                if (_directConnection.GetValueOrDefault())
                {
                    return ClusterType.Standalone;
                }
                else
                {
                    if (_replicaSetName != null)
                    {
                        return ClusterType.ReplicaSet;
                    }
                    else
                    {
                        return ClusterType.Unknown;
                    }
                }
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                switch (_connectionMode)
                {
                    case ClusterConnectionMode.ReplicaSet:
                        return ClusterType.ReplicaSet;
                    case ClusterConnectionMode.Sharded:
                        return ClusterType.Sharded;
                    case ClusterConnectionMode.Standalone:
                        return ClusterType.Standalone;
                    default:
                        return ClusterType.Unknown;
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
    }
}
