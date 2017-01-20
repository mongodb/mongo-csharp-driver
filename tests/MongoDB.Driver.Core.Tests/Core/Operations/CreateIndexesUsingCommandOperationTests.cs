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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateIndexesUsingCommandOperationTests : OperationTestBase
    {
        [Fact]
        public void constructor_should_initialize_subject()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Requests.Should().Equal(requests);

            subject.WriteConcern.Should().BeSameAs(WriteConcern.Acknowledged);
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexesUsingCommandOperation(null, Enumerable.Empty<CreateIndexRequest>(), _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_requests_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexesUsingCommandOperation(_collectionNamespace, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("requests");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexesUsingCommandOperation(_collectionNamespace, Enumerable.Empty<CreateIndexRequest>(), null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_creating_one_index()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument(null) } }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_creating_two_indexes()
        {
            var requests = new[]
            {
                new CreateIndexRequest(new BsonDocument("x", 1)),
                new CreateIndexRequest(new BsonDocument("y", 1))
            };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument(null), requests[1].CreateIndexDocument(null) } }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(1, 2)]
            int w,
            [Values(false, true)]
            bool isWriteConcernSupported)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var writeConcern = new WriteConcern(w);
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var serverVersion = Feature.CommandsThatWriteAcceptWriteConcern.SupportedOrNotSupportedVersion(isWriteConcernSupported);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument(null) } },
                { "writeConcern", () => writeConcern.ToBsonDocument(), isWriteConcernSupported }
            };
            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_background_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CreateIndexesCommand);
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Background = true } };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

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
            RequireServer.Check().Supports(Feature.CreateIndexesCommand);
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

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
            RequireServer.Check().Supports(Feature.CreateIndexesCommand);
            DropCollection();
            var requests = new[]
            {
                new CreateIndexRequest(new BsonDocument("x", 1)),
                new CreateIndexRequest(new BsonDocument("y", 1))
            };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            indexes.Select(index => index["name"].AsString).Should().BeEquivalentTo(new[] { "_id_", "x_1", "y_1" });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_partialFilterExpression_has_value(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CreateIndexesCommand, Feature.PartialIndexes);
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { PartialFilterExpression = new BsonDocument("x", new BsonDocument("$gt", 0)) } };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["partialFilterExpression"].AsBsonDocument.Should().Be(requests[0].PartialFilterExpression);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_sparse_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CreateIndexesCommand);
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Sparse = true } };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["sparse"].ToBoolean().Should().BeTrue();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_Collation_has_value(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CreateIndexesCommand, Feature.Collation);
            DropCollection();
            var collation = new Collation(locale);
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Collation = collation } };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["collation"]["locale"].AsString.Should().Be(locale);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_expireAfter_has_value(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CreateIndexesCommand);
            DropCollection();
            var expireAfter = TimeSpan.FromSeconds(1.5);
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { ExpireAfter = expireAfter } };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

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
            RequireServer.Check().Supports(Feature.CreateIndexesCommand);
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Unique = true } };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["unique"].ToBoolean().Should().BeTrue();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
           [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CreateIndexesCommand, Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(1, 2)]
            int w)
        {
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, Enumerable.Empty<CreateIndexRequest>(), _messageEncoderSettings);
            var value = new WriteConcern(w);

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void WriteConcern_set_should_throw_when_value_is_null()
        {
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, Enumerable.Empty<CreateIndexRequest>(), _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.WriteConcern = null; });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("value");
        }

        private List<BsonDocument> ListIndexes()
        {
            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var cursor = ExecuteOperation(listIndexesOperation);
            return ReadCursorToEnd(cursor);
        }
    }
}
