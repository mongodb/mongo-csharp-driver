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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class AggregateToCollectionOperationTests : OperationTestBase
    {
        private static readonly BsonDocument[] __pipeline = new[]
        {
                BsonDocument.Parse("{ $match : { _id : 1 } }"),
                BsonDocument.Parse("{ $out : \"awesome\" }")
        };

        [Fact]
        public void Constructor_should_create_a_valid_instance()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.Pipeline.Should().Equal(__pipeline);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.AllowDiskUse.Should().NotHaveValue();
            subject.BypassDocumentValidation.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.MaxTime.Should().NotHaveValue();
            subject.WriteConcern.Should().BeNull();
        }

        [Fact]
        public void Constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new AggregateToCollectionOperation(null, __pipeline, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void Constructor_should_throw_when_pipeline_is_null()
        {
            var exception = Record.Exception(() => new AggregateToCollectionOperation(_collectionNamespace, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void Constructor_should_throw_when_pipeline_does_not_end_with_out()
        {
            var exception = Record.Exception(() => new AggregateToCollectionOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void Constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new AggregateToCollectionOperation(_collectionNamespace, __pipeline, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Theory]
        [ParameterAttributeData]
        public void AllowDiskUse_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            subject.AllowDiskUse = value;
            var result = subject.AllowDiskUse;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void BypassDocumentValidation_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            subject.BypassDocumentValidation = value;
            var result = subject.BypassDocumentValidation;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US")]
            string locale)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(null, 1L)]
            long? milliseconds)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(1, 2)]
            int w)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = new WriteConcern(w);

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_AllowDiskUse_is_set(
            [Values(null, false, true)]
            bool? allowDiskUse)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "allowDiskUse", () => allowDiskUse.Value, allowDiskUse != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_BypassDocumentValidation_is_set(
            [Values(null, false, true)]
            bool? bypassDocumentValidation,
            [Values(false, true)]
            bool useServerVersionSupportingBypassDocumentValidation)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                BypassDocumentValidation = bypassDocumentValidation
            };
            var serverVersion = Feature.BypassDocumentValidation.SupportedOrNotSupportedVersion(useServerVersionSupportingBypassDocumentValidation);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "bypassDocumentValidation", () => bypassDocumentValidation.Value, bypassDocumentValidation != null && Feature.BypassDocumentValidation.IsSupported(serverVersion) }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Collation_is_set(
            [Values(null, "en_US")]
            string locale)
        {
            var collation = locale == null ? null : new Collation(locale);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set_but_not_supported()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.Collation.LastNotSupportedVersion));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxTime_is_set(
            [Values(null, 1, 2)]
            int? milliseconds)
        {
            var maxTime = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "maxTimeMS", () => milliseconds.Value, milliseconds != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w,
            [Values(false, true)]
            bool isWriteConcernSupported)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var serverVersion = Feature.CommandsThatWriteAcceptWriteConcern.SupportedOrNotSupportedVersion(isWriteConcernSupported);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null && isWriteConcernSupported }
            };
            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateOut);
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_AllowDiskUse_is_set(
            [Values(null, false, true)]
            bool? allowDiskUse,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateOut);
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_BypassDocumentValidation_is_set(
            [Values(null, false, true)]
            bool? bypassDocumentValidation,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateOut);
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                BypassDocumentValidation = bypassDocumentValidation
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Collation_is_set(
            [Values(false, true)]
            bool caseSensitive,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateOut, Feature.Collation);
            EnsureTestData();
            var pipeline = new[]
            {
                BsonDocument.Parse("{ $match : { x : \"x\" } }"),
                BsonDocument.Parse("{ $out : \"awesome\" }")
            };
            var collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, pipeline, _messageEncoderSettings)
            {
                Collation = collation
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(caseSensitive ? 1 : 2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateOut).DoesNotSupport(Feature.Collation);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_MaxTime_is_set(
            [Values(null, 1000)]
            int? milliseconds,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateOut);
            EnsureTestData();
            var maxTime = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateOut, Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
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
}