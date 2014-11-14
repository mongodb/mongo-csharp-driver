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
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class FindOperationTests : OperationTestBase
    {
        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new FindOperation<BsonDocument>(null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_should_throw_when_result_serializer_is_null()
        {
            Action act = () => new FindOperation<BsonDocument>(_collectionNamespace, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_initialize_object()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.ResultSerializer.Should().NotBeNull();
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        public void CreateWrappedQuery_should_create_the_correct_query_when_not_connected_to_a_shard_router()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Comment = "funny",
                Filter = BsonDocument.Parse("{x: 1}"),
                MaxTime = TimeSpan.FromSeconds(20),
                Modifiers = BsonDocument.Parse("{$comment: \"notfunny\", $snapshot: true}"),
                Projection = BsonDocument.Parse("{y: 1}"),
                Sort = BsonDocument.Parse("{a: 1}")
            };

            var expectedResult = new BsonDocument
            {
                { "$query", BsonDocument.Parse("{x: 1}") },
                { "$orderby", BsonDocument.Parse("{a: 1}") },
                { "$comment", "funny" },
                { "$maxTimeMS", 20000 },
                { "$snapshot", true }
            };


            var result = subject.CreateWrappedQuery(ServerType.ReplicaSetArbiter, ReadPreference.Secondary);

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateWrappedQuery_should_create_the_correct_query_when_connected_to_a_shard_router()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Comment = "funny",
                Filter = BsonDocument.Parse("{x: 1}"),
                MaxTime = TimeSpan.FromSeconds(20),
                Modifiers = BsonDocument.Parse("{$comment: \"notfunny\", $snapshot: true}"),
                Projection = BsonDocument.Parse("{y: 1}"),
                Sort = BsonDocument.Parse("{a: 1}")
            };

            var expectedResult = new BsonDocument
            {
                { "$query", BsonDocument.Parse("{x: 1}") },
                { "$readPreference", BsonDocument.Parse("{mode: \"secondary\"}") },
                { "$orderby", BsonDocument.Parse("{a: 1}") },
                { "$comment", "funny" },
                { "$maxTimeMS", 20000 },
                { "$snapshot", true }
            };

            var result = subject.CreateWrappedQuery(ServerType.ShardRouter, ReadPreference.Secondary);

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_should_find_all_the_documents_matching_the_query()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var cursor = await ExecuteOperationAsync(subject);

            var result = await ReadCursorToEndAsync(cursor);

            result.Should().HaveCount(5);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_should_find_all_the_documents_matching_the_query_when_split_across_batches()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                BatchSize = 2
            };

            var cursor = await ExecuteOperationAsync(subject);

            var result = await ReadCursorToEndAsync(cursor);

            result.Should().HaveCount(5);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_should_find_documents_matching_options()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Comment = "funny",
                Filter = BsonDocument.Parse("{y: 1}"),
                Limit = 4,
                MaxTime = TimeSpan.FromSeconds(20),
                Projection = BsonDocument.Parse("{y: 1}"),
                Skip = 1,
                Sort = BsonDocument.Parse("{_id: -1}")
            };

            var cursor = await ExecuteOperationAsync(subject);

            var result = await ReadCursorToEndAsync(cursor);

            result.Should().HaveCount(1);
        }

        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(
                    new BsonDocument("_id", 1).Add("y", 1),
                    new BsonDocument("_id", 2).Add("y", 1),
                    new BsonDocument("_id", 3).Add("y", 2),
                    new BsonDocument("_id", 4).Add("y", 2),
                    new BsonDocument("_id", 5).Add("y", 3));
            });
        }
    }
}
