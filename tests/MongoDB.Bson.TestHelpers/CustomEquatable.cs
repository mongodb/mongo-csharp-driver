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
using System.Linq;
using FluentAssertions;
using FluentAssertions.Collections;

namespace MongoDB.Bson.TestHelpers
{
    public class CustomEquatable<T>
    {
        private readonly IEqualityComparer<T> _comparer;
        private readonly T _value;

        public CustomEquatable(T value, IEqualityComparer<T> comparer)
        {
            _value = value;
            _comparer = comparer;
        }

        public override bool Equals(object obj)
        {
            T other;
            if (obj is T)
            {
                other = (T)obj;
            }
            else if (obj is CustomEquatable<T>)
            {
                other = ((CustomEquatable<T>)obj)._value;
            }
            else
            {
                return false;
            }

            return _comparer.Equals(_value, other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }

    public static class CustomEquatableExtensions
    {
        public static CustomEquatable<T> WithComparer<T>(this T value, IEqualityComparer<T> comparer)
        {
            return new CustomEquatable<T>(value, comparer);
        }

        public static IEnumerable<CustomEquatable<T>> WithComparer<T>(this IEnumerable<T> value, IEqualityComparer<T> comparer)
        {
            return value.Select(v => new CustomEquatable<T>(v, comparer));
        }

        public static void BeEquivalentToWithComparer<T>(
            this GenericCollectionAssertions<T> assertions,
            IEnumerable<T> expected,
            IEqualityComparer<T> comparer,
            string because = "",
            params object[] becauseArgs)
        {
            assertions.Subject.WithComparer(comparer).Should().BeEquivalentTo(expected.WithComparer(comparer), because, becauseArgs);         
        }
    }
}
