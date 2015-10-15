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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class CreateIndexesOperationTests : OperationTestBase
    {
        [Test]
        public void CollectionNamespace_get_should_return_expected_value()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(_collectionNamespace);
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Requests.Should().ContainInOrder(requests);
            subject.WriteConcern.Should().Be(WriteConcern.Acknowledged);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public void Execute_should_work_when_background_is_true(
            [Values(false, true)]
            bool async)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Background = true } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes(async);
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["background"].ToBoolean().Should().BeTrue();
        }

        [Test]
        [RequiresServer("DropCollection")]
        public void Execute_should_work_when_creating_one_index(
            [Values(false, true)]
            bool async)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes(async);
            indexes.Select(index => index["name"].AsString).Should().BeEquivalentTo(new[] { "_id_", "x_1" });
        }

        [Test]
        [RequiresServer("DropCollection")]
        public void Execute_should_work_when_creating_two_indexes(
            [Values(false, true)]
            bool async)
        {
            var requests = new[]
            {
                new CreateIndexRequest(new BsonDocument("x", 1)),
                new CreateIndexRequest(new BsonDocument("y", 1))
            };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes(async);
            indexes.Select(index => index["name"].AsString).Should().BeEquivalentTo(new[] { "_id_", "x_1", "y_1" });
        }

        [Test]
        [RequiresServer("DropCollection", MinimumVersion = "3.1.1")]
        public void Execute_should_work_when_partialFilterExpression_is_has_value(
            [Values(false, true)]
            bool async)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { PartialFilterExpression = new BsonDocument("x", new BsonDocument("$gt", 0)) } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes(async);
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["partialFilterExpression"].AsBsonDocument.Should().Be(requests[0].PartialFilterExpression);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public void Execute_should_work_when_sparse_is_true(
            [Values(false, true)]
            bool async)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Sparse = true } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes(async);
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["sparse"].ToBoolean().Should().BeTrue();
        }

        [Test]
        [RequiresServer("DropCollection")]
        public void Execute_should_work_when_expireAfter_has_value(
            [Values(false, true)]
            bool async)
        {
            var expireAfterSeconds = 1.5;
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { ExpireAfter = TimeSpan.FromSeconds(expireAfterSeconds) } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes(async);
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["expireAfterSeconds"].ToDouble().Should().Be(expireAfterSeconds);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public void Execute_should_work_when_unique_is_true(
            [Values(false, true)]
            bool async)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Unique = true } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes(async);
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["unique"].ToBoolean().Should().BeTrue();
        }

        [Test]
        public void MessageEncoderSettings_get_should_return_expected_value()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        public void Requests_get_should_return_expected_value()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = subject.Requests;

            result.Should().ContainInOrder(requests);
        }

        [Test]
        public void WriteConcern_get_and_set_should_work()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);
            var value = WriteConcern.WMajority;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Test]
        public void WriteConcern_set_should_throw_when_value_is_null()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            Action action = () => { subject.WriteConcern = null; };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        private List<BsonDocument> ListIndexes(bool async)
        {
            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var cursor = ExecuteOperation(listIndexesOperation, async);
            return ReadCursorToEnd(cursor, async);
        }
    }
}
