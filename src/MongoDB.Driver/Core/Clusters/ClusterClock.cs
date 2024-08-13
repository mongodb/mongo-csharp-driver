/* Copyright 2017-present MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Clusters
{
    internal sealed class ClusterClock : IClusterClock
    {
        #region static

        public static BsonDocument GreaterClusterTime(BsonDocument x, BsonDocument y)
        {
            if (x == null)
            {
                return y;
            }
            else if (y == null)
            {
                return x;
            }
            else
            {
                var xTimestamp = x["clusterTime"].AsBsonTimestamp;
                var yTimestamp = y["clusterTime"].AsBsonTimestamp;
                return xTimestamp > yTimestamp ? x : y;
            }
        }

        #endregion

        private BsonDocument _clusterTime;

        public BsonDocument ClusterTime => _clusterTime;

        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
            Ensure.IsNotNull(newClusterTime, nameof(newClusterTime));
            _clusterTime = GreaterClusterTime(_clusterTime, newClusterTime);
        }
    }
   
    internal sealed class NoClusterClock : IClusterClock
    {
        public BsonDocument ClusterTime => null;

        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
        }
    }
}
