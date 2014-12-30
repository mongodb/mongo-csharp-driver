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
        private readonly ClusterConnectionMode _connectionMode;
        private readonly IReadOnlyList<EndPoint> _endPoints;
        private readonly int _maxServerSelectionWaitQueueSize;
        private readonly string _replicaSetName;
        private readonly TimeSpan _serverSelectionTimeout;
        private readonly IServerSelector _preServerSelector;
        private readonly IServerSelector _postServerSelector;

        // constructors
        public ClusterSettings(
            Optional<ClusterConnectionMode> connectionMode = default(Optional<ClusterConnectionMode>),
            Optional<IEnumerable<EndPoint>> endPoints = default(Optional<IEnumerable<EndPoint>>),
            Optional<int> maxServerSelectionWaitQueueSize = default(Optional<int>),
            Optional<string> replicaSetName = default(Optional<string>),
            Optional<TimeSpan> serverSelectionTimeout = default(Optional<TimeSpan>),
            Optional<IServerSelector> preServerSelector = default(Optional<IServerSelector>),
            Optional<IServerSelector> postServerSelector = default(Optional<IServerSelector>))
        {
            _connectionMode = connectionMode.WithDefault(ClusterConnectionMode.Automatic);
            _endPoints = Ensure.IsNotNull(endPoints.WithDefault(__defaultEndPoints), "endPoints").ToList();
            _maxServerSelectionWaitQueueSize = Ensure.IsGreaterThanOrEqualToZero(maxServerSelectionWaitQueueSize.WithDefault(500), "maxServerSelectionWaitQueueSize");
            _replicaSetName = replicaSetName.WithDefault(null);
            _serverSelectionTimeout = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(serverSelectionTimeout.WithDefault(TimeSpan.FromSeconds(30)), "serverSelectionTimeout");
            _preServerSelector = preServerSelector.WithDefault(null);
            _postServerSelector = postServerSelector.WithDefault(null);
        }

        // properties
        public ClusterConnectionMode ConnectionMode
        {
            get { return _connectionMode; }
        }

        public IReadOnlyList<EndPoint> EndPoints
        {
            get { return _endPoints; }
        }

        public int MaxServerSelectionWaitQueueSize
        {
            get { return _maxServerSelectionWaitQueueSize; }
        }

        public string ReplicaSetName
        {
            get { return _replicaSetName; }
        }

        public TimeSpan ServerSelectionTimeout
        {
            get { return _serverSelectionTimeout; }
        }

        public IServerSelector PreServerSelector
        {
            get { return _preServerSelector; }
        }

        public IServerSelector PostServerSelector
        {
            get { return _postServerSelector; }
        }

        // methods
        public ClusterSettings With(
            Optional<ClusterConnectionMode> connectionMode = default(Optional<ClusterConnectionMode>),
            Optional<IEnumerable<EndPoint>> endPoints = default(Optional<IEnumerable<EndPoint>>),
            Optional<int> maxServerSelectionWaitQueueSize = default(Optional<int>),
            Optional<string> replicaSetName = default(Optional<string>),
            Optional<TimeSpan> serverSelectionTimeout = default(Optional<TimeSpan>),
            Optional<IServerSelector> preServerSelector = default(Optional<IServerSelector>),
            Optional<IServerSelector> postServerSelector = default(Optional<IServerSelector>))
        {
            return new ClusterSettings(
                connectionMode: connectionMode.WithDefault(_connectionMode),
                endPoints: Optional.Create(endPoints.WithDefault(_endPoints)),
                maxServerSelectionWaitQueueSize: maxServerSelectionWaitQueueSize.WithDefault(_maxServerSelectionWaitQueueSize),
                replicaSetName: replicaSetName.WithDefault(_replicaSetName),
                serverSelectionTimeout: serverSelectionTimeout.WithDefault(_serverSelectionTimeout),
                preServerSelector: Optional.Create(preServerSelector.WithDefault(_preServerSelector)),
                postServerSelector: Optional.Create(postServerSelector.WithDefault(_postServerSelector)));
        }
    }
}
