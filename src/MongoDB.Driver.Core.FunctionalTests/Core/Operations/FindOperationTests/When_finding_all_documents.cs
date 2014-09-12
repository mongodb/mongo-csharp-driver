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
    public class When_finding_all_documents : CollectionUsingSpecification
    {
        private IAsyncCursor<BsonDocument> _result;
        private FindOperation<BsonDocument> _subject;

        protected override void Given()
        {
            _subject = new FindOperation<BsonDocument>(
                CollectionNamespace,
                BsonDocumentSerializer.Instance,
                MessageEncoderSettings);

            Insert(new[] 
            {
                BsonDocument.Parse("{_id: 1, x: 1}"),
                BsonDocument.Parse("{_id: 2, x: 2}"),
                BsonDocument.Parse("{_id: 3, x: 3}"),
                BsonDocument.Parse("{_id: 4, x: 4}"),
                BsonDocument.Parse("{_id: 5, x: 5}"),
                BsonDocument.Parse("{_id: 6, x: 6}"),
            });
        }

        protected override void When()
        {
            _result = ExecuteOperation(_subject);
        }

        [Test]
        public void Result_should_include_all_documents()
        {
            var list = ReadCursorToEnd(_result);

            list.Count.Should().Be(6);
        }

    }
}