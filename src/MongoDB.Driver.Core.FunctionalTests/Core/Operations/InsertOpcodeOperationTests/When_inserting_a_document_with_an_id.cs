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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.InsertOpcodeOperationTests
{
    [TestFixture]
    public class When_inserting_a_document_with_an_id : CollectionUsingSpecification
    {
        private BsonDocument _document;
        private BsonValue _id = 1;
        private WriteConcernResult _result;

        protected override void Given()
        {
            _document = new BsonDocument("_id", _id);
        }

        protected override void When()
        {
            var documentSource = new BatchableSource<BsonDocument>(new[] { _document });
            var operation = new InsertOpcodeOperation<BsonDocument>(CollectionNamespace, documentSource, BsonDocumentSerializer.Instance, MessageEncoderSettings);

            _result = ExecuteOperationAsync(operation).GetAwaiter().GetResult();
        }

        [Test]
        public void Ok_should_be_true()
        {
            _result.Response["ok"].ToBoolean().Should().BeTrue();
        }

        [Test]
        public void The_document_should_exist_in_the_collection()
        {
            // TODO: implement
        }

        [Test]
        public void The_id_should_be_unchanged()
        {
            _document["_id"].Should().BeSameAs(_id);
        }
    }
}
