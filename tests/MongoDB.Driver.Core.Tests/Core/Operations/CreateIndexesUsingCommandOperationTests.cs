/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Driver.Core.TestHelpers;
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

        [SkippableTheory]
        [ParameterAttributeData]
        public void CommitQuorum_get_and_set_should_work(
            [Values(null, 1, 2)] int? w)
        {
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, Enumerable.Empty<CreateIndexRequest>(), _messageEncoderSettings);
            var value = w.HasValue ? CreateIndexCommitQuorum.Create(w.Value) : null;

            subject.CommitQuorum = value;
            var result = subject.CommitQuorum;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_creating_one_index()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

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
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument(null), requests[1].CreateIndexDocument(null) } }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_CommitQuorum_with_mode_is_Set(
            [Values("abc", "def")] string mode)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var commitQuorum = CreateIndexCommitQuorum.Create(mode);
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                CommitQuorum = commitQuorum
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(serverVersion: Feature.CreateIndexCommitQuorum.FirstSupportedVersion);

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument(null) } },
                { "commitQuorum", mode }
            };
            result.Should().Be(expectedResult);
            result["commitQuorum"].BsonType.Should().Be(BsonType.String);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_CommitQuorum_with_w_is_Set(
            [Values(1, 2, 3)] int w)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var commitQuorum = CreateIndexCommitQuorum.Create(w);
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                CommitQuorum = commitQuorum
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(serverVersion: Feature.CreateIndexCommitQuorum.FirstSupportedVersion);

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument(null) } },
                { "commitQuorum", w }
            };
            result.Should().Be(expectedResult);
            result["commitQuorum"].BsonType.Should().Be(BsonType.Int32);
        }

        [Theory]
        [InlineData(-10000, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(9999, 1)]
        [InlineData(10000, 1)]
        [InlineData(10001, 2)]
        public void CreateCommand_should_return_expected_result_when_MaxTime_is_Set(long maxTimeTicks, int expectedMaxTimeMS)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);
            subject.MaxTime = TimeSpan.FromTicks(maxTimeTicks);
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument(null) } },
                { "maxTimeMS", expectedMaxTimeMS },
            };
            result.Should().Be(expectedResult);
            result["maxTimeMS"].BsonType.Should().Be(BsonType.Int32);
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
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(serverVersion: Feature.CommandsThatWriteAcceptWriteConcern.SupportedOrNotSupportedVersion(isWriteConcernSupported));

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray { requests[0].CreateIndexDocument(null) } },
                { "writeConcern", () => writeConcern.ToBsonDocument(), isWriteConcernSupported }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_commitQuorum_is_specified_and_not_supported()
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var commitQuorum = CreateIndexCommitQuorum.Create(1);
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                CommitQuorum = commitQuorum
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(serverVersion: Feature.CreateIndexCommitQuorum.LastNotSupportedVersion);

            var exception = Record.Exception(() => subject.CreateCommand(session, connectionDescription));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_maxTime_is_exceeded(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings) { MaxTime = TimeSpan.FromSeconds(9001) };

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
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
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            var index = indexes.Single(i => i["name"].AsString == "x_1");
            index["background"].ToBoolean().Should().BeTrue();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_work_when_commitQuorum_is_specified(
            [Values(1, "majority", "votingMembers")] object commitQuorumCase,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded).Supports(Feature.CreateIndexCommitQuorum);
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            CreateIndexCommitQuorum commitQuorum;
            if (commitQuorumCase is int w)
            {
                commitQuorum = CreateIndexCommitQuorum.Create(w);
            }
            else if (commitQuorumCase is string mode)
            {
                switch (mode)
                {
                    case "majority": commitQuorum = CreateIndexCommitQuorum.Majority; break;
                    case "votingMembers": commitQuorum = CreateIndexCommitQuorum.VotingMembers; break;
                    default: commitQuorum = CreateIndexCommitQuorum.Create(mode); break;
                }
            }
            else
            {
                throw new ArgumentException($"Invalid commitQuorumCase: {commitQuorumCase}.", nameof(commitQuorumCase));
            }
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings) { CommitQuorum = commitQuorum };

            ExecuteOperation(subject, async);

            var indexes = ListIndexes();
            indexes.Select(index => index["name"].AsString).Should().BeEquivalentTo(new[] { "_id_", "x_1" });
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
            RequireServer.Check();
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
            RequireServer.Check().Supports(Feature.PartialIndexes);
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
            RequireServer.Check();
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
            RequireServer.Check().Supports(Feature.Collation);
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
            RequireServer.Check();
            DropCollection();
            var expireAfter = TimeSpan.FromSeconds(1);
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { ExpireAfter = expireAfter } };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

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
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) { Hidden = hidden } };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            ExecuteOperation(subject, async);

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
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "createIndexes", async);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(null, -10000, 0, 1, 42, 9000, 10000, 10001)] int? maxTimeTicks)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);
            var value = maxTimeTicks == null ? (TimeSpan?)null : TimeSpan.FromTicks(maxTimeTicks.Value);
            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [Values(-10001, -9999, -42, -1)] long maxTimeTicks)
        {
            var requests = new[] { new CreateIndexRequest(new BsonDocument("x", 1)) };
            var subject = new CreateIndexesUsingCommandOperation(_collectionNamespace, requests, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
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
