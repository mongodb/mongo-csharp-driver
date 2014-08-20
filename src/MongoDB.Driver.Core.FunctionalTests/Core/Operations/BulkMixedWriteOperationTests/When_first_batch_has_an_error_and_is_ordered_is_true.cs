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
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.BulkMixedWriteOperationTests
{
    [TestFixture]
    public class When_first_batch_has_an_error_and_is_ordered_is_true : CollectionUsingSpecification
    {
        private BulkWriteException _exception;
        private BsonDocument[] _expectedDocuments;
        private WriteRequest[] _requests;

        protected override void Given()
        {
            var keys = new BsonDocument("x", 1);
            var createIndexOperation = new CreateIndexOperation(DatabaseName, CollectionName, keys).WithUnique(true);
            ExecuteOperationAsync(createIndexOperation).GetAwaiter().GetResult();

            _requests = new WriteRequest[]
            {
                new InsertRequest(new BsonDocument { { "_id", 1 }, { "x", 1 } }),
                new InsertRequest(new BsonDocument { { "_id", 2 }, { "x", 1 } }), // will fail
                new InsertRequest(new BsonDocument { { "_id", 3 }, { "x", 1 } }),
            };

            _expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "_id", 1 }, { "x", 1 } }
            };
        }

        protected override void When()
        {
            var subject = new BulkMixedWriteOperation(DatabaseName, CollectionName, _requests)
            {
                IsOrdered = true
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
            result.InsertedCount.Should().Be(1);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().Equal(_requests.Take(2), SameAs.Predicate);
            result.RequestCount.Should().Be(3);
            result.Upserts.Should().BeEmpty();
        }

        [Test]
        public void UnprocessedRequests_should_have_the_expected_values()
        {
            _exception.UnprocessedRequests.Should().Equal(_requests.Skip(2), SameAs.Predicate);
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
            writeError.Index.Should().Be(1);
        }

        [Test]
        public void WriteErrors_should_have_one_error()
        {
            _exception.WriteErrors.Count().Should().Be(1);
        }

        [Test]
        public void Collection_should_contain_the_expected_documents()
        {
            var documents = ReadAll();
            documents.Should().Equal(_expectedDocuments);
        }
    }
}
