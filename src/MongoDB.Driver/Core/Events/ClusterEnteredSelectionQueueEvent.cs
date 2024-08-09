/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters.ServerSelectors;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when entering selection wait queue.
    /// </summary>
    internal struct ClusterEnteredSelectionQueueEvent : IEvent
    {
        public ClusterDescription ClusterDescription { get; }
        public long? OperationId { get; }
        public string OperationName { get; }
        public IServerSelector ServerSelector { get; }
        public TimeSpan RemainingTimeout { get; }
        public DateTime Timestamp { get; }

        public ClusterId ClusterId => ClusterDescription.ClusterId;
        EventType IEvent.Type => EventType.ClusterEnteredSelectionWaitQueue;

        public ClusterEnteredSelectionQueueEvent(ClusterDescription clusterDescription, IServerSelector serverSelector, long? operationId, string operationName, TimeSpan remainingTimeout)
        {
            ClusterDescription = clusterDescription;
            ServerSelector = serverSelector;
            OperationId = operationId;
            OperationName = operationName;
            RemainingTimeout = remainingTimeout;
            Timestamp = DateTime.UtcNow;
        }
    }
}
