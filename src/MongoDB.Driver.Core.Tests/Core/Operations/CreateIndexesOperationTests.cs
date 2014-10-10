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
        public void CreateCommand_should_return_expected_result_when_creating_one_index()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument() } }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_creating_two_indexes()
        {
            var requests = new[]
            {
                new CreateIndexRequest(new BsonDocument("x", 1)),
                new CreateIndexRequest(new BsonDocument("y", 1))
            };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument(), requests[1].CreateIndexDocument() } }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_work_when_background_is_true()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Background = true } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result["ok"].ToBoolean().Should().BeTrue();

            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var indexes = (await ExecuteOperationAsync(listIndexesOperation)).ToList();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["background"].ToBoolean().Should().BeTrue();
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_work_when_creating_one_index()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result["ok"].ToBoolean().Should().BeTrue();

            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var indexes = await ExecuteOperationAsync(listIndexesOperation);
            indexes.Select(index => index["name"].AsString).Should().BeEquivalentTo(new[] { "_id_", "x_1" });
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_work_when_creating_two_indexes()
        {
            var requests = new[]
            {
                new CreateIndexRequest(new BsonDocument("x", 1)),
                new CreateIndexRequest(new BsonDocument("y", 1))
            };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result["ok"].ToBoolean().Should().BeTrue();

            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var indexes = await ExecuteOperationAsync(listIndexesOperation);
            indexes.Select(index => index["name"].AsString).Should().BeEquivalentTo(new[] { "_id_", "x_1", "y_1" });
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_work_when_sparse_is_true()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Sparse = true } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result["ok"].ToBoolean().Should().BeTrue();

            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var indexes = (await ExecuteOperationAsync(listIndexesOperation)).ToList();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["sparse"].ToBoolean().Should().BeTrue();
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_work_when_timeToLive_has_value()
        {
            var timeToLiveSeconds = 1.5;
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { TimeToLive = TimeSpan.FromSeconds(timeToLiveSeconds) } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result["ok"].ToBoolean().Should().BeTrue();

            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var indexes = (await ExecuteOperationAsync(listIndexesOperation)).ToList();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["expireAfterSeconds"].ToDouble().Should().Be(timeToLiveSeconds);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_work_when_unique_is_true()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Unique = true } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result["ok"].ToBoolean().Should().BeTrue();

            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var indexes = (await ExecuteOperationAsync(listIndexesOperation)).ToList();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["unique"].ToBoolean().Should().BeTrue();
        }

        [Test]
        public void GetIndexName_with_BsonDocument_should_return_expected_result()
        {
            var keys = new BsonDocument
            {
                { "a", new BsonDouble(1.0) },
                { "b", new BsonInt32(1) },
                { "c", new BsonInt64(1) },
                { "d", new BsonDouble(-1.0) },
                { "e", new BsonInt32(-1) },
                { "f", new BsonInt64(-1) },
                { "g g", "s s" },
                { "h", false }
            };
            var expectedResult = "a_1_b_1_c_1_d_-1_e_-1_f_-1_g_g_s_s_h_x";

            var result = CreateIndexesOperation.GetIndexName(keys);

            result.Should().Be(expectedResult);
        }

        [Test]
        public void GetIndexName_with_names_should_return_expected_result()
        {
            var keys = new[] { "a", "b", "c c" };
            var expectedResult = "a_1_b_1_c_c_1";

            var result = CreateIndexesOperation.GetIndexName(keys);

            result.Should().Be(expectedResult);
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
    }
}
