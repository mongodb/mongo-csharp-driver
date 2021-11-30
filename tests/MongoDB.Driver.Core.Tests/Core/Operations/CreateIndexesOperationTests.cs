﻿/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateIndexesOperationTests : OperationTestBase
    {
        [Fact]
        public void constructor_should_initialize_subject()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Requests.Should().Equal(requests);

            subject.WriteConcern.Should().BeSameAs(WriteConcern.Acknowledged);
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexesOperation(null, Enumerable.Empty<CreateIndexRequest>(), _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_requests_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexesOperation(_collectionNamespace, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("requests");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexesOperation(_collectionNamespace, Enumerable.Empty<CreateIndexRequest>(), null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CommitQuorum_get_and_set_should_work(
            [Values(null, 1, 2)] int? w)
        {
            var subject = new CreateIndexesOperation(_collectionNamespace, Enumerable.Empty<CreateIndexRequest>(), _messageEncoderSettings);
            var value = w.HasValue ? CreateIndexCommitQuorum.Create(w.Value) : null;

            subject.CommitQuorum = value;
            var result = subject.CommitQuorum;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateOperation_should_return_expected_result(
            [Values(null, 1, 2)] int? w,
            [Values(null, -10000, 0, 1, 42, 9000, 10000, 10001)] int? maxTimeTicks)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var commitQuorum = w.HasValue ? CreateIndexCommitQuorum.Create(w.Value) : null;
            var maxTime = maxTimeTicks == null ? (TimeSpan?)null : TimeSpan.FromTicks(maxTimeTicks.Value);
            var writeConcern = new WriteConcern(1);
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                CommitQuorum = commitQuorum,
                MaxTime = maxTime,
                WriteConcern = writeConcern
            };

            var result = subject.CreateOperation();

            result.Should().BeOfType<CreateIndexesUsingCommandOperation>();
            var operation = (CreateIndexesUsingCommandOperation)result;
            operation.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            operation.CommitQuorum.Should().BeSameAs(commitQuorum);
            operation.MaxTime.Should().Be(maxTime);
            operation.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            operation.Requests.Should().Equal(requests);
            operation.WriteConcern.Should().BeSameAs(writeConcern);
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
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

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
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

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
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes();
            indexes.Select(index => index["name"].AsString).Should().BeEquivalentTo(new[] { "_id_", "x_1", "y_1" });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_partialFilterExpression_is_has_value(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { PartialFilterExpression = new BsonDocument("x", new BsonDocument("$gt", 0)) } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

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
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Sparse = true } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

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
            RequireServer.Check();
            DropCollection();
            var collation = new Collation(locale);
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Collation = collation } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

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
            RequireServer.Check();
            DropCollection();
            var expireAfter = TimeSpan.FromSeconds(1);
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { ExpireAfter = expireAfter } };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["expireAfterSeconds"].ToDouble().Should().Be(expireAfter.TotalSeconds);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_hidden_has_value(
            [Values(null, false, true)] bool? hidden,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.HiddenIndex);
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Hidden = hidden} };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            if (hidden.GetValueOrDefault())
            {
                index["hidden"].AsBoolean.Should().BeTrue();
            }
            else
            {
                index.Contains("hidden").Should().BeFalse();
            }
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
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["unique"].ToBoolean().Should().BeTrue();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "createIndexes", async);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(null, -10000, 0, 1, 42, 9000, 10000, 10001)] int? maxTimeTicks)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var writeConcern = new WriteConcern(1);
            var maxTime = maxTimeTicks == null ? (TimeSpan?)null : TimeSpan.FromTicks(maxTimeTicks.Value);
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            subject.MaxTime = maxTime;
            var result = subject.MaxTime;

            result.Should().Be(maxTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [Values(-10001, -9999, -42, -1)] long maxTimeTicks)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var writeConcern = new WriteConcern(1);
            var maxTime = TimeSpan.FromTicks(maxTimeTicks);
            var subject = new CreateIndexesOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };

            var exception = Record.Exception(() => subject.MaxTime = maxTime);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(1, 2)]
            int w)
        {
            var subject = new CreateIndexesOperation(_collectionNamespace, Enumerable.Empty<CreateIndexRequest>(), _messageEncoderSettings);
            var value = new WriteConcern(w);

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void WriteConcern_set_should_throw_when_value_is_null()
        {
            var subject = new CreateIndexesOperation(_collectionNamespace, Enumerable.Empty<CreateIndexRequest>(), _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.WriteConcern = null; });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("value");
        }

        // private methods
        private List<BsonDocument> ListIndexes()
        {
            var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            var cursor = ExecuteOperation(listIndexesOperation);
            return ReadCursorToEnd(cursor);
        }
    }
}
