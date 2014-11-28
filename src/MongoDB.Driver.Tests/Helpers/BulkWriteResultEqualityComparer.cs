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

using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Tests.Helpers
{
    public class BulkWriteResultEqualityComparer<T> : IEqualityComparer<BulkWriteResult<T>>
    {
        public bool Equals(BulkWriteResult<T> x, BulkWriteResult<T> y)
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

            // check BulkWriteResult fields
            if (x.RequestCount != y.RequestCount)
            {
                return false;
            }

            // check BulkWriteResult<T> fields
            if (!x.ProcessedRequests.SequenceEqual(y.ProcessedRequests, new WriteModelEqualityComparer<T>()))
            {
                return false;
            }

            // check BulkWriteResult<T>.Acknowledged fields
            if (x.IsAcknowledged)
            {
                if (
                    x.DeletedCount != y.DeletedCount ||
                    x.InsertedCount != y.InsertedCount ||
                    x.MatchedCount != y.MatchedCount ||
                    x.ModifiedCount != y.ModifiedCount ||
                    !x.Upserts.SequenceEqual(y.Upserts, new BulkWriteUpsertEqualityComparer()))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(BulkWriteResult<T> x)
        {
            return 1;
        }
    }
}
