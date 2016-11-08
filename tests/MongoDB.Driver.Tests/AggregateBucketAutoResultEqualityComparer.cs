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

using System.Collections.Generic;

namespace MongoDB.Driver.Tests
{
    public class AggregateBucketAutoResultEqualityComparer<TValue> : IEqualityComparer<AggregateBucketAutoResult<TValue>>
    {
        private static readonly AggregateBucketAutoResultEqualityComparer<TValue> __instance = new AggregateBucketAutoResultEqualityComparer<TValue>();

        public static AggregateBucketAutoResultEqualityComparer<TValue> Instance => __instance;

        public bool Equals(AggregateBucketAutoResult<TValue> x, AggregateBucketAutoResult<TValue> y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return false;
            }

            return
                x.Id.Min.Equals(y.Id.Min) &&
                x.Id.Max.Equals(y.Id.Max) &&
                x.Count == y.Count;
        }

        public int GetHashCode(AggregateBucketAutoResult<TValue> obj)
        {
            return 0;
        }
    }
}
