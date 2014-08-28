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
    public class When_second_batch_has_an_error_and_isOrdered_is_false : CollectionUsingSpecification
    {
        private BsonDocument[] _documents;
        private BulkWriteException _exception;
        private InsertRequest[] _requests;

        protected override void Given()
        {
            _documents = new[]
            {
                new BsonDocument { { "_id", 1 }, { "x", 1 } },
                new BsonDocument { { "_id", 2 }, { "x", 2 } },
                new BsonDocument { { "_id", 3 }, { "x", 3 } },
                new BsonDocument { { "_id", 1 }, { "x", 4 } }, // will fail
                new BsonDocument { { "_id", 5 }, { "x", 5 } }
            };

            _requests = _documents.Select(d => new InsertRequest(d, BsonDocumentSerializer.Instance)).ToArray();
        }

        protected override void When()
        {
            var subject = new BulkMixedWriteOperation(CollectionNamespace, _requests, MessageEncoderSettings)
            {
                IsOrdered = false,
                MaxBatchCount = 2,
            };
            _exception = Catch<BulkWriteException>(() => ExecuteOperationAsync(subject).GetAwaiter().GetResult());
        }

        [Test]
        public void ExecuteOperationAsync_should_throw_a_BulkWriteException()
        {
            _exception.Should().NotBeNull();
            _exception.Should().BeOfType<BulkWriteException>();
        }

        [Test]
        public void Result_should_have_the_expected_values()
        {
            var result = _exception.Result;
            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(4);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().Equal(_requests, SameAs.Predicate);
            result.RequestCount.Should().Be(_requests.Length);
            result.Upserts.Should().BeEmpty();
        }

        [Test]
        public void UnprocessedRequests_should_be_empty()
        {
            _exception.UnprocessedRequests.Should().BeEmpty();
        }

        [Test]
        public void WriteConcernError_should_be_null()
        {
            _exception.WriteConcernError.Should().BeNull();
        }

        [Test]
        public void WriteError_should_have_the_expected_values()
        {
            var writeError = _exception.WriteErrors[0];
            writeError.Code.Should().Be(11000);
            writeError.Index.Should().Be(3);
        }

        [Test]
        public void WriteErrors_should_have_one_error()
        {
            _exception.WriteErrors.Count().Should().Be(1);
        }

        [Test]
        public void Collection_should_contain_the_expected_documents()
        {
            var documents = ReadAll(MessageEncoderSettings);
            documents.Should().Equal(_documents.Where(d => d["x"] != 4));
        }
    }
}
