/* Copyright 2013-2016 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateIndexesUsingInsertOperationTests : OperationTestBase
    {
        [Fact]
        public void constructor_should_initialize_subject()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.Requests.Should().Equal(requests);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexesUsingInsertOperation(null, Enumerable.Empty<CreateIndexRequest>(), _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_requests_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexesUsingInsertOperation(_collectionNamespace, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("requests");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexesUsingInsertOperation(_collectionNamespace, Enumerable.Empty<CreateIndexRequest>(), null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void CreateOperation_should_return_expected_result()
        {
            var request = new CreateIndexRequest(new BsonDocument("x", 1));
            var requests = new[] { request };
            var writeConcern = new WriteConcern(1);
            var subject = new CreateIndexesUsingInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = subject.CreateOperation(null, request);

            result.BypassDocumentValidation.Should().NotHaveValue();
            result.CollectionNamespace.CollectionName.Should().Be("system.indexes");
            result.ContinueOnError.Should().BeFalse();
            result.DocumentSource.Batch.Should().NotBeNull();
            result.MaxBatchCount.Should().NotHaveValue();
            result.MaxDocumentSize.Should().NotHaveValue();
            result.MaxMessageSize.Should().NotHaveValue();
            result.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            result.Serializer.Should().BeSameAs(BsonDocumentSerializer.Instance);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_background_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Background = true } };
            var subject = new CreateIndexesUsingInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["background"].ToBoolean().Should().BeTrue();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_creating_one_index(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            indexes.Select(index => index["name"].AsString).Should().BeEquivalentTo(new[] { "_id_", "x_1" });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_creating_two_indexes(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[]
            {
                new CreateIndexRequest(new BsonDocument("x", 1)),
                new CreateIndexRequest(new BsonDocument("y", 1))
            };
            var subject = new CreateIndexesUsingInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            indexes.Select(index => index["name"].AsString).Should().BeEquivalentTo(new[] { "_id_", "x_1", "y_1" });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_sparse_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Sparse = true } };
            var subject = new CreateIndexesUsingInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["sparse"].ToBoolean().Should().BeTrue();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_expireAfter_has_value(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var expireAfter = TimeSpan.FromSeconds(1.5);
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { ExpireAfter = expireAfter } };
            var subject = new CreateIndexesUsingInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["expireAfterSeconds"].ToDouble().Should().Be(expireAfter.TotalSeconds);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_unique_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Unique = true } };
            var subject = new CreateIndexesUsingInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["unique"].ToBoolean().Should().BeTrue();
        }

        private List<BsonDocument> ListIndexes()
        {
            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var cursor = ExecuteOperation(listIndexesOperation);
            return ReadCursorToEnd(cursor);
        }
    }
}
