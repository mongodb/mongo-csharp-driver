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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
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
        public void MaxTime_get_and_set_should_work()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = TimeSpan.FromSeconds(2);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
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

        [SkippableTheory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxTime_is_set(
            [Values(1, 2)]
            int milliseconds)
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                MaxTime = TimeSpan.FromMilliseconds(milliseconds)
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(__pipeline) },
                { "maxTimeMS", milliseconds }
            };
            result.Should().Be(expectedResult);
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
    }
}
