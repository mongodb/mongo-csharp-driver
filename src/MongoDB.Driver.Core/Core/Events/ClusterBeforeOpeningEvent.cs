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

using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;

namespace MongoDB.Driver.Core.Events
{
    public struct ClusterBeforeOpeningEvent
    {
        private readonly ClusterId _clusterId;
        private readonly ClusterSettings _clusterSettings;

        public ClusterBeforeOpeningEvent(ClusterId clusterId, ClusterSettings clusterSettings)
        {
            _clusterId = clusterId;
            _clusterSettings = clusterSettings;
        }

        public ClusterId ClusterId
        {
            get { return _clusterId; }
        }

        public ClusterSettings ClusterSettings
        {
            get { return _clusterSettings; }
        }
    }
}
