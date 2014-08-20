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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Clusters;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents settings for a cluster.
    /// </summary>
    public class ClusterSettings
    {
        #region static
        // static fields
        private readonly IReadOnlyList<EndPoint> __defaultEndPoints = new EndPoint[] { new DnsEndPoint("localhost", 27017) };
        #endregion

        // fields
        private readonly ClusterConnectionMode _connectionMode;
        private readonly IReadOnlyList<EndPoint> _endPoints;
        private readonly string _replicaSetName;

        // constructors
        public ClusterSettings()
        {
            _endPoints = __defaultEndPoints;
        }

        internal ClusterSettings(
            ClusterConnectionMode connectionMode,
            IReadOnlyList<EndPoint> endPoints,
            string replicaSetName)
        {
            _connectionMode = connectionMode;
            _endPoints = endPoints;
            _replicaSetName = replicaSetName;
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

        public string ReplicaSetName
        {
            get { return _replicaSetName; }
        }

        // methods
        public ClusterSettings WithConnectionMode(ClusterConnectionMode value)
        {
            return (_connectionMode == value) ? this : new Builder(this) { _connectionMode = value }.Build();
        }

        public ClusterSettings WithEndPoints(IEnumerable<EndPoint> value)
        {
            var list = value.ToList();
            return _endPoints.SequenceEqual(list) ? this : new Builder(this) { _endPoints = list }.Build();
        }

        public ClusterSettings WithReplicaSetName(string value)
        {
            return object.Equals(_replicaSetName, value) ? this : new Builder(this) { _replicaSetName = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public ClusterConnectionMode _connectionMode;
            public IReadOnlyList<EndPoint> _endPoints;
            public string _replicaSetName;

            // constructors
            public Builder(ClusterSettings other)
            {
                _connectionMode = other._connectionMode;
                _endPoints = other._endPoints;
                _replicaSetName = other._replicaSetName;
            }

            // methods
            public ClusterSettings Build()
            {
                return new ClusterSettings(
                    _connectionMode,
                    _endPoints,
                    _replicaSetName);
            }
        }
    }
}
