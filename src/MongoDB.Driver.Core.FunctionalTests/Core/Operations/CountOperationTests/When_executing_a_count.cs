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

using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.CountOperationTests
{
    [TestFixture]
    public class When_executing_a_count : CollectionUsingSpecification
    {
        private CountOperation _subject;
        private long _result;

        protected override void Given()
        {
            Insert(new[] {
                new BsonDocument("x", 1),
                new BsonDocument("x", 2),
                new BsonDocument("x", 3),
                new BsonDocument("x", 4),
                new BsonDocument("x", 5),
                new BsonDocument("x", 6),
            });

            _subject = new CountOperation(CollectionNamespace, MessageEncoderSettings)
            {
                Filter = BsonDocument.Parse("{ x : { $gt : 2 } }"),
                Limit = 2,
                Skip = 1
            };
        }

        protected override void When()
        {
            _result = ExecuteOperation(_subject);
        }

        [Test]
        public void It_should_return_the_correct_number_of_documents()
        {
            _result.Should().Be(2);
        }
    }
}
