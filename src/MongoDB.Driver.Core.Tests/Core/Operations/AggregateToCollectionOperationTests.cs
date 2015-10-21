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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class AggregateToCollectionOperationTests : OperationTestBase
    {
        private BsonDocument[] _pipeline;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();

            _pipeline = new[] 
            { 
                BsonDocument.Parse("{$match: {_id: { $gt: 3}}}"),
                BsonDocument.Parse("{$out: \"awesome\"}")
            };
        }

        [Test]
        public void Constructor_should_create_a_valid_instance()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, _pipeline, _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.Pipeline.Should().HaveCount(2);
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new AggregateToCollectionOperation(null, _pipeline, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_pipeline_is_null()
        {
            Action act = () => new AggregateToCollectionOperation(_collectionNamespace, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_pipeline_does_not_end_with_out()
        {
            Action act = () => new AggregateToCollectionOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new AggregateToCollectionOperation(_collectionNamespace, _pipeline, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void BypassDocumentValidation_should_have_the_correct_value()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, _pipeline, _messageEncoderSettings);

            subject.BypassDocumentValidation.Should().Be(null);

            subject.BypassDocumentValidation = true;

            subject.BypassDocumentValidation.Should().Be(true);
        }

        [Test]
        public void AllowDiskUse_should_have_the_correct_value()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, _pipeline, _messageEncoderSettings);

            subject.AllowDiskUse.Should().Be(null);

            subject.AllowDiskUse = true;

            subject.AllowDiskUse.Should().Be(true);
        }

        [Test]
        public void MaxTime_should_have_the_correct_value()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, _pipeline, _messageEncoderSettings);

            subject.MaxTime.Should().Be(null);

            subject.MaxTime = TimeSpan.FromSeconds(2);

            subject.MaxTime.Should().Be(TimeSpan.FromSeconds(2));
        }

        [Test]
        public void CreateCommand_should_create_the_correct_command(
            [Values(null, false, true)] bool? allowDiskUse,
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(null, 2000)] int? maxTime,
            [Values("3.0.0", "3.1.3")] string serverVersionString)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, _pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse,
                BypassDocumentValidation = bypassDocumentValidation,
                MaxTime = maxTime.HasValue ? TimeSpan.FromMilliseconds(maxTime.Value) : (TimeSpan?)null,
            };
            var serverVersion = SemanticVersion.Parse(serverVersionString);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(subject.Pipeline) },
                { "allowDiskUse", () => allowDiskUse.Value, allowDiskUse.HasValue },
                { "bypassDocumentValidation", () => bypassDocumentValidation.Value, bypassDocumentValidation.HasValue && SupportedFeatures.IsBypassDocumentValidationSupported(serverVersion) },
                { "maxTimeMS", () => maxTime.Value, maxTime.HasValue }
            };

            var result = subject.CreateCommand(serverVersion);

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "2.6.0")]
        public void Executing_with_matching_documents_using_all_options(
            [Values(false, true)]
            bool async)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, _pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = true,
                BypassDocumentValidation = true,
                MaxTime = TimeSpan.FromSeconds(20)
            };

            ExecuteOperation(subject, async);

            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(Enumerable.Range(1, 5).Select(id => new BsonDocument("_id", id)));
            });
        }
    }
}