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
    public class When_explaining_a_count : CollectionUsingSpecification
    {
        private IReadOperation<BsonDocument> _subject;
        private BsonDocument _result;

        protected override void Given()
        {
            Require.MinimumServerVersion("2.7.6");

            _subject = new CountOperation(CollectionNamespace, MessageEncoderSettings)
            {
                Criteria = BsonDocument.Parse("{ x : { $gt : 2 } }"),
                Limit = 2,
                Skip = 1
            }.ToExplainOperation(ExplainVerbosity.QueryPlanner);
        }

        protected override void When()
        {
            _result = ExecuteOperation(_subject);
        }

        [Test]
        public void Result_should_not_be_null()
        {
            _result.Should().NotBeNull();
        }
    }
}
