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
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.BulkMixedWriteOperationTests
{
    [TestFixture]
    public class When_there_are_more_deletes_than_maxBatchCount : CollectionUsingSpecification
    {
        private BsonDocument[] _documents;
        private int _maxBatchCount;
        private DeleteRequest[] _requests;
        private BulkWriteResult _result;

        protected override void Given()
        {
            _documents = new[]
            {
                new BsonDocument { { "_id", 1 }, { "x", 1 } },
                new BsonDocument { { "_id", 2 }, { "x", 2 } },
                new BsonDocument { { "_id", 3 }, { "x", 3 } }
            };
            Insert(_documents);

            _maxBatchCount = 2;
            _requests = _documents.Select(d => new DeleteRequest(new BsonDocument("_id", d["_id"]))).ToArray();
        }

        protected override void When()
        {
            var subject = new BulkMixedWriteOperation(DatabaseName, CollectionName, _requests)
            {
                MaxBatchCount = _maxBatchCount,
            };
            _result = ExecuteOperationAsync(subject).GetAwaiter().GetResult();
        }

        [Test]
        public void Result_should_have_the_expected_values()
        {
            _result.DeletedCount.Should().Be(3);
            _result.InsertedCount.Should().Be(0);
            if (_result.IsModifiedCountAvailable)
            {
                _result.ModifiedCount.Should().Be(0);
            }
            _result.MatchedCount.Should().Be(0);
            _result.ProcessedRequests.Should().Equal(_requests, SameAs.Predicate);
            _result.RequestCount.Should().Be(3);
            _result.Upserts.Should().BeEmpty();
        }

        [Test]
        public void Collection_should_contain_the_expected_documents()
        {
            var documents = ReadAll();
            documents.Should().BeEmpty();
        }
    }
}
