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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class FindOneAndReplaceOperationTests : OperationTestBase
    {
        private BsonDocument _filter;
        private BsonDocument _replacement;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();

            _filter = new BsonDocument("x", 1);
            _replacement = BsonDocument.Parse("{_id: 10, a: 2}");
        }

        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new FindOneAndReplaceOperation<BsonDocument>(null, _filter, _replacement, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_filter_is_null()
        {
            Action act = () => new FindOneAndReplaceOperation<BsonDocument>(_collectionNamespace, null, _replacement, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_replacement_is_null()
        {
            Action act = () => new FindOneAndReplaceOperation<BsonDocument>(_collectionNamespace, _filter, null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_result_serializer_is_null()
        {
            Action act = () => new FindOneAndReplaceOperation<BsonDocument>(_collectionNamespace, _filter, _replacement, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new FindOneAndReplaceOperation<BsonDocument>(_collectionNamespace, _filter, _replacement, BsonDocumentSerializer.Instance, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_initialize_object()
        {
            var subject = new FindOneAndReplaceOperation<BsonDocument>(_collectionNamespace, _filter, _replacement, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.BypassDocumentValidation.Should().NotHaveValue();
            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.Filter.Should().Be(_filter);
            subject.Replacement.Should().Be(_replacement);
            subject.ResultSerializer.Should().NotBeNull();
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        public void CreateCommand_should_create_the_correct_command(
            [Values(null, false, true)] bool? bypassDocumentValidation,
            [Values(false, true)] bool isUpsert,
            [Values(null, 10)] int? maxTimeMS,
            [Values(null, "{a: 1}")] string projection,
            [Values(ReturnDocument.Before, ReturnDocument.After)] ReturnDocument returnDocument,
            [Values(null, "{b: 1}")] string sort,
            [Values(null, "{ w : 2 }")] string writeConcernString,
            [Values("3.0.0", "3.1.1")] string serverVersionString)
        {
            var projectionDoc = projection == null ? (BsonDocument)null : BsonDocument.Parse(projection);
            var sortDoc = sort == null ? (BsonDocument)null : BsonDocument.Parse(sort);
            var writeConcern = writeConcernString == null ? null : WriteConcern.FromBsonDocument(BsonDocument.Parse(writeConcernString));
            var serverVersion = SemanticVersion.Parse(serverVersionString);
            var subject = new FindOneAndReplaceOperation<BsonDocument>(_collectionNamespace, _filter, _replacement, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                BypassDocumentValidation = bypassDocumentValidation,
                IsUpsert = isUpsert,
                MaxTime = maxTimeMS.HasValue ? TimeSpan.FromMilliseconds(maxTimeMS.Value) : (TimeSpan?)null,
                Projection = projectionDoc,
                ReturnDocument = returnDocument,
                Sort = sortDoc,
                WriteConcern = writeConcern
            };

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "sort", sortDoc, sortDoc != null },
                { "update", _replacement, _replacement != null },
                { "new", returnDocument == ReturnDocument.After },
                { "fields", projectionDoc, projectionDoc != null },
                { "upsert", isUpsert },
                { "maxTimeMS", () => maxTimeMS.Value, maxTimeMS.HasValue },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null && SupportedFeatures.IsFindAndModifyWriteConcernSupported(serverVersion) },
                { "bypassDocumentValidation", () => bypassDocumentValidation.Value, bypassDocumentValidation.HasValue && SupportedFeatures.IsBypassDocumentValidationSupported(serverVersion) }
            };

            var result = subject.CreateCommand(serverVersion);

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_against_an_existing_document_returning_the_original(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOneAndReplaceOperation<BsonDocument>(
                _collectionNamespace,
                _filter,
                _replacement,
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings)
            {
                BypassDocumentValidation = true,
                ReturnDocument = ReturnDocument.Before
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be("{_id: 10, x: 1}");

            var serverList = ReadAllFromCollection(async);

            serverList[0].Should().Be("{_id: 10, a: 2}");
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_against_an_existing_document_returning_the_replacement(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOneAndReplaceOperation<BsonDocument>(
                _collectionNamespace,
                _filter,
                _replacement,
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings)
            {
                BypassDocumentValidation = true,
                ReturnDocument = ReturnDocument.After
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be("{_id: 10, a: 2}");

            var serverList = ReadAllFromCollection(async);

            serverList[0].Should().Be("{_id: 10, a: 2}");
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_against_a_non_existing_document_returning_the_original(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOneAndReplaceOperation<BsonDocument>(
                _collectionNamespace,
                BsonDocument.Parse("{alkjasdf: 10}"),
                _replacement,
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings)
            {
                BypassDocumentValidation = true,
                ReturnDocument = ReturnDocument.Before
            };

            var result = ExecuteOperation(subject, async);

            result.Should().BeNull();

            var serverList = ReadAllFromCollection(async);

            serverList[0].Should().Be("{_id: 10, x: 1}");
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_against_a_non_existing_document_returning_the_replacement(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOneAndReplaceOperation<BsonDocument>(
                _collectionNamespace,
                BsonDocument.Parse("{alkjasdf: 10}"),
                _replacement,
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings)
            {
                BypassDocumentValidation = true,
                ReturnDocument = ReturnDocument.After
            };

            var result = ExecuteOperation(subject, async);

            result.Should().BeNull();

            var serverList = ReadAllFromCollection(async);

            serverList[0].Should().Be("{_id: 10, x: 1}");
        }

        [Test]
        [RequiresServer("DropCollection")]
        public void Execute_against_a_non_existing_document_returning_the_original_with_upsert(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOneAndReplaceOperation<BsonDocument>(
                _collectionNamespace,
                BsonDocument.Parse("{_id: 10}"),
                _replacement,
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings)
            {
                BypassDocumentValidation = true,
                IsUpsert = true,
                ReturnDocument = ReturnDocument.Before
            };

            var result = ExecuteOperation(subject, async);

            result.Should().BeNull();

            var serverList = ReadAllFromCollection(async);

            serverList[0].Should().Be("{_id: 10, a: 2}");
        }

        [Test]
        [RequiresServer("DropCollection")]
        public void Execute_against_a_non_existing_document_returning_the_replacement_with_upsert(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOneAndReplaceOperation<BsonDocument>(
                _collectionNamespace,
                BsonDocument.Parse("{_id: 10}"),
                _replacement,
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings)
            {
                BypassDocumentValidation = true,
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Be("{_id: 10, a: 2}");

            var serverList = ReadAllFromCollection(async);

            serverList[0].Should().Be("{_id: 10, a: 2}");
        }
        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "3.2.0-rc0", ClusterTypes = ClusterTypes.ReplicaSet)]
        public void Execute_should_throw_when_there_is_a_write_concern_error(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOneAndReplaceOperation<BsonDocument>(
                _collectionNamespace,
                _filter,
                _replacement,
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings)
            {
                BypassDocumentValidation = true,
                ReturnDocument = ReturnDocument.Before,
                WriteConcern = new WriteConcern(9)
            };

            Action action = () => ExecuteOperation(subject, async);

            var exception = action.ShouldThrow<MongoWriteConcernException>().Which;
            var commandResult = exception.Result;
            var result = commandResult["value"].AsBsonDocument;
            result.Should().Be("{_id: 10, x: 1}");
            var serverList = ReadAllFromCollection(async);
            serverList[0].Should().Be("{_id: 10, a: 2}");
        }


        private void EnsureTestData()
        {
            DropCollection();
            Insert(BsonDocument.Parse("{_id: 10, x: 1}"));
        }
    }
}
