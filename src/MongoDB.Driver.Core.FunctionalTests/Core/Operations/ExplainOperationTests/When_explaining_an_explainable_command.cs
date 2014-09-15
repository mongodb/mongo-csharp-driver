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

namespace MongoDB.Driver.Core.Operations.ExplainOperationTests
{
    [TestFixture]
    public class When_explaining_an_explainable_command : CollectionUsingSpecification
    {
        private ExplainOperation _subject;
        private BsonDocument _result;

        protected override void Given()
        {
            Require.MinimumServerVersion("2.7.6");

            var command = new BsonDocument("count", CollectionNamespace.CollectionName);
            _subject = new ExplainOperation(DatabaseNamespace, command, MessageEncoderSettings);
        }

        protected override void When()
        {
            _result = ExecuteOperation((IReadOperation<BsonDocument>)_subject);
        }

        [Test]
        public void It_should_return_a_non_null_result()
        {
            _result.Should().NotBeNull();
        }
    }
}
