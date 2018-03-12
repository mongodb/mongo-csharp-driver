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
using System.Threading;
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
    public class AggregateExplainOperationTests : OperationTestBase
    {
        private static BsonDocument[] __pipeline = new[] { BsonDocument.Parse("{ $match : { x : 1 } }") };

        [Fact]
        public void Constructor_should_create_a_valid_instance()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.Pipeline.Should().Equal(__pipeline);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.AllowDiskUse.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.MaxTime.Should().NotHaveValue();
        }

        [Fact]
        public void Constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new AggregateExplainOperation(null, __pipeline, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void Constructor_should_throw_when_pipeline_is_null()
        {
            var exception = Record.Exception(() => new AggregateExplainOperation(_collectionNamespace, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void Constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new AggregateExplainOperation(_collectionNamespace, __pipeline, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void AllowDiskUse_get_and_set_should_work()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            subject.AllowDiskUse = true;
            var result = subject.AllowDiskUse;

            result.Should().Be(true);
        }

        [Fact]
        public void Collation_get_and_set_should_work()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = new Collation("en_US");

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void Comment_get_and_set_should_work()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = "test";

            subject.Comment = value;
            var result = subject.Comment;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void Hint_get_and_set_should_work()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = new BsonDocument("x", 1);

            subject.Hint = value;
            var result = subject.Hint;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(-10000, 0, 1, 10000, 99999)] long maxTimeTicks)
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
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
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(__pipeline) }
            };
            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_AllowDiskUse_is_set(
            [Values(false, true)]
            bool allowDiskUse)
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(__pipeline) },
                { "allowDiskUse", allowDiskUse }
            };
            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Collation_is_set(
            [Values("en_US", "fr_CA")]
            string locale)
        {
            var collation = new Collation(locale);
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(__pipeline) },
                { "collation", collation.ToBsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Comment_is_set(
            [Values(null, "test")]
            string comment)
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Comment = comment,
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(__pipeline) },
                { "comment", () => comment, comment != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Hint_is_set(
            [Values(null, "{x: 1}")]
            string hintJson)
        {
            var hint = hintJson == null ? null : BsonDocument.Parse(hintJson);
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Hint = hint
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(__pipeline) },
                { "hint", () => hint, hint != null }
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
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                MaxTime = TimeSpan.FromTicks(maxTimeTicks)
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(__pipeline) },
                { "maxTimeMS", expectedMaxTimeMS }
            };
            result.Should().Be(expectedResult);
            result["maxTimeMS"].BsonType.Should().Be(BsonType.Int32);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set_but_not_supported()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.Collation.LastNotSupportedVersion));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateExplain);
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().NotBeNull();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_AllowDiskUse_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateExplain);
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = true
            };

            var result = ExecuteOperation(subject, async);

            result.Should().NotBeNull();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Collation_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateExplain, Feature.Collation);
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().NotBeNull();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set_but_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateExplain).DoesNotSupport(Feature.Collation);

            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
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
            RequireServer.Check().Supports(Feature.AggregateExplain, Feature.FailPoints).ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings) { MaxTime = TimeSpan.FromSeconds(9001) };

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Comment_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check()
                .ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet)
                .Supports(Feature.AggregateExplain, Feature.AggregateComment);
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Comment = "test"
            };

            using (var profile = Profile(_collectionNamespace.DatabaseNamespace))
            {
                var result = ExecuteOperation(subject, async);

                result.Should().NotBeNull();

                var profileEntries = profile.Find(new BsonDocument("command.aggregate", new BsonDocument("$exists", true)));
                profileEntries.Should().HaveCount(1);
                profileEntries[0]["command"]["comment"].AsString.Should().Be(subject.Comment);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Hint_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateExplain, Feature.AggregateHint);
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Hint = "_id_"
            };

            var result = ExecuteOperation(subject, async);

            result.Should().NotBeNull();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_MaxTime_is_set(
           [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateExplain);
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                MaxTime = TimeSpan.FromSeconds(1)
            };

            var result = ExecuteOperation(subject, async);

            result.Should().NotBeNull();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateExplain);
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var cancellationToken = new CancellationTokenSource().Token;

            VerifySessionIdWasSentWhenSupported(subject, "aggregate", async);
        }
    }
}
