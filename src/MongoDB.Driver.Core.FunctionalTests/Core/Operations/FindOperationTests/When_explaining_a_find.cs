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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Async;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.FindOperationTests
{
    [TestFixture]
    public class When_explaining_a_find : CollectionUsingSpecification
    {
        private BsonDocument _result;
        private IReadOperation<BsonDocument> _subject;

        protected override void Given()
        {
            _subject = new FindOperation<BsonDocument>(
                CollectionNamespace,
                BsonDocumentSerializer.Instance,
                MessageEncoderSettings)
                .ToExplainOperation(ExplainVerbosity.QueryPlanner);
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