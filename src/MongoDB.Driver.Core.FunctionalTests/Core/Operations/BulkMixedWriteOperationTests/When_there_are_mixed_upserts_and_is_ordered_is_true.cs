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
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.BulkMixedWriteOperationTests
{
    [TestFixture]
    public class When_there_are_mixed_upserts_and_is_ordered_is_true : CollectionUsingSpecification
    {
        private BsonDocument[] _expectedDocuments;
        private BulkWriteUpsert[] _expectedUpserts;
        private MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();
        private WriteRequest[] _requests;
        private BulkWriteResult _result;

        protected override void Given()
        {
            var id = ObjectId.GenerateNewId();
            _requests = new WriteRequest[]
            {
                new UpdateRequest(new BsonDocument("_id", id), new BsonDocument("$set", new BsonDocument("x", 1))) { IsUpsert = true },
                new DeleteRequest(new BsonDocument("_id", id)),
                new UpdateRequest(new BsonDocument("_id", id), new BsonDocument("$set", new BsonDocument("x", 2))) { IsUpsert = true },
                new DeleteRequest(new BsonDocument("_id", id)),
                new UpdateRequest(new BsonDocument("_id", id), new BsonDocument("$set", new BsonDocument("x", 3))) { IsUpsert = true }
            };

            _expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "_id", id }, { "x", 3 } }
            };

            _expectedUpserts = new BulkWriteUpsert[]
            {
                new BulkWriteUpsert(0, id),
                new BulkWriteUpsert(2, id),
                new BulkWriteUpsert(4, id)
            };
        }

        protected override void When()
        {
            var subject = new BulkMixedWriteOperation(DatabaseName, CollectionName, _requests, _messageEncoderSettings);
            _result = ExecuteOperationAsync(subject).GetAwaiter().GetResult();
        }

        [Test]
        public void Result_should_have_the_expected_values()
        {
            _result.DeletedCount.Should().Be(2);
            _result.InsertedCount.Should().Be(0);
            if (_result.IsModifiedCountAvailable)
            {
                _result.ModifiedCount.Should().Be(0);
            }
            _result.MatchedCount.Should().Be(0);
            _result.ProcessedRequests.Should().Equal(_requests, SameAs.Predicate);
            _result.RequestCount.Should().Be(5);
            _result.Upserts.Should().Equal(_expectedUpserts, BulkWriteUpsertEqualityComparer.Equals);
        }

        [Test]
        public void Collection_should_contain_the_expected_documents()
        {
            var documents = ReadAll(_messageEncoderSettings);
            documents.Should().Equal(_expectedDocuments);
        }
    }
}
