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
        internal static ClusterDescription CreateInitial(ClusterId clusterId, ClusterType clusterType)
        {
            return new ClusterDescription(
                clusterId,
                clusterType,
                Enumerable.Empty<ServerDescription>());
        }
        #endregion

        // fields
        private readonly ClusterId _clusterId;
        private readonly IReadOnlyList<ServerDescription> _servers;
        private readonly ClusterType _type;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterDescription"/> class.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        /// <param name="type">The type.</param>
        /// <param name="servers">The servers.</param>
        public ClusterDescription(
            ClusterId clusterId,
            ClusterType type,
            IEnumerable<ServerDescription> servers)
        {
            _clusterId = Ensure.IsNotNull(clusterId, "clusterId");
            _type = type;
            _servers = (servers ?? new ServerDescription[0]).OrderBy(n => n.EndPoint, new ToStringComparer<EndPoint>()).ToList();
        }

        // properties
        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        /// <value>
        /// The cluster identifier.
        /// </value>
        public ClusterId ClusterId
        {
            get { return _clusterId; }
        }

        /// <summary>
        /// Gets the servers.
        /// </summary>
        /// <value>
        /// The servers.
        /// </value>
        public IReadOnlyList<ServerDescription> Servers
        {
            get { return _servers; }
        }

        /// <summary>
        /// Gets the cluster state.
        /// </summary>
        /// <value>
        /// The cluster state.
        /// </value>
        public ClusterState State
        {
            get { return _servers.Any(x => x.State == ServerState.Connected) ? ClusterState.Connected : ClusterState.Disconnected; }
        }

        /// <summary>
        /// Gets the cluster type.
        /// </summary>
        /// <value>
        /// The cluster type.
        /// </value>
        public ClusterType Type
        {
            get { return _type; }
        }

        // methods
        /// <inheritdoc/>
        public bool Equals(ClusterDescription other)
        {
            if (other == null)
            {
                return false;
            }

            return
                _clusterId.Equals(other._clusterId) &&
                _servers.SequenceEqual(other._servers) &&
                _type == other._type;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ClusterDescription);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // ignore _revision
            return new Hasher()
                .Hash(_clusterId)
                .HashElements(_servers)
                .Hash(_type)
                .GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var servers = string.Join(", ", _servers.Select(n => n.ToString()).ToArray());
            return string.Format(
                "{{ ClusterId : \"{0}\", Type : \"{1}\", State : \"{2}\", Servers : [{3}] }}",
                _clusterId,
                _type,
                State,
                servers);
        }

        /// <summary>
        /// Returns a new ClusterDescription with a changed ServerDescription.
        /// </summary>
        /// <param name="value">The server description.</param>
        /// <returns>A ClusterDescription.</returns>
        public ClusterDescription WithServerDescription(ServerDescription value)
        {
            Ensure.IsNotNull(value, "value");

            IEnumerable<ServerDescription> replacementServers;

            var oldServerDescription = _servers.SingleOrDefault(s => s.EndPoint == value.EndPoint);
            if (oldServerDescription != null)
            {
                if (oldServerDescription.Equals(value))
                {
                    return this;
                }

                replacementServers = _servers.Select(s => s.EndPoint == value.EndPoint ? value : s);
            }
            else
            {
                replacementServers = _servers.Concat(new[] { value });
            }

            return new ClusterDescription(
                _clusterId,
                _type,
                replacementServers);
        }

        /// <summary>
        /// Returns a new ClusterDescription with a ServerDescription removed.
        /// </summary>
        /// <param name="endPoint">The end point of the server description to remove.</param>
        /// <returns>A ClusterDescription.</returns>
        public ClusterDescription WithoutServerDescription(EndPoint endPoint)
        {
            var oldServerDescription = _servers.SingleOrDefault(s => s.EndPoint == endPoint);
            if (oldServerDescription == null)
            {
                return this;
            }

            return new ClusterDescription(
                    _clusterId,
                    _type,
                    _servers.Where(s => !EndPointHelper.Equals(s.EndPoint, endPoint)));
        }

        /// <summary>
        /// Returns a new ClusterDescription with a changed ClusterType.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A ClusterDescription.</returns>
        public ClusterDescription WithType(ClusterType value)
        {
            return _type == value ? this : new ClusterDescription(_clusterId, value, _servers);
        }
    }
}
