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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents information about a cluster.
    /// </summary>
    public sealed class ClusterDescription : IEquatable<ClusterDescription>
    {
        #region static
        // static methods
        public static ClusterDescription CreateDisposed(ClusterId clusterId, ClusterType type)
        {
            return new ClusterDescription(clusterId, type, ClusterState.Disposed, Enumerable.Empty<ServerDescription>(), null, 0);
        }

        public static ClusterDescription CreateUninitialized(ClusterId clusterId, ClusterType type)
        {
            return new ClusterDescription(clusterId, type, ClusterState.Uninitialized, Enumerable.Empty<ServerDescription>(), null, 0);
        }
        #endregion

        // fields
        private readonly ClusterId _clusterId;
        private readonly ReplicaSetConfig _replicaSetConfig;
        private readonly int _revision;
        private readonly IReadOnlyList<ServerDescription> _servers;
        private readonly ClusterState _state;
        private readonly ClusterType _type;

        // constructors
        public ClusterDescription(
            ClusterId clusterId,
            ClusterType type,
            ClusterState state,
            IEnumerable<ServerDescription> servers,
            ReplicaSetConfig replicaSetConfig,
            int revision)
        {
            _clusterId = Ensure.IsNotNull(clusterId, "clusterId");
            _type = type;
            _state = state;
            _servers = (servers ?? new ServerDescription[0]).OrderBy(n => n.EndPoint, new ToStringComparer<EndPoint>()).ToList();
            _replicaSetConfig = replicaSetConfig; // can be null
            _revision = revision;
        }

        // properties
        public ClusterId ClusterId
        {
            get { return _clusterId; }
        }

        public ReplicaSetConfig ReplicaSetConfig
        {
            get { return _replicaSetConfig; }
        }

        public int Revision
        {
            get { return _revision; }
        }

        public IReadOnlyList<ServerDescription> Servers
        {
            get { return _servers; }
        }

        public ClusterState State
        {
            get { return _state; }
        }

        public ClusterType Type
        {
            get { return _type; }
        }

        // methods
        public bool Equals(ClusterDescription other)
        {
            if (other == null)
            {
                return false;
            }

            // ignore _revision
            return
                _clusterId.Equals(other._clusterId) &&
                object.Equals(_replicaSetConfig, other.ReplicaSetConfig) &&
                _servers.SequenceEqual(other._servers) &&
                _state == other._state &&
                _type == other._type;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ClusterDescription);
        }

        public override int GetHashCode()
        {
            // ignore _revision
            return new Hasher()
                .Hash(_clusterId)
                .Hash(_replicaSetConfig)
                .HashElements(_servers)
                .Hash(_state)
                .Hash(_type)
                .GetHashCode();
        }

        public override string ToString()
        {
            var servers = string.Join(", ", _servers.Select(n => n.ToString()).ToArray());
            return string.Format(
                "{{ ClusterId : {0}, Type : {1}, State : {2}, Servers : [{3}], ReplicaSetConfig : {4}, Revision : {5} }}",
                _clusterId,
                _type,
                _state,
                servers,
                _replicaSetConfig == null ? "null" : _replicaSetConfig.ToString(),
                _revision);
        }

        public ClusterDescription WithRevision(int value)
        {
            return _revision == value ? this : new ClusterDescription(_clusterId, _type, _state, _servers, _replicaSetConfig, value);
        }

        public ClusterDescription WithServerDescription(ServerDescription value)
        {
            Ensure.IsNotNull(value, "value");

            var oldServerDescription = _servers.SingleOrDefault(s => s.EndPoint == value.EndPoint);
            if (oldServerDescription == null)
            {
                var message = string.Format("No server description found: '{0}'.", value.EndPoint);
                throw new ArgumentException(message, "value");
            }

            return oldServerDescription.Equals(value) ? this :
                new ClusterDescription(
                    _clusterId,
                    _type,
                    _state,
                    _servers.Select(s => s.EndPoint == value.EndPoint ? value : s),
                    _replicaSetConfig,
                    0);
        }

        public ClusterDescription WithType(ClusterType value)
        {
            return _type == value ? this : new ClusterDescription(_clusterId, value, _state, _servers, _replicaSetConfig, 0);
        }
    }
}
