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
    public class When_upsert_matches_no_documents : CollectionUsingSpecification
    {
        private BsonDocument[] _expectedDocuments;
        private BulkWriteUpsert[] _expectedUpserts;
        private UpdateRequest[] _requests;
        private BulkWriteOperationResult _result;

        protected override void Given()
        {
            var initialDocuments = new[]
            {
                new BsonDocument { { "_id", 2 }, { "x", 2 } }
            };
            Insert(initialDocuments, MessageEncoderSettings);

            _requests = new[]
            {
                new UpdateRequest(new BsonDocument("_id", 1), new BsonDocument("$set", new BsonDocument("x", 1))) { IsMultiUpdate = true, IsUpsert = true }
            };

            _expectedDocuments = new[]
            {
                new BsonDocument { { "_id", 2 }, { "x", 2 } },
                new BsonDocument { { "_id", 1 }, { "x", 1 } }
            };

            _expectedUpserts = new[]
            {
                new BulkWriteUpsert(0, 1)
            };
        }

        protected override void When()
        {
            var subject = new BulkMixedWriteOperation(CollectionNamespace, _requests, MessageEncoderSettings);
            _result = ExecuteOperationAsync(subject).GetAwaiter().GetResult();
        }

        [Test]
        public void Result_should_have_the_expected_values()
        {
            _result.DeletedCount.Should().Be(0);
            _result.InsertedCount.Should().Be(0);
            if (_result.IsModifiedCountAvailable)
            {
                _result.ModifiedCount.Should().Be(0);
            }
            _result.MatchedCount.Should().Be(0);
            _result.ProcessedRequests.Should().Equal(_requests, SameAs.Predicate);
            _result.RequestCount.Should().Be(1);
            _result.Upserts.Should().Equal(_expectedUpserts, BulkWriteUpsertEqualityComparer.Equals);
        }

        [Test]
        public void Collection_should_contain_the_expected_documents()
        {
            var documents = ReadAll(MessageEncoderSettings);
            documents.Should().Equal(_expectedDocuments);
        }
    }
}
