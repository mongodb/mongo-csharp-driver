/* Copyright 2013-2015 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class AggregateOperationTests : OperationTestBase
    {
        [Test]
        public void Constructor_should_create_a_valid_instance()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, Enumerable.Empty<BsonDocument>(), BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.Pipeline.Should().BeEmpty();
            subject.ResultSerializer.Should().BeSameAs(BsonDocumentSerializer.Instance);
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new AggregateOperation<BsonDocument>(null, Enumerable.Empty<BsonDocument>(), BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_pipeline_is_null()
        {
            Action act = () => new AggregateOperation<BsonDocument>(_collectionNamespace, null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_result_serializer_is_null()
        {
            Action act = () => new AggregateOperation<BsonDocument>(_collectionNamespace, Enumerable.Empty<BsonDocument>(), null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new AggregateOperation<BsonDocument>(_collectionNamespace, Enumerable.Empty<BsonDocument>(), BsonDocumentSerializer.Instance, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void AllowDiskUse_should_have_the_correct_value()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, Enumerable.Empty<BsonDocument>(), BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.AllowDiskUse.Should().Be(null);

            subject.AllowDiskUse = true;

            subject.AllowDiskUse.Should().Be(true);
        }

        [Test]
        public void BatchSize_should_have_the_correct_value()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, Enumerable.Empty<BsonDocument>(), BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.BatchSize.Should().Be(null);

            subject.BatchSize = 23;

            subject.BatchSize.Should().Be(23);
        }

        [Test]
        public void MaxTime_should_have_the_correct_value()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, Enumerable.Empty<BsonDocument>(), BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.MaxTime.Should().Be(null);

            subject.MaxTime = TimeSpan.FromSeconds(2);

            subject.MaxTime.Should().Be(TimeSpan.FromSeconds(2));
        }

        [Test]
        public void UseCursor_should_have_the_correct_value()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, Enumerable.Empty<BsonDocument>(), BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.UseCursor.Should().Be(null);

            subject.UseCursor = true;

            subject.UseCursor.Should().Be(true);
        }

        [Test]
        [Category("ReadConcern")]
        public void CreateCommand_should_create_the_correct_command(
            [Values("2.4.0", "2.6.0", "2.8.0", "3.0.0", "3.2.0")] string serverVersion,
            [Values(null, false, true)] bool? allowDiskUse,
            [Values(null, 10, 20)] int? batchSize,
            [Values(null, 2000)] int? maxTime,
            [Values(null, ReadConcernLevel.Local, ReadConcernLevel.Majority)] ReadConcernLevel? readConcernLevel,
            [Values(null, false, true)] bool? useCursor)
        {
            var semanticServerVersion = SemanticVersion.Parse(serverVersion);
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, Enumerable.Empty<BsonDocument>(), BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse,
                BatchSize = batchSize,
                MaxTime = maxTime.HasValue ? TimeSpan.FromMilliseconds(maxTime.Value) : (TimeSpan?)null,
                ReadConcern = new ReadConcern(readConcernLevel),
                UseCursor = useCursor
            };

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(subject.Pipeline) },
                { "allowDiskUse", () => allowDiskUse.Value, allowDiskUse.HasValue },
                { "maxTimeMS", () => maxTime.Value, maxTime.HasValue }
            };

            if (!subject.ReadConcern.IsServerDefault)
            {
                expectedResult["readConcern"] = subject.ReadConcern.ToBsonDocument();
            }

            if (SupportedFeatures.IsAggregateCursorResultSupported(semanticServerVersion) && useCursor.GetValueOrDefault(true))
            {
                expectedResult["cursor"] = new BsonDocument
                {
                    { "batchSize", () => batchSize.Value, batchSize.HasValue }
                };
            }

            if (!subject.ReadConcern.IsSupported(semanticServerVersion))
            {
                Action act = () => subject.CreateCommand(semanticServerVersion);
                act.ShouldThrow<MongoClientException>();
            }
            else
            {
                var result = subject.CreateCommand(semanticServerVersion);
                result.Should().Be(expectedResult);
            }
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "2.4.0")]
        public void Executing_with_matching_documents_using_no_options(
            [Values(false, true)]
            bool async)
        {
            var pipeline = BsonDocument.Parse("{$match: {_id: { $gt: 3}}}");
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, new[] { pipeline }, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "2.6.0")]
        public void Executing_with_matching_documents_using_all_options(
            [Values(false, true)]
            bool async)
        {
            var pipeline = BsonDocument.Parse("{$match: {_id: { $gt: 3}}}");
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, new[] { pipeline }, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                AllowDiskUse = true,
                BatchSize = 2,
                MaxTime = TimeSpan.FromSeconds(20)
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "2.6.0")]
        public void Executing_with_matching_documents_using_a_cursor(
            [Values(false, true)]
            bool async)
        {
            var pipeline = BsonDocument.Parse("{$match: {_id: { $gt: 3}}}");
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, new[] { pipeline }, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                UseCursor = true
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "2.4.0")]
        public void Executing_with_matching_documents_without_a_cursor(
            [Values(false, true)]
            bool async)
        {
            var pipeline = BsonDocument.Parse("{$match: {_id: { $gt: 3}}}");
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, new[] { pipeline }, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                UseCursor = false
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "2.4.0")]
        public void Executing_with_no_matching_documents_without_a_cursor(
            [Values(false, true)]
            bool async)
        {
            var pipeline = BsonDocument.Parse("{$match: {_id: { $gt: 5}}}");
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, new[] { pipeline }, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                UseCursor = false
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "2.6.0")]
        public void Executing_with_no_matching_documents_using_a_cursor(
            [Values(false, true)]
            bool async)
        {
            var pipeline = BsonDocument.Parse("{$match: {_id: { $gt: 5}}}");
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, new[] { pipeline }, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                UseCursor = true
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
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