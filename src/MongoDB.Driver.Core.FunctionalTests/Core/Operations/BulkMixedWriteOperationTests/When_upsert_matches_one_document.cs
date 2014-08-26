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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.BulkMixedWriteOperationTests
{
    [TestFixture]
    public class When_upsert_matches_one_document : CollectionUsingSpecification
    {
        private BsonDocument[] _expectedDocuments;
        private MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();
        private UpdateRequest[] _requests;
        private BulkWriteResult _result;

        protected override void Given()
        {
            var initialDocuments = new[]
            {
                new BsonDocument { { "_id", 1 }, { "x", 1 } },
                new BsonDocument { { "_id", 2 }, { "x", 2 } }
            };
            Insert(initialDocuments, _messageEncoderSettings);

            _requests = new[]
            {
                new UpdateRequest(new BsonDocument("_id", 1), new BsonDocument("$set", new BsonDocument("x", 3))) { IsMultiUpdate = true, IsUpsert = true }
            };

            _expectedDocuments = new[]
            {
                new BsonDocument { { "_id", 1 }, { "x", 3 } },
                new BsonDocument { { "_id", 2 }, { "x", 2 } }
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
            _result.DeletedCount.Should().Be(0);
            _result.InsertedCount.Should().Be(0);
            if (_result.IsModifiedCountAvailable)
            {
                _result.ModifiedCount.Should().Be(1);
            }
            _result.MatchedCount.Should().Be(1);
            _result.ProcessedRequests.Should().Equal(_requests, SameAs.Predicate);
            _result.RequestCount.Should().Be(1);
            _result.Upserts.Should().BeEmpty();
        }

        [Test]
        public void Collection_should_contain_the_expected_documents()
        {
            var documents = ReadAll(_messageEncoderSettings);
            documents.Should().Equal(_expectedDocuments);
        }
    }
}
