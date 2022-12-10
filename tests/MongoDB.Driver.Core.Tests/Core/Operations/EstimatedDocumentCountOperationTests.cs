/* Copyright 2021-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class EstimatedDocumentCountOperationTests : OperationTestBase
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Comment.Should().BeNull();
            subject.MaxTime.Should().NotHaveValue();
            subject.ReadConcern.IsServerDefault.Should().BeTrue();
            subject.RetryRequested.Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new EstimatedDocumentCountOperation(null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new EstimatedDocumentCountOperation(_collectionNamespace, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void Comment_get_and_set_should_work()
        {
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings);
            var value = new BsonString("comment");

            subject.Comment = value;
            var result = subject.Comment;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCountOperation_should_return_expected_result_when_Comment_is_set(
            [Values(null, "test")] string comment)
        {
            var value = (BsonValue)comment;
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Comment = value
            };

            var result = subject.CreateCountOperation();

            result.Should().BeOfType<CountOperation>()
                .Subject.Comment.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(-10000, 0, 1, 10000, 99999)] long maxTimeTicks)
        {
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [Values(-10001, -9999, -1)] long maxTimeTicks)
        {
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_get_and_set_should_work(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)] ReadConcernLevel? level)
        {
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings);
            var value = level == null ? ReadConcern.Default : new ReadConcern(level.Value);

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void RetryRequested_get_and_set_should_work([Values(false, true)] bool value)
        {
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings);

            subject.RetryRequested = value;
            var result = subject.RetryRequested;

            result.Should().Be(value);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings);
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = CreateCommand(subject, connectionDescription, session);

            AssertCommandDocument(result);
        }

        [Theory]
        [InlineData(-10000, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(9999, 1)]
        [InlineData(10000, 1)]
        [InlineData(10001, 2)]
        public void CreateCommand_should_return_expected_result_when_MaxTime_is_set(long maxTimeTicks, int expectedMaxTimeMS)
        {
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxTime = TimeSpan.FromTicks(maxTimeTicks)
            };
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = CreateCommand(subject, connectionDescription, session);

            AssertCommandDocument(result, expectedMaxTimeMS: expectedMaxTimeMS);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)] ReadConcernLevel? level)
        {
            var readConcern = level == null ? ReadConcern.Default : new ReadConcern(level);
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = CreateCommand(subject, connectionDescription, session);

            AssertCommandDocument(result, readConcern: readConcern.IsServerDefault ? null : readConcern.ToBsonDocument());
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_using_causal_consistency(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)] ReadConcernLevel? level)
        {
            var readConcern = level == null ? ReadConcern.Default : new ReadConcern(level);
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription(supportsSessions: true);
            var session = OperationTestHelper.CreateSession(true, new BsonTimestamp(100));

            var result = CreateCommand(subject, connectionDescription, session);

            var expectedReadConcernDocument = readConcern.ToBsonDocument();
            expectedReadConcernDocument["afterClusterTime"] = new BsonTimestamp(100);

            AssertCommandDocument(result, readConcern: expectedReadConcernDocument);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result([Values(false, true)] bool async)
        {
            RequireServer.Check();

            EnsureTestData();
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_maxTime_is_exceeded([Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);

            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings) { MaxTime = TimeSpan.FromSeconds(9001) };

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_MaxTime_is_set(
            [Values(null, 1000L)] long? milliseconds,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();

            EnsureTestData();
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxTime = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value)
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Local)] ReadConcernLevel? level,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();

            EnsureTestData();
            var readConcern = level == null ? ReadConcern.Default : new ReadConcern(level.Value);
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported([Values(false, true)] bool async)
        {
            RequireServer.Check();

            EnsureTestData();
            var subject = new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "count", async);
        }

        // private methods
        private void AssertCommandDocument(BsonDocument actualResult, int? expectedMaxTimeMS = null, BsonDocument readConcern = null)
        {
            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "maxTimeMS", () => expectedMaxTimeMS.Value, expectedMaxTimeMS.HasValue },
                { "readConcern", () => readConcern, readConcern != null }
            };
            actualResult.Should().Be(expectedResult);
            if (actualResult.TryGetValue("maxTimeMS", out var maxTimeMS))
            {
                maxTimeMS.BsonType.Should().Be(BsonType.Int32);
            }
        }

        private BsonDocument CreateCommand(EstimatedDocumentCountOperation subject, ConnectionDescription connectionDescription, ICoreSession session)
        {
            var countOperation = (CountOperation)subject.CreateCountOperation();
            return countOperation.CreateCommand(connectionDescription, session);
        }

        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(new BsonDocument { { "_id", 1 }, { "x", "x" } });
                Insert(new BsonDocument { { "_id", 2 }, { "x", "X" } });
            });
        }
    }

    internal static class EstimatedDocumentCountOperationReflector
    {
        public static IExecutableInRetryableReadContext<long> CreateCountOperation(this EstimatedDocumentCountOperation operation)
        {
            return (IExecutableInRetryableReadContext<long>)Reflector.Invoke(operation, nameof(CreateCountOperation));
        }
    }
}
