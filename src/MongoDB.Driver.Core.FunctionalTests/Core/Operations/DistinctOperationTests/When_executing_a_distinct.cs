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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.CountOperationTests
{
    [TestFixture]
    public class When_executing_a_distinct : CollectionUsingSpecification
    {
        private DistinctOperation<int> _subject;
        private IReadOnlyList<int> _result;

        protected override void Given()
        {
            Insert(new[] {
                new BsonDocument("x", 1).Add("y", 1),
                new BsonDocument("x", 2).Add("y", 1),
                new BsonDocument("x", 3).Add("y", 2),
                new BsonDocument("x", 4).Add("y", 2),
                new BsonDocument("x", 5).Add("y", 3),
                new BsonDocument("x", 6).Add("y", 3),
            });

            _subject = new DistinctOperation<int>(CollectionNamespace, new Int32Serializer(), "y", MessageEncoderSettings)
            {
                Criteria = BsonDocument.Parse("{ x : { $gt : 2 } }"),
            };
        }

        protected override void When()
        {
            _result = ExecuteOperation(_subject);
        }

        [Test]
        public void It_should_return_the_correct_values()
        {
            _result.Count.Should().Be(2);
            _result.Should().OnlyHaveUniqueItems();
            _result.Should().Contain(new[] { 2, 3 });
        }
    }
}
