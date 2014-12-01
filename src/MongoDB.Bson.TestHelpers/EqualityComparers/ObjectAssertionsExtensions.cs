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
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace MongoDB.Bson.TestHelpers.EqualityComparers
{
    public static class ObjectAssertionsExtensions
    {
        // static methods
        public static AndConstraint<ObjectAssertions> BeUsing(this ObjectAssertions assertions, object expected, IEqualityComparer comparer, string because = "", params object[] reasonArgs)
        {
            Execute.Assertion
                .BecauseOf(because, reasonArgs)
                .ForCondition(comparer.Equals(assertions.Subject, expected))
                .FailWith("Expected {context:object} to be {0}{reason}, but found {1}.", expected,
                    assertions.Subject);

            return new AndConstraint<ObjectAssertions>(assertions);
        }

        public static AndConstraint<ObjectAssertions> BeUsing(this ObjectAssertions assertions, object expected, IEqualityComparerSource source, string because = "", params object[] reasonArgs)
        {
            var actual = assertions.Subject;
            var comparer = actual == null ? ReferenceEqualsEqualityComparer.Instance : source.GetComparer(actual.GetType());
            return assertions.BeUsing(expected, comparer, because, reasonArgs);
        }

        public static AndConstraint<ObjectAssertions> NotBeUsing(this ObjectAssertions assertions, object unexpected, IEqualityComparer comparer, string because = "", params object[] reasonArgs)
        {
            Execute.Assertion
                .BecauseOf(because, reasonArgs)
                .ForCondition(!comparer.Equals(assertions.Subject, unexpected))
                .FailWith("Did not expect {context:object} to be equal to {0}{reason}.", unexpected);

            return new AndConstraint<ObjectAssertions>(assertions);
        }

        public static AndConstraint<ObjectAssertions> NotBeUsing(this ObjectAssertions assertions, object unexpected, IEqualityComparerSource source, string because = "", params object[] reasonArgs)
        {
            var actual = assertions.Subject;
            var comparer = actual == null ? ReferenceEqualsEqualityComparer.Instance : source.GetComparer(actual.GetType());
            return assertions.NotBeUsing(unexpected, comparer, because, reasonArgs);
        }
    }
}
