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

using FluentAssertions;
using FluentAssertions.Common;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace MongoDB.Bson.TestHelpers
{
    public class BsonDocumentAssertions : ReferenceTypeAssertions<BsonDocument, BsonDocumentAssertions>
    {
        // constructors
        public BsonDocumentAssertions(BsonDocument value)
        {
            Subject = value;
        }

        // methods
        public AndConstraint<BsonDocumentAssertions> Be(BsonDocument expected, string because = "", params object[] reasonArgs)
        {
            Execute.Assertion
                .BecauseOf(because, reasonArgs)
                .ForCondition(Subject.IsSameOrEqualTo(expected))
                .FailWith("Expected {context:object} to be {0}{reason}, but found {1}.", expected,
                    Subject);

            return new AndConstraint<BsonDocumentAssertions>(this);
        }

        public AndConstraint<BsonDocumentAssertions> Be(string json, string because = "", params object[] reasonArgs)
        {
            return Be(BsonDocument.Parse(json), because, reasonArgs);
        }

        public AndConstraint<BsonDocumentAssertions> NotBe(BsonDocument unexpected, string because = "", params object[] reasonArgs)
        {
            Execute.Assertion
                .BecauseOf(because, reasonArgs)
                .ForCondition(!Subject.IsSameOrEqualTo(unexpected))
                .FailWith("Did not expect {context:object} to be equal to {0}{reason}.", unexpected);

            return new AndConstraint<BsonDocumentAssertions>(this);
        }

        public AndConstraint<BsonDocumentAssertions> NotBe(string json, string because = "", params object[] reasonArgs)
        {
            return NotBe(BsonDocument.Parse(json), because, reasonArgs);
        }

        protected override string Context
        {
            get { return "BsonDocument"; }
        }
    }
}
