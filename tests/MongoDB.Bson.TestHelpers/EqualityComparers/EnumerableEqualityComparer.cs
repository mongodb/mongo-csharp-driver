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

using System;
using System.Collections;
using System.Collections.Generic;

namespace MongoDB.Bson.TestHelpers.EqualityComparers
{
    public class EnumerableEqualityComparer : IEqualityComparer
    {
        // fields
        private readonly IEqualityComparerSource _source;

        // constructors
        public EnumerableEqualityComparer(IEqualityComparerSource source)
        {
            _source = source;
        }

        // methods
        public new bool Equals(object x, object y)
        {
            IEnumerator xEnumerator, yEnumerator;
            using ((xEnumerator = ((IEnumerable)x).GetEnumerator()) as IDisposable)
            using ((yEnumerator = ((IEnumerable)y).GetEnumerator()) as IDisposable)
            {
                bool xHasMore, yHasMore;
                while ((xHasMore = xEnumerator.MoveNext()) & (yHasMore = yEnumerator.MoveNext()))
                {
                    if (!ItemEquals(xEnumerator.Current, yEnumerator.Current))
                    {
                        return false;
                    }
                }

                return xHasMore == yHasMore;
            }
        }

        public int GetHashCode(object x)
        {
            return 1;
        }

        private bool ItemEquals(object xItem, object yItem)
        {
            if (xItem == null) { return yItem == null; }
            var itemComparer = _source.GetComparer(xItem.GetType());
            return itemComparer.Equals(xItem, yItem);
        }
    }

    public class EnumerableEqualityComparer<TValue, TItem> : IEqualityComparer<TValue>
        where TValue : IEnumerable<TItem>
    {
        // fields
        private readonly IEqualityComparerSource _source;

        // constructors
        public EnumerableEqualityComparer(IEqualityComparerSource source)
        {
            _source = source;
        }

        // methods
        public bool Equals(TValue x, TValue y)
        {
            using (var xEnumerator = x.GetEnumerator())
            using (var yEnumerator = y.GetEnumerator())
            {
                var itemComparer = _source.GetComparer<TItem>();
                bool xHasMore, yHasMore;
                while ((xHasMore = xEnumerator.MoveNext()) & (yHasMore = yEnumerator.MoveNext()))
                {
                    if (!itemComparer.Equals(xEnumerator.Current, yEnumerator.Current))
                    {
                        return false;
                    }
                }

                return xHasMore == yHasMore;
            }
        }

        public int GetHashCode(TValue x)
        {
            return 1;
        }
    }
}
