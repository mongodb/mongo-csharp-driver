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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class AggregateOperationTests : OperationTestBase
    {
        private static BsonDocument[] __pipeline = new[] { BsonDocument.Parse("{ $match : { x : 'x' } }") };
        private static IBsonSerializer<BsonDocument> __resultSerializer = BsonDocumentSerializer.Instance;

        [Fact]
        public void Constructor_should_create_a_valid_instance()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.Pipeline.Should().Equal(__pipeline);
            subject.ResultSerializer.Should().BeSameAs(__resultSerializer);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.AllowDiskUse.Should().NotHaveValue();
            subject.BatchSize.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.MaxTime.Should().NotHaveValue();
            subject.ReadConcern.IsServerDefault.Should().BeTrue();
            subject.UseCursor.Should().NotHaveValue();
        }

        [Fact]
        public void Constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new AggregateOperation<BsonDocument>(null, __pipeline, __resultSerializer, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void Constructor_should_throw_when_pipeline_is_null()
        {
            var exception = Record.Exception(() => new AggregateOperation<BsonDocument>(_collectionNamespace, null, __resultSerializer, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void Constructor_should_throw_when_resultSerializer_is_null()
        {
            var exception = Record.Exception(() => new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("resultSerializer");
        }

        [Fact]
        public void Constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void AllowDiskUse_get_and_set_should_work()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);

            subject.AllowDiskUse = true;
            var result = subject.AllowDiskUse;

            result.Should().Be(true);
        }

        [Fact]
        public void BatchSize_get_and_set_should_work()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);

            subject.BatchSize = 23;
            var result = subject.BatchSize;

            result.Should().Be(23);
        }

        [Fact]
        public void Collation_get_and_set_should_work()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);
            var collation = new Collation("en_US");

            subject.Collation = collation;
            var result = subject.Collation;

            result.Should().BeSameAs(collation);
        }

        [Fact]
        public void MaxTime_get_and_set_should_work()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);
            var value = TimeSpan.FromSeconds(2);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Fact]
        public void ReadConcern_get_and_set_should_work()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);
            var value = new ReadConcern(ReadConcernLevel.Linearizable);

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void UseCursor_get_and_set_should_work()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);

            subject.UseCursor = true;
            var result = subject.UseCursor;

            result.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result(
            [Values(false, true)]
            bool useServerVersionSupportingAggregateCursorResult)
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);
            var serverVersion = Feature.AggregateCursorResult.SupportedOrNotSupportedVersion(useServerVersionSupportingAggregateCursorResult);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "cursor", () => new BsonDocument(), Feature.AggregateCursorResult.IsSupported(serverVersion) }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_AllowDiskUse_is_set(
            [Values(null, false, true)]
            bool? allowDiskUse)
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse
            };

            var result = subject.CreateCommand(Feature.AggregateCursorResult.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "allowDiskUse", () => allowDiskUse.Value, allowDiskUse != null },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_BatchSize_is_set(
            [Values(null, 1)]
            int? batchSize,
            [Values(false, true)]
            bool useServerVersionSupportingAggregateCursorResult)
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                BatchSize = batchSize
            };
            var serverVersion = Feature.AggregateCursorResult.SupportedOrNotSupportedVersion(useServerVersionSupportingAggregateCursorResult);

            var result = subject.CreateCommand(serverVersion);

            BsonDocument cursor = null;
            if (Feature.AggregateCursorResult.IsSupported(serverVersion))
            {
                cursor = new BsonDocument
                {
                    { "batchSize", () => batchSize.Value, batchSize != null }
                };
            }
            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "cursor", () => cursor, cursor != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Collation_is_set(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var collation = locale == null ? null : new Collation(locale);
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "collation", () => new BsonDocument("locale", locale), collation != null },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.Collation.LastNotSupportedVersion));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_MaxTime_is_set(
            [Values(null, 1)]
            int? milliSeconds)
        {
            var maxTime = milliSeconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliSeconds.Value);
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var result = subject.CreateCommand(Feature.AggregateCursorResult.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "maxTimeMS", () => maxTime.Value.TotalMilliseconds, maxTime != null },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Linearizable)]
            ReadConcernLevel? level)
        {
            var readConcern = new ReadConcern(level);
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var result = subject.CreateCommand(Feature.ReadConcern.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "readConcern", () => readConcern.ToBsonDocument(), level != null },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_ReadConcern_is_set_but_not_supported()
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Linearizable)
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.ReadConcern.LastNotSupportedVersion));

            exception.Should().BeOfType<MongoClientException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_UseCursor_is_set(
            [Values(null, false, true)]
            bool? useCursor,
            [Values(false, true)]
            bool useServerVersionSupportingAggregateCursorResult)
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                UseCursor = useCursor
            };
            var serverVersion = Feature.AggregateCursorResult.SupportedOrNotSupportedVersion(useServerVersionSupportingAggregateCursorResult);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "cursor", () => new BsonDocument(), useCursor.GetValueOrDefault(true) && Feature.AggregateCursorResult.IsSupported(serverVersion) }
            };
            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Aggregate);
            EnsureTestData();
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings);

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.ExecuteAsync(null, CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Execute(null, CancellationToken.None));
            }

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("binding");
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_pipeline_ends_with_out(
            [Values(false, true)]
            bool async)
        {
            var pipeline = new [] { BsonDocument.Parse("{ $out : \"xyz\" }") };
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, pipeline, __resultSerializer, _messageEncoderSettings);

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("pipeline");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_AllowDiskUse_is_set(
            [Values(null, false, true)]
            bool? allowDiskUse,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Aggregate, Feature.AggregateAllowDiskUse);
            EnsureTestData();
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_BatchSize_is_set(
            [Values(null, 1, 10)]
            int? batchSize,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Aggregate);
            EnsureTestData();
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                BatchSize = batchSize
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

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
            RequireServer.Check().Supports(Feature.Aggregate, Feature.Collation);
            EnsureTestData();
            var collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary);
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                Collation = collation
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(caseSensitive ? 1 : 2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set_but_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Aggregate).DoesNotSupport(Feature.Collation);
            var collation = new Collation("en_US");
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                Collation = collation
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
            RequireServer.Check().Supports(Feature.Aggregate, Feature.MaxTime);
            EnsureTestData();
            var maxTime = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value);
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Local)]
            ReadConcernLevel? level,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Aggregate, Feature.ReadConcern);
            EnsureTestData();
            var readConcern = new ReadConcern(level);
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_ReadConcern_is_set_but_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Aggregate).DoesNotSupport(Feature.ReadConcern);
            var readConcern = new ReadConcern(ReadConcernLevel.Linearizable);
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoClientException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_UseCursor_is_set(
            [Values(null, false, true)]
            bool? useCursor,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Aggregate);
            EnsureTestData();
            var subject = new AggregateOperation<BsonDocument>(_collectionNamespace, __pipeline, __resultSerializer, _messageEncoderSettings)
            {
                UseCursor = useCursor
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
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