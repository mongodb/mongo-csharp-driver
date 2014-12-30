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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class FindOneAndDeleteOperationTests : OperationTestBase
    {
        private BsonDocument _filter;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();

            _filter = new BsonDocument("x", 1);
        }

        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new FindOneAndDeleteOperation<BsonDocument>(null, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_filter_is_null()
        {
            Action act = () => new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_result_serializer_is_null()
        {
            Action act = () => new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_initialize_object()
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.Filter.Should().Be(_filter);
            subject.ResultSerializer.Should().NotBeNull();
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        public void CreateCommand_should_create_the_correct_command(
            [Values(null, 10)] int? maxTimeMS,
            [Values(null, "{a: 1}")] string projection,
            [Values(null, "{b: 1}")] string sort)
        {
            var projectionDoc = projection == null ? (BsonDocument)null : BsonDocument.Parse(projection);
            var sortDoc = sort == null ? (BsonDocument)null : BsonDocument.Parse(sort);
            var subject = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, _filter, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                MaxTime = maxTimeMS.HasValue ? TimeSpan.FromMilliseconds(maxTimeMS.Value) : (TimeSpan?)null,
                Projection = projectionDoc,
                Sort = sortDoc
            };

            var expectedResult = new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _filter },
                { "sort", sortDoc, sortDoc != null },
                { "remove", true },
                { "fields", projectionDoc, projectionDoc != null },
                { "maxTimeMS", () => maxTimeMS.Value, maxTimeMS.HasValue }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_against_an_existing_document()
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(
                _collectionNamespace,
                BsonDocument.Parse("{x: 1}"),
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.Should().Be("{_id: 10, x: 1}");

            var serverList = await ReadAllFromCollectionAsync();

            serverList.Should().BeEmpty();
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_when_document_does_not_exist()
        {
            var subject = new FindOneAndDeleteOperation<BsonDocument>(
                _collectionNamespace,
                BsonDocument.Parse("{x: 2}"),
                new FindAndModifyValueDeserializer<BsonDocument>(BsonDocumentSerializer.Instance),
                _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.Should().BeNull();

            var serverList = await ReadAllFromCollectionAsync();

            serverList.Should().HaveCount(1);
        }

        private void EnsureTestData()
        {
            DropCollection();
            Insert(BsonDocument.Parse("{_id: 10, x: 1}"));
        }
    }
}
