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
using MongoDB.Driver.Core.Clusters;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs before a server is added to the cluster.
    /// </summary>
    public struct ClusterAddingServerEvent
    {
        private readonly ClusterId _clusterId;
        private readonly EndPoint _endPoint;
        private readonly DateTime _timestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterAddingServerEvent"/> struct.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        /// <param name="endPoint">The end point.</param>
        public ClusterAddingServerEvent(ClusterId clusterId, EndPoint endPoint)
        {
            _clusterId = clusterId;
            _endPoint = endPoint;
            _timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        public ClusterId ClusterId
        {
            get { return _clusterId; }
        }

        /// <summary>
        /// Gets the end point.
        /// </summary>
        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
        }
    }
}
