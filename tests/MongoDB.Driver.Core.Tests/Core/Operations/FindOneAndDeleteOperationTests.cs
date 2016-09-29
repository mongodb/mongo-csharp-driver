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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class FindOneAndDeleteOperationTests : OperationTestBase
    {
        private BsonDocument _filter;
        private IBsonSerializer<BsonDocument> _findAndModifyValueDeserializer;

        public FindOneAndDeleteOperationTests()
        {
            _filter = new BsonDocument("x", 1);
            _findAndModifyValueDeserializer = new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance);
        }

        [Fact]
        public void Constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new FindOneAndDeleteOperation<BsonDocument>(null, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void Constructor_should_throw_when_filter_is_null()
        {
            var exception = Record.Exception(() => new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, null, BsonDocumentSerializer.Instance, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("filter");
        }

        [Fact]
        public void Constructor_should_throw_when_resultSerializer_is_null()
        {
            var exception = Record.Exception(() => new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("resultSerializer");
        }

        [Fact]
        public void Constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void Constructor_should_initialize_object()
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.Filter.Should().BeSameAs(_filter);
            subject.ResultSerializer.Should().BeSameAs(BsonDocumentSerializer.Instance);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.MaxTime.Should().NotHaveValue();
            subject.Projection.Should().BeNull();
            subject.Sort.Should().BeNull();
            subject.WriteConcern.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Projection_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x :  2 }")]
            string valueString)
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Projection = value;
            var result = subject.Projection;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sort_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ y :  1 }")]
            string valueString)
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Sort = value;
            var result = subject.Sort;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void CreateCommand_should_return_the_expected_result()
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "remove", true }
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
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "remove", true },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_MaxTime_is_set(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var maxTime = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "remove", true },
                { "maxTimeMS", () => seconds.Value * 1000, seconds.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Projection_is_set(
            [Values(null, "{ x : 1 }", "{ y : 1 }")]
            string projectionString)
        {
            var projection = projectionString == null ? null : BsonDocument.Parse(projectionString);
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Projection = projection
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "remove", true },
                { "fields", projection, projection != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Sort_is_set(
            [Values(null, "{ x : 1 }", "{ y : 1 }")]
            string sortString)
        {
            var sort = sortString == null ? null : BsonDocument.Parse(sortString);
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Sort = sort
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "remove", true },
                { "sort", sort, sort != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };

            var result = subject.CreateCommand(Feature.FindAndModifyWriteConcern.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "remove", true },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set_and_not_supported()
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings)
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
            RequireServer.Check();
            EnsureTestData();
            var filter = BsonDocument.Parse("{ x : 1 }");
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, filter, _findAndModifyValueDeserializer, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().Be("{ _id : 10, x : 1, y : 'a' }");
            ReadAllFromCollection().Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Collation_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Collation);
            EnsureTestData();
            var filter = BsonDocument.Parse("{ y : 'a' }");
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, filter, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                Collation = new Collation("en_US", caseLevel: false, strength: CollationStrength.Primary),
                Sort = BsonDocument.Parse("{ _id : -1 }")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be("{ _id : 11, x : 2, y : 'A' }");
            ReadAllFromCollection().Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set_and_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.Collation);
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_there_is_a_write_concern_error(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.FindAndModifyWriteConcern).ClusterType(ClusterType.ReplicaSet);
            EnsureTestData();
            var filter = BsonDocument.Parse("{ x : 1 }");
            var resultSerializer = new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance);
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, filter, resultSerializer, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            var writeConcernException = exception.Should().BeOfType<MongoWriteConcernException>().Subject;
            var commandResult = writeConcernException.Result;
            var result = commandResult["value"].AsBsonDocument;
            result.Should().Be("{ _id : 10, x : 1, y : 'a' }");
            ReadAllFromCollection().Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_when_document_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new FindOneAndDeleteOperation<BsonDocument>(
                _collectionNamespace,
                BsonDocument.Parse("{ x : 3 }"),
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().BeNull();
            ReadAllFromCollection().Should().HaveCount(2);
        }

        private void EnsureTestData()
        {
            DropCollection();
            Insert(
                BsonDocument.Parse("{ _id : 10, x : 1, y : 'a' }"),
                BsonDocument.Parse("{ _id : 11, x : 2, y : 'A' }"));
        }
    }
}
