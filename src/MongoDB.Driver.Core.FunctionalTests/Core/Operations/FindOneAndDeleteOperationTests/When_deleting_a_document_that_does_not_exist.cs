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

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.FindOneAndDeleteOperationTests
{
    [TestFixture]
    public class When_deleting_a_document_that_does_not_exist : CollectionUsingSpecification
    {
        private FindOneAndDeleteOperation<BsonDocument> _subject;
        private BsonDocument _result;

        protected override void Given()
        {
            _subject = new FindOneAndDeleteOperation<BsonDocument>(
                CollectionNamespace,
                BsonDocument.Parse("{x: 1}"),
                new ElementDeserializer<BsonDocument>("value", BsonDocumentSerializer.Instance, deserializeNull: false),
                MessageEncoderSettings);
        }

        protected override void When()
        {
            _result = ExecuteOperation(_subject);
        }

        [Test]
        public void Result_should_be_null()
        {
            _result.Should().BeNull();
        }
    }
}
