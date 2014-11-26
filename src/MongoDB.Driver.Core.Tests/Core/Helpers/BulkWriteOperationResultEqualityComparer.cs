/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.Helpers
{
    public class BulkWriteOperationResultEqualityComparer : IEqualityComparer<BulkWriteOperationResult>
    {
        public bool Equals(BulkWriteOperationResult x, BulkWriteOperationResult y)
        {
            if ((object)x == (object)y)
            {
                return true;
            }

            if ((object)x == null || (object)y == null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            if (!x.ProcessedRequests.SequenceEqual(y.ProcessedRequests) ||
                !x.RequestCount.Equals(y.RequestCount))
            {
                return false;
            }

            if (x.GetType() == typeof(BulkWriteOperationResult.Unacknowledged))
            {
                return true;
            }

            return
                x.DeletedCount == y.DeletedCount &&
                x.InsertedCount == y.InsertedCount &&
                x.IsModifiedCountAvailable == y.IsModifiedCountAvailable &&
                x.MatchedCount == y.MatchedCount &&
                (!x.IsModifiedCountAvailable || x.ModifiedCount == y.ModifiedCount) &&
                x.ProcessedRequests.SequenceEqual(y.ProcessedRequests) &&
                x.RequestCount == y.RequestCount &&
                x.Upserts.SequenceEqual(y.Upserts);
        }

        public int GetHashCode(BulkWriteOperationResult x)
        {
            return 1;
        }
    }
}
