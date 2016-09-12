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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class GeoNearOperationTests : OperationTestBase
    {
        private readonly BsonValue _near = new BsonArray { 1, 2 };
        private readonly IBsonSerializer<BsonDocument> _resultSerializer = BsonDocumentSerializer.Instance;

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.Near.Should().BeSameAs(_near);
            subject.ResultSerializer.Should().BeSameAs(_resultSerializer);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.DistanceMultiplier.Should().NotHaveValue();
            subject.Filter.Should().BeNull();
            subject.IncludeLocs.Should().NotHaveValue();
            subject.Limit.Should().NotHaveValue();
            subject.MaxDistance.Should().NotHaveValue();
            subject.MaxTime.Should().NotHaveValue();
            subject.ReadConcern.Should().BeSameAs(ReadConcern.Default);
            subject.Spherical.Should().NotHaveValue();
            subject.UniqueDocs.Should().NotHaveValue();
        }

        [Fact]
        public void Constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new GeoNearOperation<BsonDocument>(null, _near, _resultSerializer, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void Constructor_should_throw_when_near_is_null()
        {
            var exception = Record.Exception(() => new GeoNearOperation<BsonDocument>(_collectionNamespace, null, _resultSerializer, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("near");
        }

        [Fact]
        public void Constructor_should_throw_when_resultSerializer_is_null()
        {
            var exception = Record.Exception(() => new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("resultSerializer");
        }

        [Fact]
        public void Constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void DistanceMultiplier_get_and_set_should_work(
            [Values(null, 1.0, 2.0)]
            double? value)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            subject.DistanceMultiplier = value;
            var result = subject.DistanceMultiplier;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void IncludeLocs_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            subject.IncludeLocs = value;
            var result = subject.IncludeLocs;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Limit_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? value)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxDistance_get_and_set_should_work(
            [Values(null, 1.0, 2.0)]
            double? value)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            subject.MaxDistance = value;
            var result = subject.MaxDistance;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);
            var value = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_get_and_set_should_work(
            [Values(ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel level)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);
            var value = new ReadConcern(level);

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().Be(value);
        }

        [Fact]
        public void ReadConcern_set_should_throw_when_value_is_null()
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            var exception = Record.Exception(() => subject.ReadConcern = null);

            var argumentNulException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNulException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Spherical_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            subject.Spherical = value;
            var result = subject.Spherical;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void UniqueDocs_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            subject.UniqueDocs = value;
            var result = subject.UniqueDocs;

            result.Should().Be(value);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } }
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
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_DistanceMultiplier_is_set(
            [Values(null, 1.0, 2.0)]
            double? distanceMultiplier)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                DistanceMultiplier = distanceMultiplier
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "distanceMultiplier", () => distanceMultiplier.Value, distanceMultiplier.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Filter_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string filterString)
        {
            var filter = filterString == null ? null : BsonDocument.Parse(filterString);
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                Filter = filter
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "query", () => filter, filter != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_IncludeLocs_is_set(
            [Values(null, false, true)]
            bool? includeLocs)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                IncludeLocs = includeLocs
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "includeLocs", () => includeLocs.Value, includeLocs.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Limit_is_set(
            [Values(null, 1, 2)]
            int? limit)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                Limit = limit
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "limit", () => limit.Value, limit.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxDistance_is_set(
            [Values(null, 1.0, 2.0)]
            double? maxDistance)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                MaxDistance = maxDistance
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "maxDistance", () => maxDistance.Value, maxDistance.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxTime_is_set(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var maxTime = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "maxTimeMS", () => seconds.Value * 1000, seconds.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var readConcern = new ReadConcern(level);
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var result = subject.CreateCommand(Feature.ReadConcern.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "readConcern", () => readConcern.ToBsonDocument(), !readConcern.IsServerDefault }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Spherical_is_set(
           [Values(null, false, true)]
            bool? spherical)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                Spherical = spherical
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "spherical", () => spherical.Value, spherical.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_UniqueDocs_is_set(
           [Values(null, false, true)]
            bool? uniqueDocs)
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                UniqueDocs = uniqueDocs
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "geoNear", _collectionNamespace.CollectionName },
                { "near", new BsonArray { 1, 2 } },
                { "uniqueDocs", () => uniqueDocs.Value, uniqueDocs.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set_but_not_supported()
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.Collation.LastNotSupportedVersion));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void CreateCommand_should_throw_when_ReadConcern_is_set_but_not_supported()
        {
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Local)
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.ReadConcern.LastNotSupportedVersion));

            exception.Should().BeOfType<MongoClientException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["results"].AsBsonArray.Count.Should().Be(5);
            result["results"].AsBsonArray.Select(i => i["dis"].ToDouble()).Should().BeInAscendingOrder();
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
            var collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary);
            var filter = BsonDocument.Parse("{ x : 'x' }");
            var subject = new GeoNearOperation<BsonDocument>(_collectionNamespace, _near, _resultSerializer, _messageEncoderSettings)
            {
                Collation = collation,
                Filter = filter
            };

            var result = ExecuteOperation(subject, async);

            result["results"].AsBsonArray.Count.Should().Be(caseSensitive ? 2 : 5);
            result["results"].AsBsonArray.Select(i => i["dis"].ToDouble()).Should().BeInAscendingOrder();
        }

        // helper methods
        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(Enumerable.Range(1, 5).Select(id => new BsonDocument
                {
                    { "_id", id },
                    { "Location", new BsonArray { id, id + 1 } },
                    { "x", (id % 2) == 0 ? "x" : "X" } // some lower case and some upper case
                }));
                CreateIndexes(new CreateIndexRequest(new BsonDocument("Location", "2d")));
            });
        }
    }
}
