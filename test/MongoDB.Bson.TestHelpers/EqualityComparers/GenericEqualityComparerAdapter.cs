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

using System.Collections;
using System.Collections.Generic;

namespace MongoDB.Bson.TestHelpers.EqualityComparers
{
    public class GenericEqualityComparerAdapter<T> : IEqualityComparer<T>
    {
        // fields
        private readonly IEqualityComparer _nonGenericComparer;

        // constructors
        public GenericEqualityComparerAdapter(IEqualityComparer nonGenericComparer)
        {
            _nonGenericComparer = nonGenericComparer;
        }

        // methods
        public bool Equals(T x, T y)
        {
            return _nonGenericComparer.Equals(x, y);
        }

        public int GetHashCode(T x)
        {
            return _nonGenericComparer.GetHashCode(x);
        }
    }
}
