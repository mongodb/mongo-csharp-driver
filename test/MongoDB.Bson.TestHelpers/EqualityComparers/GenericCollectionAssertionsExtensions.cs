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
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Collections;

namespace MongoDB.Bson.TestHelpers.EqualityComparers
{
    public static class GenericCollectionAssertionsExtensions
    {
        // static methods
        public static AndConstraint<GenericCollectionAssertions<T>> EqualUsing<T>(
            this GenericCollectionAssertions<T> assertions, IEnumerable<T> expectation, IEqualityComparer<T> comparer, string because = "", params object[] reasonArgs)
        {
            Func<T, T, bool> predicate = (x, y) =>
            {
                return comparer.Equals(x, y);
            };
          
            return assertions.Equal(expectation, predicate, because, reasonArgs);
        }

        public static AndConstraint<GenericCollectionAssertions<T>> EqualUsing<T>(
            this GenericCollectionAssertions<T> assertions, IEnumerable<T> expectation, IEqualityComparerSource source, string because = "", params object[] reasonArgs)
        {
            return assertions.EqualUsing(expectation, source.GetComparer<T>(), because, reasonArgs);
        }
    }
}
