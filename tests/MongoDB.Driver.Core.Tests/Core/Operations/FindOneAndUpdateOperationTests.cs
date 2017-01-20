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
    public class FindOneAndUpdateOperationTests : OperationTestBase
    {
        private BsonDocument _filter;
        private IBsonSerializer<BsonDocument> _findAndModifyValueDeserializer;
        private BsonDocument _update;

        public FindOneAndUpdateOperationTests()
        {
            _filter = new BsonDocument("x", 1);
            _findAndModifyValueDeserializer = new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance);
            _update = BsonDocument.Parse("{ $set : { x : 0 } }");
        }

        [Fact]
        public void Constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new FindOneAndUpdateOperation<BsonDocument>(null, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void Constructor_should_throw_when_filter_is_null()
        {
            var exception = Record.Exception(() => new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, null, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("filter");
        }

        [Fact]
        public void Constructor_should_throw_when_update_is_null()
        {
            var exception = Record.Exception(() => new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, null, BsonDocumentSerializer.Instance, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("update");
        }

        [Fact]
        public void Constructor_should_throw_when_update_is_empty()
        {
            var exception = Record.Exception(() => new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, new BsonDocument(), BsonDocumentSerializer.Instance, _messageEncoderSettings));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("update");
        }

        [Fact]
        public void Constructor_should_throw_when_resultSerializer_is_null()
        {
            var exception = Record.Exception(() => new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("resultSerializer");
        }

        [Fact]
        public void Constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void Constructor_should_initialize_object()
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.Filter.Should().BeSameAs(_filter);
            subject.Update.Should().BeSameAs(_update);
            subject.ResultSerializer.Should().BeSameAs(BsonDocumentSerializer.Instance);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.BypassDocumentValidation.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.IsUpsert.Should().BeFalse();
            subject.MaxTime.Should().NotHaveValue();
            subject.Projection.Should().BeNull();
            subject.ReturnDocument.Should().Be(ReturnDocument.Before);
            subject.Sort.Should().BeNull();
            subject.WriteConcern.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void BypassDocumentValidation_get_and_set_should_work(
           [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.BypassDocumentValidation = value;
            var result = subject.BypassDocumentValidation;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsUpsert_get_and_set_should_work(
            [Values(false, true)]
            bool value)
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.IsUpsert = true;
            var result = subject.IsUpsert;

            result.Should().Be(true);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Projection_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ y : 2 }")]
            string valueString)
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Projection = value;
            var result = subject.Projection;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReturnDocument_get_and_set_should_work(
            [Values(ReturnDocument.After, ReturnDocument.Before)]
            ReturnDocument value)
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ReturnDocument = value;
            var result = subject.ReturnDocument;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sort_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ y : 2 }")]
            string valueString)
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Sort = value;
            var result = subject.Sort;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "update", _update }
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
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                BypassDocumentValidation = bypassDocumentValidation
            };
            var serverVersion = Feature.BypassDocumentValidation.SupportedOrNotSupportedVersion(useServerVersionSupportingBypassDocumentValidation);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "update", _update },
                { "bypassDocumentValidation", () => bypassDocumentValidation.Value, bypassDocumentValidation.HasValue && Feature.BypassDocumentValidation.IsSupported(serverVersion) }
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
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "update", _update },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_IsUpsert_is_set(
            [Values(false, true)]
            bool value)
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                IsUpsert = value
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "update", _update },
                { "upsert", () => true, value }
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
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "update", _update },
                { "maxTimeMS", () => seconds.Value * 1000, seconds.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Projection_is_set(
            [Values(null, "{ x : 1 }", "{ y : 1 }")]
            string projectionString)
        {
            var projection = projectionString == null ? null : BsonDocument.Parse(projectionString);
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Projection = projection
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "update", _update },
                { "fields", projection, projection != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReturnDocument_is_set(
            [Values(ReturnDocument.After, ReturnDocument.Before)]
            ReturnDocument value)
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                ReturnDocument = value
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "update", _update },
                { "new", true, value == ReturnDocument.After }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Sort_is_set(
            [Values(null, "{ x : 1 }", "{ y : 1 }")]
            string sortString)
        {
            var sort = sortString == null ? null : BsonDocument.Parse(sortString);
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Sort = sort
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "update", _update },
                { "sort", sort, sort != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w,
            [Values(false, true)]
            bool useServerVersionSupportingFindAndModifyWriteConcern)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var serverVersion = Feature.FindAndModifyWriteConcern.SupportedOrNotSupportedVersion(useServerVersionSupportingFindAndModifyWriteConcern);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "update", _update },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null && Feature.FindAndModifyWriteConcern.IsSupported(serverVersion) }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set_but_not_supported()
        {
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.Collation.LastNotSupportedVersion));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_against_an_existing_document_returning_the_original(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                ReturnDocument = ReturnDocument.Before,
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be("{ _id : 10, x : 1, y : 'a' }");
            ReadAllFromCollection().Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 10, x : 0, y : 'a' }"),
                BsonDocument.Parse("{ _id : 11, x : 2, y : 'A' }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_against_an_existing_document_returning_the_updated(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                ReturnDocument = ReturnDocument.After
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be("{ _id : 10, x : 0, y : 'a' }");
            ReadAllFromCollection().Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 10, x : 0, y : 'a' }"),
                BsonDocument.Parse("{ _id : 11, x : 2, y : 'A' }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_against_a_non_existing_document_returning_the_original(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var filter = BsonDocument.Parse("{ asdf : 1 }");
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, filter, _update, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                ReturnDocument = ReturnDocument.Before
            };

            var result = ExecuteOperation(subject, async);

            result.Should().BeNull();
            ReadAllFromCollection().Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 10, x : 1, y : 'a' }"),
                BsonDocument.Parse("{ _id : 11, x : 2, y : 'A' }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_against_a_non_existing_document_returning_the_updated(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var filter = BsonDocument.Parse("{ asdf : 1 }");
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, filter, _update, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                ReturnDocument = ReturnDocument.After
            };

            var result = ExecuteOperation(subject, async);

            result.Should().BeNull();
            ReadAllFromCollection().Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 10, x : 1, y : 'a' }"),
                BsonDocument.Parse("{ _id : 11, x : 2, y : 'A' }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_against_a_non_existing_document_returning_the_original_with_upsert(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var filter = BsonDocument.Parse("{ _id : 12 }");
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, filter, _update, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                ReturnDocument = ReturnDocument.Before,
                IsUpsert = true
            };

            var result = ExecuteOperation(subject, async);

            result.Should().BeNull();
            ReadAllFromCollection().Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 10, x : 1, y : 'a' }"),
                BsonDocument.Parse("{ _id : 11, x : 2, y : 'A' }"),
                BsonDocument.Parse("{ _id : 12, x : 0 }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_against_a_non_existing_document_returning_the_updated_with_upsert(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var filter = BsonDocument.Parse("{ _id : 12 }");
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, filter, _update, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be("{ _id : 12, x : 0 }");
            ReadAllFromCollection().Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 10, x : 1, y : 'a' }"),
                BsonDocument.Parse("{ _id : 11, x : 2, y : 'A' }"),
                BsonDocument.Parse("{ _id : 12, x : 0 }"));
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
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, filter, _update, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                ReturnDocument = ReturnDocument.Before,
                Collation = new Collation("en_US", caseLevel: false, strength: CollationStrength.Primary),
                Sort = BsonDocument.Parse("{ _id : -1 }")
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be("{ _id : 11, x : 2, y : 'A' }");
            ReadAllFromCollection().Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 10, x : 1, y : 'a' }"),
                BsonDocument.Parse("{ _id : 11, x : 0, y : 'A' }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set_and_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.Collation);
            EnsureTestData();
            var filter = BsonDocument.Parse("{ y : 'a' }");
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, filter, _update, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                ReturnDocument = ReturnDocument.Before,
                Collation = new Collation("en_US", caseLevel: false, strength: CollationStrength.Primary),
                Sort = BsonDocument.Parse("{ _id : -1 }")
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_there_is_a_write_concern_error(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.FindAndModifyWriteConcern).ClusterType(ClusterType.ReplicaSet);
            EnsureTestData();
            var subject = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, _filter, _update, _findAndModifyValueDeserializer, _messageEncoderSettings)
            {
                ReturnDocument = ReturnDocument.Before,
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            var writeConcernException = exception.Should().BeOfType<MongoWriteConcernException>().Subject;
            var commandResult = writeConcernException.Result;
            var result = commandResult["value"].AsBsonDocument;
            result.Should().Be("{ _id : 10, x : 1, y : 'a' }");
            ReadAllFromCollection().Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 10, x : 0, y : 'a' }"),
                BsonDocument.Parse("{ _id : 11, x : 2, y : 'A' }"));
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
