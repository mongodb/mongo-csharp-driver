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
    public class CountOperationTests : OperationTestBase
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.Filter.Should().BeNull();
            subject.Hint.Should().BeNull();
            subject.Limit.Should().NotHaveValue();
            subject.MaxTime.Should().NotHaveValue();
            subject.ReadConcern.IsServerDefault.Should().BeTrue();
            subject.RetryRequested.Should().BeFalse();
            subject.Skip.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new CountOperation(null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new CountOperation(_collectionNamespace, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_get_and_set_should_work(
            [Values(null, "{ x : 1 }")]
            string valueString)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Hint_get_and_set_should_work(
            [Values(null, "{ hint : \"x_1\" }", "{ hint : { x : 1 } }")]
            string valueString)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString)["hint"];

            subject.Hint = value;
            var result = subject.Hint;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Limit_get_and_set_should_work(
            [Values(null, 1L, 2L)]
            long? value)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(-10000, 0, 1, 10000, 99999)] long maxTimeTicks)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
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
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_get_and_set_should_work(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);
            var value = level == null ? ReadConcern.Default : new ReadConcern(level.Value);

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void RetryRequestedget_and_set_should_work(
            [Values(false, true)]
            bool value)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);

            subject.RetryRequested = value;
            var result = subject.RetryRequested;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Skip_get_and_set_should_work(
            [Values(null, 1L, 2L)]
            long? value)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);

            subject.Skip = value;
            var result = subject.Skip;

            result.Should().Be(value);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);

            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(connectionDescription, session);

            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Collation_is_set(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var collation = locale == null ? null : new Collation(locale);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = collation
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription(Feature.Collation.FirstSupportedVersion);
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(connectionDescription, session);

            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set_but_not_supported()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription(Feature.Collation.LastNotSupportedVersion);
            var session = OperationTestHelper.CreateSession();

            var exception = Record.Exception(() => subject.CreateCommand(connectionDescription, session));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Filter_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string filterString)
        {
            var filter = filterString == null ? null : BsonDocument.Parse(filterString);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Filter = filter
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(connectionDescription, session);

            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "query", filter, filter != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Hint_is_set(
            [Values(null, "{ hint : \"x_1\" }", "{ hint : { x : 1 } }")]
            string hintString)
        {
            var hint = hintString == null ? null : BsonDocument.Parse(hintString)["hint"];
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Hint = hint
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(connectionDescription, session);

            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "hint", hint, hint != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Limit_is_set(
            [Values(null, 1L, 2L)]
            long? limit)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Limit = limit
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(connectionDescription, session);

            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "limit", () => limit.Value, limit != null }
            };
            result.Should().Be(expectedResult);
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
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxTime = TimeSpan.FromTicks(maxTimeTicks)
            };
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(connectionDescription, session);

            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "maxTimeMS", expectedMaxTimeMS }
            };
            result.Should().Be(expectedResult);
            result["maxTimeMS"].BsonType.Should().Be(BsonType.Int32);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var readConcern = level == null ? ReadConcern.Default : new ReadConcern(level);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription(Feature.ReadConcern.FirstSupportedVersion);
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(connectionDescription, session);

            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "readConcern", () => readConcern.ToBsonDocument(), !readConcern.IsServerDefault }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_ReadConcern_is_set_but_not_supported()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Linearizable)
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription(Feature.ReadConcern.LastNotSupportedVersion);
            var session = OperationTestHelper.CreateSession();

            var exception = Record.Exception(() => subject.CreateCommand(connectionDescription, session));

            exception.Should().BeOfType<MongoClientException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_using_causal_consistency(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var readConcern = level == null ? ReadConcern.Default : new ReadConcern(level);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription(Feature.ReadConcern.FirstSupportedVersion, supportsSessions: true);
            var session = OperationTestHelper.CreateSession(true, new BsonTimestamp(100));

            var result = subject.CreateCommand(connectionDescription, session);

            var expectedReadConcernDocument = readConcern.ToBsonDocument();
            expectedReadConcernDocument["afterClusterTime"] = new BsonTimestamp(100);

            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "readConcern", expectedReadConcernDocument }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Skip_is_set(
            [Values(null, 1L, 2L)]
            long? skip)
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Skip = skip
            };

            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(connectionDescription, session);

            var expectedResult = new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "skip", () => skip.Value, skip != null }
            };
            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Collation_is_set(
            [Values(false, true)]
            bool caseSensitive,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Collation);
            EnsureTestData();
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary),
                Filter = BsonDocument.Parse("{ x : \"x\" }")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(caseSensitive ? 1 : 2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set_but_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.Collation);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_maxTime_is_exceeded(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings) { MaxTime = TimeSpan.FromSeconds(9001) };

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Filter_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Filter = BsonDocument.Parse("{ _id : 1 }")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Hint_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Hint = BsonDocument.Parse("{ _id : 1 }")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Limit_is_set(
            [Values(null, 1L, 2L)]
            long? limit,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Limit = limit
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(limit ?? 2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_MaxTime_is_set(
            [Values(null, 1000L)]
            long? milliseconds,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxTime = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value)
            };

            // TODO: use failpoints to force a timeout?
            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Local)]
            ReadConcernLevel? level,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.ReadConcern);
            EnsureTestData();
            var readConcern = level == null ? ReadConcern.Default : new ReadConcern(level.Value);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_ReadConcern_is_set_but_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.ReadConcern);
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Local)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoClientException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Skip_is_set(
            [Values(null, 1L, 2L)]
            long? skip,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Skip = skip
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(2 - (skip ?? 0));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "count", async);
        }

        // helper methods
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
}
