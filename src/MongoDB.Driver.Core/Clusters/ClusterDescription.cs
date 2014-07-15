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
using MongoDB.Driver.Core.Servers;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents information about a cluster.
    /// </summary>
    public sealed class ClusterDescription
    {
        #region static
        // static methods
        public static ClusterDescription CreateDisposed(ClusterType type)
        {
            return new ClusterDescription(type, ClusterState.Disposed, Enumerable.Empty<ServerDescription>(), null, 0);
        }

        public static ClusterDescription CreateUninitialized(ClusterType type)
        {
            return new ClusterDescription(type, ClusterState.Uninitialized, Enumerable.Empty<ServerDescription>(), null, 0);
        }
        #endregion

        // fields
        private readonly ReplicaSetConfig _replicaSetConfig;
        private readonly int _revision;
        private readonly IReadOnlyList<ServerDescription> _servers;
        private readonly ClusterState _state;
        private readonly ClusterType _type;

        // constructors
        public ClusterDescription(
            ClusterType type,
            ClusterState state,
            IEnumerable<ServerDescription> servers,
            ReplicaSetConfig replicaSetConfig,
            int revision)
        {
            _type = type;
            _state = state;
            _servers = (servers ?? new ServerDescription[0]).OrderBy(n => n.EndPoint).ToList();
            _replicaSetConfig = replicaSetConfig; // can be null
            _revision = 0;
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
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj)) { return true; }
            if (obj == null || obj.GetType() != typeof(ClusterDescription)) { return false; }
            var rhs = (ClusterDescription)obj;
            return
                _type.Equals(rhs._type) &&
                _state.Equals(rhs._state) &&
                _servers.SequenceEqual(rhs._servers) &&
                object.Equals(_replicaSetConfig, rhs.ReplicaSetConfig);
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_type)
                .Hash(_state)
                .HashElements(_servers)
                .Hash(_replicaSetConfig)
                .GetHashCode();
        }

        public override string ToString()
        {
            var servers = string.Join(", ", _servers.Select(n => n.ToString()).ToArray());
            return string.Format("{{ Type : {0}, State : {1}, Servers : [{2}], ReplicaSetConfig : {3}, Revision : {4} }}", _type, _state, servers, _replicaSetConfig, _revision);
        }

        public ClusterDescription WithRevision(int value)
        {
            return _revision == value ? this : new ClusterDescription(_type, _state, _servers, _replicaSetConfig, value);
        }
    }
}
