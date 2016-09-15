/* Copyright 2016 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Servers
{
    public class ServerDescriptionWithSimilarLastUpdateTimestampEqualityComparer : IEqualityComparer<ServerDescription>
    {
        private static IEqualityComparer<ServerDescription> __instance = new ServerDescriptionWithSimilarLastUpdateTimestampEqualityComparer();

        public static IEqualityComparer<ServerDescription> Instance => __instance;

        public bool Equals(ServerDescription x, ServerDescription y)
        {
            var lastUpdateTimestampDelta = Math.Abs((x.LastUpdateTimestamp - y.LastUpdateTimestamp).TotalMilliseconds);
            var tolerance = 10.0;
            return
                x.AverageRoundTripTime.Equals(y.AverageRoundTripTime) &&
                object.Equals(x.CanonicalEndPoint, y.CanonicalEndPoint) &&
                object.Equals(x.ElectionId, y.ElectionId) &&
                EndPointHelper.Equals(x.EndPoint, y.EndPoint) &&
                object.Equals(x.HeartbeatException, y.HeartbeatException) &&
                x.HeartbeatInterval.Equals(y.HeartbeatInterval) &&
                lastUpdateTimestampDelta <= tolerance &&
                x.LastWriteTimestamp.Equals(y.LastWriteTimestamp) &&
                x.MaxBatchCount.Equals(y.MaxBatchCount) &&
                x.MaxDocumentSize.Equals(y.MaxDocumentSize) &&
                x.MaxMessageSize.Equals(y.MaxMessageSize) &&
                x.MaxWireDocumentSize.Equals(y.MaxWireDocumentSize) &&
                object.Equals(x.ReplicaSetConfig, y.ReplicaSetConfig) &&
                x.ServerId.Equals(y.ServerId) &&
                x.State.Equals(y.State) &&
                object.Equals(x.Tags, y.Tags) &&
                x.Type.Equals(y.Type) &&
                object.Equals(x.Version, y.Version) &&
                object.Equals(x.WireVersionRange, y.WireVersionRange);
        }

        public int GetHashCode(ServerDescription obj)
        {
            return 0;
        }
    }
}
