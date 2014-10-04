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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class AggregateExplainOperationTests
    {
        private CollectionNamespace _collectionNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _collectionNamespace = SuiteConfiguration.GetCollectionNamespaceForTestFixture();
            _messageEncoderSettings = SuiteConfiguration.MessageEncoderSettings;
        }

        [Test]
        public void Constructor_should_create_a_valid_instance()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            subject.CollectionNamespace.FullName.Should().Be(_collectionNamespace.FullName);
            subject.Pipeline.Should().BeEmpty();
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new AggregateExplainOperation(null, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_pipeline_is_null()
        {
            Action act = () => new AggregateExplainOperation(_collectionNamespace, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void AllowDiskUse_should_have_the_correct_value()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            subject.AllowDiskUse.Should().Be(null);

            subject.AllowDiskUse = true;

            subject.AllowDiskUse.Should().Be(true);
        }

        [Test]
        public void MaxTime_should_have_the_correct_value()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            subject.MaxTime.Should().Be(null);

            subject.MaxTime = TimeSpan.FromSeconds(2);

            subject.MaxTime.Should().Be(TimeSpan.FromSeconds(2));
        }

        [Test]
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

            var result = subject.CreateCommand();
            
            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer(MinimumVersion="2.4.0")]
        public async Task ExecuteAsync_should_return_the_result()
        {
            var subject = new AggregateExplainOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings)
            {
                AllowDiskUse = true,
                MaxTime = TimeSpan.FromSeconds(20)
            };

            using(var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding, Timeout.InfiniteTimeSpan, CancellationToken.None);

                result.Should().NotBeNull();
            }
        }
    }
}
