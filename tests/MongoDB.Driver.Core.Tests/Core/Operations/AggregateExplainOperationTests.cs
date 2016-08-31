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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class AggregateExplainOperationTests : OperationTestBase
    {
        [Fact]
        public void Constructor_should_create_a_valid_instance()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.Pipeline.Should().BeEmpty();
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Fact]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new AggregateExplainOperation(null, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_pipeline_is_null()
        {
            Action act = () => new AggregateExplainOperation(_collectionNamespace, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void AllowDiskUse_should_have_the_correct_value()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            subject.AllowDiskUse.Should().Be(null);

            subject.AllowDiskUse = true;

            subject.AllowDiskUse.Should().Be(true);
        }

        [Fact]
        public void MaxTime_should_have_the_correct_value()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            subject.MaxTime.Should().Be(null);

            subject.MaxTime = TimeSpan.FromSeconds(2);

            subject.MaxTime.Should().Be(TimeSpan.FromSeconds(2));
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_create_the_correct_command(
            [Values(null, false, true)] bool? allowDiskUse,
            [Values(null, 2000)] int? maxTime)
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse,
                MaxTime = maxTime.HasValue ? TimeSpan.FromMilliseconds(maxTime.Value) : (TimeSpan?)null
            };

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(subject.Pipeline) },
                { "allowDiskUse", () => allowDiskUse.Value, allowDiskUse.HasValue },
                { "maxTimeMS", () => maxTime.Value, maxTime.HasValue }
            };

            var result = subject.CreateCommand(new Misc.SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_the_result_without_any_options(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Where(minimumVersion: "2.4.0");
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().NotBeNull();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_the_result_with_allow_disk_use(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Where(minimumVersion: "2.6.0");
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings)
            {
                AllowDiskUse = true,
                MaxTime = TimeSpan.FromSeconds(20)
            };

            var result = ExecuteOperation(subject, async);

            result.Should().NotBeNull();
        }
    }
}
