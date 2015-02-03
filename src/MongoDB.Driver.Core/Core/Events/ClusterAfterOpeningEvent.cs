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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;

namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents information about a ClusterAfterOpening event.
    /// </summary>
    public struct ClusterAfterOpeningEvent
    {
        private readonly ClusterId _clusterId;
        private readonly ClusterSettings _clusterSettings;
        private readonly TimeSpan _elapsed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterAfterOpeningEvent"/> struct.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        /// <param name="clusterSettings">The cluster settings.</param>
        /// <param name="elapsed">The elapsed time.</param>
        public ClusterAfterOpeningEvent(ClusterId clusterId, ClusterSettings clusterSettings, TimeSpan elapsed)
        {
            _clusterId = clusterId;
            _clusterSettings = clusterSettings;
            _elapsed = elapsed;
        }

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
        /// Gets the cluster settings.
        /// </summary>
        /// <value>
        /// The cluster settings.
        /// </value>
        public ClusterSettings ClusterSettings
        {
            get { return _clusterSettings; }
        }

        /// <summary>
        /// Gets the elapsed time.
        /// </summary>
        /// <value>
        /// The elapsed time.
        /// </value>
        public TimeSpan Elapsed
        {
            get { return _elapsed; }
        }
    }
}
