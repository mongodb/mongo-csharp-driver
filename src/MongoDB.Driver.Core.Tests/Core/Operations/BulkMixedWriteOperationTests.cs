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
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class BulkMixedWriteOperationTests : OperationTestBase
    {
        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action action = () => new BulkMixedWriteOperation(null, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_requests_is_null()
        {
            Action action = () => new BulkMixedWriteOperation(_collectionNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action action = () => new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_initialize_subject()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.Requests.Should().BeEmpty();
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        public void IsOrdered_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.IsOrdered.Should().BeTrue();

            subject.IsOrdered = false;

            subject.IsOrdered.Should().BeFalse();
        }

        [Test]
        public void MaxBatchCount_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.MaxBatchCount.Should().Be(null);

            subject.MaxBatchCount = 20;

            subject.MaxBatchCount.Should().Be(20);
        }

        [Test]
        public void MaxBatchLength_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.MaxBatchLength.Should().Be(null);

            subject.MaxBatchLength = 20;

            subject.MaxBatchLength.Should().Be(20);
        }

        [Test]
        public void MaxDocumentSize_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.MaxDocumentSize.Should().Be(null);

            subject.MaxDocumentSize = 20;

            subject.MaxDocumentSize.Should().Be(20);
        }

        [Test]
        public void MaxWireDocumentSize_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.MaxWireDocumentSize.Should().Be(null);

            subject.MaxWireDocumentSize = 20;

            subject.MaxWireDocumentSize.Should().Be(20);
        }

        [Test]
        public void WriteConcern_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.WriteConcern.Should().Be(WriteConcern.Acknowledged);

            subject.WriteConcern = WriteConcern.W2;

            subject.WriteConcern.Should().Be(WriteConcern.W2);
        }

        [Test]
        public void ExecuteAsync_with_zero_requests_should_throw_an_exception()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            Func<Task> act = () => ExecuteOperationAsync(subject);

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_one_delete_against_a_matching_document()
        {
            var requests = new[] { new DeleteRequest(BsonDocument.Parse("{x: 1}")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(1);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(5);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_one_delete_against_a_matching_document_with_multi()
        {
            var requests = new[] { new DeleteRequest(BsonDocument.Parse("{x: 1}")) { Limit = 0 } };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(3);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(3);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_one_delete_without_matching_a_document()
        {
            var requests = new[] { new DeleteRequest(BsonDocument.Parse("{_id: 20}")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_multiple_deletes()
        {
            var requests = new[] 
            { 
                new DeleteRequest(BsonDocument.Parse("{_id: 1}")),
                new DeleteRequest(BsonDocument.Parse("{_id: 2}")) 
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(2);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(2);
            result.RequestCount.Should().Be(2);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(4);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_fewer_deletes_than_maxBatchCount()
        {
            var requests = new[] 
            { 
                new DeleteRequest(BsonDocument.Parse("{_id: 1}")),
                new DeleteRequest(BsonDocument.Parse("{_id: 2}")),
                new DeleteRequest(BsonDocument.Parse("{_id: 3}")) 
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                MaxBatchCount = 4
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(3);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(3);
            result.RequestCount.Should().Be(3);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(3);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_more_deletes_than_maxBatchCount()
        {
            var requests = new[] 
            { 
                new DeleteRequest(BsonDocument.Parse("{_id: 1}")),
                new DeleteRequest(BsonDocument.Parse("{_id: 2}")),
                new DeleteRequest(BsonDocument.Parse("{_id: 3}")) 
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                MaxBatchCount = 2
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(3);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(3);
            result.RequestCount.Should().Be(3);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(3);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_with_one_insert()
        {
            var requests = new[] { new InsertRequest(BsonDocument.Parse("{_id: 1, x: 3}")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(1);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(1);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_with_fewer_inserts_than_maxBatchCount()
        {
            var requests = new[] 
            { 
                new InsertRequest(BsonDocument.Parse("{_id: 1}")),
                new InsertRequest(BsonDocument.Parse("{_id: 2}")) 
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                MaxBatchCount = 3
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(2);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(2);
            result.RequestCount.Should().Be(2);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(2);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_with_more_inserts_than_maxBatchCount()
        {
            var requests = new[] 
            { 
                new InsertRequest(BsonDocument.Parse("{_id: 1}")),
                new InsertRequest(BsonDocument.Parse("{_id: 2}")), 
                new InsertRequest(BsonDocument.Parse("{_id: 3}")), 
                new InsertRequest(BsonDocument.Parse("{_id: 4}")) 
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                MaxBatchCount = 3
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(4);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(4);
            result.RequestCount.Should().Be(4);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(4);
        }

        [Test]
        public void ExecuteAsync_with_an_empty_update_document_should_throw()
        {
            var requests = new[] { new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), new BsonDocument()) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            Func<Task> act = () => ExecuteOperationAsync(subject);
            act.ShouldThrow<BsonSerializationException>();
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_one_update_against_a_matching_document()
        {
            var requests = new[] { new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 1}}")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(1);
            }
            result.MatchedCount.Should().Be(1); // I don't understand this...
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_one_update_against_a_matching_document_with_multi()
        {
            var requests = new[] { new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 1}}")) { IsMulti = true } };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(3);
            }
            result.MatchedCount.Should().Be(3);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_one_update_without_matching_a_document()
        {
            var requests = new[] { new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{_id: 20}"), BsonDocument.Parse("{$set: {a: 1}}")) { IsMulti = true } };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_fewer_updates_than_maxBatchCount()
        {
            var requests = new[] 
            { 
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 1}}")),
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 2}}"))
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                MaxBatchCount = 3
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(2);
            }
            result.MatchedCount.Should().Be(2);
            result.ProcessedRequests.Should().HaveCount(2);
            result.RequestCount.Should().Be(2);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_more_updates_than_maxBatchCount()
        {
            var requests = new[] 
            { 
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 1}}")),
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 2}}")),
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 3}}")),
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 4}}"))
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                MaxBatchCount = 3
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(4);
            }
            result.MatchedCount.Should().Be(4);
            result.ProcessedRequests.Should().HaveCount(4);
            result.RequestCount.Should().Be(4);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "2.6.0")]
        public async Task ExecuteAsync_with_a_very_large_upsert()
        {
            var smallDocument = new BsonDocument { { "_id", 7 }, { "x", "" } };
            var smallDocumentSize = smallDocument.ToBson().Length;
            var stringSize = 16 * 1024 * 1024 - smallDocumentSize;
            var largeString = new string('x', stringSize);

            var requests = new[] { new UpdateRequest(UpdateType.Update, new BsonDocument("_id", 7), new BsonDocument("$set", new BsonDocument("x", largeString))) { IsUpsert = true } };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().HaveCount(1);
            result.Upserts.Should().OnlyContain(x => x.Id == 7 && x.Index == 0);

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(7);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_an_upsert_matching_multiple_documents()
        {
            var requests = new[] 
            { 
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {y: 1}}")) { IsMulti = true, IsUpsert = true } 
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(3);
            }
            result.MatchedCount.Should().Be(3);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_an_upsert_matching_no_documents()
        {
            var requests = new[] 
            { 
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 5}"), BsonDocument.Parse("{$set: {y: 1}}")) { IsMulti = true, IsUpsert = true } 
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().HaveCount(1);

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(7);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_an_upsert_matching_one_document()
        {
            var requests = new[] 
            { 
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 3}"), BsonDocument.Parse("{$set: {y: 1}}")) { IsMulti = true, IsUpsert = true } 
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(0);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(1);
            }
            result.MatchedCount.Should().Be(1);
            result.ProcessedRequests.Should().HaveCount(1);
            result.RequestCount.Should().Be(1);
            result.Upserts.Should().HaveCount(0);

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_mixed_requests_and_ordered_is_false()
        {
            var requests = new WriteRequest[] 
            { 
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 3}"), BsonDocument.Parse("{$set: {y: 1}}")),
                new DeleteRequest(new BsonDocument("_id", 2)),
                new InsertRequest(new BsonDocument("_id", 7)),
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 4}"), BsonDocument.Parse("{$set: {y: 2}}"))
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                IsOrdered = false
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(1);
            result.InsertedCount.Should().Be(1);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(1);
            }
            result.MatchedCount.Should().Be(1);
            result.ProcessedRequests.Should().HaveCount(4);
            result.RequestCount.Should().Be(4);
            result.Upserts.Should().HaveCount(0);

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_mixed_requests_and_ordered_is_true()
        {
            var requests = new WriteRequest[] 
            { 
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 3}"), BsonDocument.Parse("{$set: {y: 1}}")),
                new DeleteRequest(new BsonDocument("_id", 2)),
                new InsertRequest(new BsonDocument("_id", 7)),
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 4}"), BsonDocument.Parse("{$set: {y: 2}}"))
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                IsOrdered = true
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(1);
            result.InsertedCount.Should().Be(1);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(1);
            }
            result.MatchedCount.Should().Be(1);
            result.ProcessedRequests.Should().HaveCount(4);
            result.RequestCount.Should().Be(4);
            result.Upserts.Should().HaveCount(0);

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(6);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_mixed_upserts_and_ordered_is_false()
        {
            var requests = new WriteRequest[] 
            { 
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 12}"), BsonDocument.Parse("{$set: {y: 1}}")) { IsUpsert = true },
                new DeleteRequest(new BsonDocument("_id", 2)),
                new InsertRequest(new BsonDocument("_id", 7)),
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 13}"), BsonDocument.Parse("{$set: {y: 2}}"))  { IsUpsert = true }
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                IsOrdered = false
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(1);
            result.InsertedCount.Should().Be(1);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(4);
            result.RequestCount.Should().Be(4);
            result.Upserts.Should().HaveCount(2);

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(8);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public async Task ExecuteAsync_with_mixed_upserts_and_ordered_is_true()
        {
            var requests = new WriteRequest[] 
            { 
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 12}"), BsonDocument.Parse("{$set: {y: 1}}")) { IsUpsert = true },
                new DeleteRequest(new BsonDocument("_id", 2)),
                new InsertRequest(new BsonDocument("_id", 7)),
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 4}"), BsonDocument.Parse("{$set: {y: 2}}")) { IsUpsert = true }
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                IsOrdered = true
            };

            var result = await ExecuteOperationAsync(subject);

            result.DeletedCount.Should().Be(1);
            result.InsertedCount.Should().Be(1);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(4);
            result.RequestCount.Should().Be(4);
            result.Upserts.Should().HaveCount(2);

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(8);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_with_an_error_in_the_first_batch_and_ordered_is_false()
        {
            var requests = new[]
            {
                new InsertRequest(new BsonDocument { { "_id", 1 }}),
                new InsertRequest(new BsonDocument { { "_id", 1 }}), // will fail
                new InsertRequest(new BsonDocument { { "_id", 3 }}),
                new InsertRequest(new BsonDocument { { "_id", 1 }}), // will fail
                new InsertRequest(new BsonDocument { { "_id", 5 }}),
            };

            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                IsOrdered = false
            };

            var ex = await CatchAsync<MongoBulkWriteOperationException>(() => ExecuteOperationAsync(subject));
            var result = ex.Result;
            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(3);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(5);
            result.RequestCount.Should().Be(5);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(3);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_with_an_error_in_the_first_batch_and_ordered_is_true()
        {
            var keys = new BsonDocument("x", 1);
            var createIndexRequests = new[] { new CreateIndexRequest(keys) { Unique = true } };
            var createIndexOperation = new CreateIndexesOperation(_collectionNamespace, createIndexRequests, _messageEncoderSettings);
            await ExecuteOperationAsync(createIndexOperation);

            var requests = new[]
            {
                new InsertRequest(new BsonDocument { { "_id", 1 }}),
                new InsertRequest(new BsonDocument { { "_id", 1 }}), // will fail
                new InsertRequest(new BsonDocument { { "_id", 3 }}),
                new InsertRequest(new BsonDocument { { "_id", 1 }}), // will fail
                new InsertRequest(new BsonDocument { { "_id", 5 }}),
            };

            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                IsOrdered = true
            };

            var ex = await CatchAsync<MongoBulkWriteOperationException>(() => ExecuteOperationAsync(subject));

            var result = ex.Result;
            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(1);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(2);
            result.RequestCount.Should().Be(5);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(1);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_with_an_error_in_the_second_batch_and_ordered_is_false()
        {
            var requests = new[]
            {
                new InsertRequest(new BsonDocument { { "_id", 1 }}),
                new InsertRequest(new BsonDocument { { "_id", 2 }}),
                new InsertRequest(new BsonDocument { { "_id", 3 }}),
                new InsertRequest(new BsonDocument { { "_id", 1 }}), // will fail
                new InsertRequest(new BsonDocument { { "_id", 5 }}),
            };

            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                IsOrdered = false,
                MaxBatchCount = 2
            };

            var ex = await CatchAsync<MongoBulkWriteOperationException>(() => ExecuteOperationAsync(subject));

            var result = ex.Result;
            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(4);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(5);
            result.RequestCount.Should().Be(5);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(4);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_with_an_error_in_the_second_batch_and_ordered_is_true()
        {
            var requests = new[]
            {
                new InsertRequest(new BsonDocument { { "_id", 1 }}),
                new InsertRequest(new BsonDocument { { "_id", 2 }}),
                new InsertRequest(new BsonDocument { { "_id", 3 }}),
                new InsertRequest(new BsonDocument { { "_id", 1 }}), // will fail
                new InsertRequest(new BsonDocument { { "_id", 5 }}),
            };

            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                IsOrdered = true,
                MaxBatchCount = 2
            };

            var ex = await CatchAsync<MongoBulkWriteOperationException>(() => ExecuteOperationAsync(subject));

            var result = ex.Result;
            result.DeletedCount.Should().Be(0);
            result.InsertedCount.Should().Be(3);
            if (result.IsModifiedCountAvailable)
            {
                result.ModifiedCount.Should().Be(0);
            }
            result.MatchedCount.Should().Be(0);
            result.ProcessedRequests.Should().HaveCount(4);
            result.RequestCount.Should().Be(5);
            result.Upserts.Should().BeEmpty();

            var list = await ReadAllFromCollectionAsync();
            list.Should().HaveCount(3);
        }

        private void EnsureTestData()
        {
            DropCollection();
            Insert(
                BsonDocument.Parse("{_id: 1, x: 1 }"),
                BsonDocument.Parse("{_id: 2, x: 1 }"),
                BsonDocument.Parse("{_id: 3, x: 1 }"),
                BsonDocument.Parse("{_id: 4, x: 2 }"),
                BsonDocument.Parse("{_id: 5, x: 2 }"),
                BsonDocument.Parse("{_id: 6, x: 3 }"));
        }
    }
}
