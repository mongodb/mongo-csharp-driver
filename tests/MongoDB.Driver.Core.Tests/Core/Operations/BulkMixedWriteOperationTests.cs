/* Copyright 2013-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class BulkMixedWriteOperationTests : OperationTestBase
    {
        [Fact]
        public void BypassDocumentValidation_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.BypassDocumentValidation.Should().NotHaveValue();

            subject.BypassDocumentValidation = true;

            subject.BypassDocumentValidation.Should().BeTrue();
        }

        [Fact]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action action = () => new BulkMixedWriteOperation(null, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_requests_is_null()
        {
            Action action = () => new BulkMixedWriteOperation(_collectionNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action action = () => new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_initialize_subject()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.BypassDocumentValidation.Should().NotHaveValue();
            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.Requests.Should().BeEmpty();
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Fact]
        public void IsOrdered_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.IsOrdered.Should().BeTrue();

            subject.IsOrdered = false;

            subject.IsOrdered.Should().BeFalse();
        }

        [Fact]
        public void MaxBatchCount_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.MaxBatchCount.Should().Be(null);

            subject.MaxBatchCount = 20;

            subject.MaxBatchCount.Should().Be(20);
        }

        [Fact]
        public void MaxBatchLength_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.MaxBatchLength.Should().Be(null);

            subject.MaxBatchLength = 20;

            subject.MaxBatchLength.Should().Be(20);
        }

        [Fact]
        public void MaxDocumentSize_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.MaxDocumentSize.Should().Be(null);

            subject.MaxDocumentSize = 20;

            subject.MaxDocumentSize.Should().Be(20);
        }

        [Fact]
        public void MaxWireDocumentSize_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.MaxWireDocumentSize.Should().Be(null);

            subject.MaxWireDocumentSize = 20;

            subject.MaxWireDocumentSize.Should().Be(20);
        }

        [Fact]
        public void WriteConcern_should_work()
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            subject.WriteConcern.Should().Be(WriteConcern.Acknowledged);

            subject.WriteConcern = WriteConcern.W2;

            subject.WriteConcern.Should().Be(WriteConcern.W2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_with_zero_requests_should_throw_an_exception(
            [Values(false, true)]
            bool async)
        {
            var subject = new BulkMixedWriteOperation(_collectionNamespace, Enumerable.Empty<WriteRequest>(), _messageEncoderSettings);

            Action act = () => ExecuteOperation(subject, async);

            act.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_with_collation_should_throw_when_collation_is_not_supported(
            [Values(typeof(DeleteRequest), typeof(UpdateRequest))] Type requestWithCollationType,
            [Values(false, true)] bool async)
        {
            var collation = new Collation("en_US");
            var requests = new List<WriteRequest>
            {
                new DeleteRequest(new BsonDocument("x", 1)),
                new InsertRequest(new BsonDocument("x", 1)),
                new UpdateRequest(UpdateType.Update, new BsonDocument("x", 1), new BsonDocument("$set", new BsonDocument("x", 2)))
            };
            if (requestWithCollationType == typeof(DeleteRequest))
            {
                requests.Add(new DeleteRequest(new BsonDocument("x", 1)) { Collation = collation });
            }
            if (requestWithCollationType == typeof(UpdateRequest))
            {
                requests.Add(new UpdateRequest(UpdateType.Update, new BsonDocument("x", 1), new BsonDocument("$set", new BsonDocument("x", 2))) { Collation = collation });
            }
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            if (Feature.Collation.IsSupported(CoreTestConfiguration.ServerVersion))
            {
                exception.Should().BeNull();
            }
            else
            {
                exception.Should().BeOfType<NotSupportedException>();
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_one_delete_against_a_matching_document(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[] { new DeleteRequest(BsonDocument.Parse("{x: 1}")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(5);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_one_delete_against_a_matching_document_with_multi(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[] { new DeleteRequest(BsonDocument.Parse("{x: 1}")) { Limit = 0 } };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(3);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_one_delete_without_matching_a_document(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[] { new DeleteRequest(BsonDocument.Parse("{_id: 20}")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_multiple_deletes(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[]
            {
                new DeleteRequest(BsonDocument.Parse("{_id: 1}")),
                new DeleteRequest(BsonDocument.Parse("{_id: 2}"))
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(4);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_fewer_deletes_than_maxBatchCount(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
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

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(3);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_more_deletes_than_maxBatchCount(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
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

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(3);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_one_insert(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new InsertRequest(BsonDocument.Parse("{_id: 1, x: 3}")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_fewer_inserts_than_maxBatchCount(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[]
            {
                new InsertRequest(BsonDocument.Parse("{_id: 1}")),
                new InsertRequest(BsonDocument.Parse("{_id: 2}"))
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                MaxBatchCount = 3
            };

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_more_inserts_than_maxBatchCount(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
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

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(4);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_one_update_against_a_matching_document(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[] { new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 1}}")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                BypassDocumentValidation = true
            };

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_one_update_against_a_matching_document_with_multi(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[] { new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 1}}")) { IsMulti = true } };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_one_update_without_matching_a_document(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[] { new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{_id: 20}"), BsonDocument.Parse("{$set: {a: 1}}")) { IsMulti = true } };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_fewer_updates_than_maxBatchCount(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[]
            {
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 1}}")),
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {a: 2}}"))
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                MaxBatchCount = 3
            };

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_more_updates_than_maxBatchCount(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
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

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_a_very_large_upsert(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.WriteCommands);
            EnsureTestData();
            var smallDocument = new BsonDocument { { "_id", 7 }, { "x", "" } };
            var smallDocumentSize = smallDocument.ToBson().Length;
            var stringSize = 16 * 1024 * 1024 - smallDocumentSize;
            var largeString = new string('x', stringSize);

            var requests = new[] { new UpdateRequest(UpdateType.Update, new BsonDocument("_id", 7), new BsonDocument("$set", new BsonDocument("x", largeString))) { IsUpsert = true } };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(7);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_an_upsert_matching_multiple_documents(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[]
            {
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 1}"), BsonDocument.Parse("{$set: {y: 1}}")) { IsMulti = true, IsUpsert = true }
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_an_upsert_matching_no_documents(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[]
            {
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 5}"), BsonDocument.Parse("{$set: {y: 1}}")) { IsMulti = true, IsUpsert = true }
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(7);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_an_upsert_matching_one_document(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var requests = new[]
            {
                new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{x: 3}"), BsonDocument.Parse("{$set: {y: 1}}")) { IsMulti = true, IsUpsert = true }
            };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_mixed_requests_and_ordered_is_false(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
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

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_mixed_requests_and_ordered_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
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

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(6);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_mixed_upserts_and_ordered_is_false(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
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

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(8);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_mixed_upserts_and_ordered_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
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

            var result = ExecuteOperation(subject, async);

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(8);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_an_error_in_the_first_batch_and_ordered_is_false(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
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

            Action action = () => ExecuteOperation(subject, async);
            var ex = action.ShouldThrow<MongoBulkWriteOperationException>().Subject.Single();

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(3);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_an_error_in_the_first_batch_and_ordered_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var keys = new BsonDocument("x", 1);
            var createIndexRequests = new[] { new CreateIndexRequest(keys) { Unique = true } };
            var createIndexOperation = new CreateIndexesOperation(_collectionNamespace, createIndexRequests, _messageEncoderSettings);

            ExecuteOperation(createIndexOperation, async);

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

            Action action = () => ExecuteOperation(subject, async);
            var ex = action.ShouldThrow<MongoBulkWriteOperationException>().Subject.Single();

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(1);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_an_error_in_the_second_batch_and_ordered_is_false(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
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

            Action action = () => ExecuteOperation(subject, async);
            var ex = action.ShouldThrow<MongoBulkWriteOperationException>().Subject.Single();

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(4);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_an_error_in_the_second_batch_and_ordered_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
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

            Action action = () => ExecuteOperation(subject, async);
            var ex = action.ShouldThrow<MongoBulkWriteOperationException>().Subject.Single();

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

            var list = ReadAllFromCollection(async);
            list.Should().HaveCount(3);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_unacknowledged_with_an_error_in_the_first_batch_and_ordered_is_false(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
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
                IsOrdered = false,
                WriteConcern = WriteConcern.Unacknowledged
            };

            using (var readWriteBinding = CreateReadWriteBinding(useImplicitSession: true))
            using (var channelSource = readWriteBinding.GetWriteChannelSource(CancellationToken.None))
            using (var channel = channelSource.GetChannel(CancellationToken.None))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, readWriteBinding.Session.Fork()))
            {
                var result = ExecuteOperation(subject, channelBinding, async);

                result.ProcessedRequests.Should().HaveCount(5);
                result.RequestCount.Should().Be(5);

                var list = ReadAllFromCollection(channelBinding);
                list.Should().HaveCount(3);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_unacknowledged_with_an_error_in_the_first_batch_and_ordered_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var keys = new BsonDocument("x", 1);
            var createIndexRequests = new[] { new CreateIndexRequest(keys) { Unique = true } };
            var createIndexOperation = new CreateIndexesOperation(_collectionNamespace, createIndexRequests, _messageEncoderSettings);

            ExecuteOperation(createIndexOperation, async);

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
                IsOrdered = true,
                WriteConcern = WriteConcern.Unacknowledged
            };

            using (var readWriteBinding = CreateReadWriteBinding(useImplicitSession: true))
            using (var channelSource = readWriteBinding.GetWriteChannelSource(CancellationToken.None))
            using (var channel = channelSource.GetChannel(CancellationToken.None))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, readWriteBinding.Session.Fork()))
            {
                var result = ExecuteOperation(subject, channelBinding, async);
                result.ProcessedRequests.Should().HaveCount(5);
                result.RequestCount.Should().Be(5);

                var list = ReadAllFromCollection(channelBinding);
                list.Should().HaveCount(1);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_unacknowledged_with_an_error_in_the_second_batch_and_ordered_is_false(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
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
                MaxBatchCount = 2,
                WriteConcern = WriteConcern.Unacknowledged
            };

            using (var readWriteBinding = CreateReadWriteBinding(useImplicitSession: true))
            using (var channelSource = readWriteBinding.GetWriteChannelSource(CancellationToken.None))
            using (var channel = channelSource.GetChannel(CancellationToken.None))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, readWriteBinding.Session.Fork()))
            {
                var result = ExecuteOperation(subject, channelBinding, async);
                result.ProcessedRequests.Should().HaveCount(5);
                result.RequestCount.Should().Be(5);

                var list = ReadAllFromCollection(channelBinding);
                list.Should().HaveCount(4);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_unacknowledged_with_an_error_in_the_second_batch_and_ordered_is_true(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
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
                MaxBatchCount = 2,
                WriteConcern = WriteConcern.Unacknowledged
            };

            using (var readWriteBinding = CreateReadWriteBinding(useImplicitSession: true))
            using (var channelSource = readWriteBinding.GetWriteChannelSource(CancellationToken.None))
            using (var channel = channelSource.GetChannel(CancellationToken.None))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, readWriteBinding.Session.Fork()))
            {
                var result = ExecuteOperation(subject, channelBinding, async);
                result.ProcessedRequests.Should().HaveCount(4);
                result.RequestCount.Should().Be(5);

                var list = ReadAllFromCollection(channelBinding);
                list.Should().HaveCount(3);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_delete_should_not_send_session_id_when_unacknowledged_writes(
            [Values(false, true)] bool useImplicitSession,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();

            var collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestMethod(nameof(BulkMixedWriteOperationTests), nameof(Execute_with_delete_should_not_send_session_id_when_unacknowledged_writes));
            DropCollection(collectionNamespace);
            var requests = new[] { new DeleteRequest(BsonDocument.Parse("{ x : 1 }")) };
            var subject = new BulkMixedWriteOperation(collectionNamespace, requests, _messageEncoderSettings)
            {
                WriteConcern = WriteConcern.Unacknowledged
            };

            VerifySessionIdWasNotSentIfUnacknowledgedWrite(subject, "delete", async, useImplicitSession);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_delete_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            var requests = new[] { new DeleteRequest(BsonDocument.Parse("{ x : 1 }")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "delete", async);
        }


        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_insert_should_not_send_session_id_when_unacknowledged_writes(
            [Values(false, true)] bool useImplicitSession,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();

            var collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestMethod(nameof(BulkMixedWriteOperationTests), nameof(Execute_with_insert_should_not_send_session_id_when_unacknowledged_writes));
            DropCollection(collectionNamespace);
            var requests = new[] { new InsertRequest(BsonDocument.Parse("{ _id : 1, x : 3 }")) };
            var subject = new BulkMixedWriteOperation(collectionNamespace, requests, _messageEncoderSettings)
            {
                WriteConcern = WriteConcern.Unacknowledged
            };

            VerifySessionIdWasNotSentIfUnacknowledgedWrite(subject, "insert", async, useImplicitSession);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_insert_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            DropCollection();
            var requests = new[] { new InsertRequest(BsonDocument.Parse("{ _id : 1, x : 3 }")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "insert", async);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_update_should_not_send_session_id_when_unacknowledged_writes(
            [Values(false, true)] bool useImplicitSession,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();

            var collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestMethod(nameof(BulkMixedWriteOperationTests), nameof(Execute_with_update_should_not_send_session_id_when_unacknowledged_writes));
            DropCollection(collectionNamespace);
            var requests = new[] { new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{ x : 1 }"), BsonDocument.Parse("{ $set : { a : 1 } }")) };
            var subject = new BulkMixedWriteOperation(collectionNamespace, requests, _messageEncoderSettings)
            {
                WriteConcern = WriteConcern.Unacknowledged
            };

            VerifySessionIdWasNotSentIfUnacknowledgedWrite(subject, "update", async, useImplicitSession);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_with_update_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            var requests = new[] { new UpdateRequest(UpdateType.Update, BsonDocument.Parse("{ x : 1 }"), BsonDocument.Parse("{ $set : { a : 1 } }")) };
            var subject = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "update", async);
        }

        [SkippableTheory]
        [InlineData(new[] { 1 }, new[] { 1 })]
        [InlineData(new[] { 1, 1 }, new[] { 2 })]
        [InlineData(new[] { 10000000, 10000000, 10000000, 10000000 }, new[] { 4 })]
        [InlineData(new[] { 10000000, 10000000, 10000000, 10000000, 7999887 }, new[] { 5 })]
        [InlineData(new[] { 10000000, 10000000, 10000000, 10000000, 7999888 }, new[] { 4, 1 })]
        public void Execute_with_multiple_deletes_should_split_batches_as_expected_when_using_write_commands_via_opmessage(int[] requestSizes, int[] expectedBatchCounts)
        {
            RequireServer.Check().Supports(Feature.WriteCommands, Feature.CommandMessage);
            DropCollection();

            using (EventContext.BeginOperation())
            {
                var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "delete" && e.OperationId == EventContext.OperationId);
                using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
                using (var session = NoCoreSession.NewHandle())
                using (var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster, session.Fork())))
                {
                    var requests = requestSizes.Select(size => CreateDeleteRequest(size));
                    var operation = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

                    var result = ExecuteOperation(operation, binding, async: false);

                    var commandStartedEvents = eventCapturer.Events.OfType<CommandStartedEvent>().ToList();
                    var actualBatchCounts = commandStartedEvents.Select(e => e.Command["deletes"].AsBsonArray.Count).ToList();
                    actualBatchCounts.Should().Equal(expectedBatchCounts);
                }
            }
        }

        [SkippableTheory]
        [InlineData(new[] { 1 }, new[] { 1 })]
        [InlineData(new[] { 1, 1 }, new[] { 2 })]
        [InlineData(new[] { 8388605, 8388605 }, new[] { 2 })]
        [InlineData(new[] { 8388605, 8388606 }, new[] { 1, 1 })]
        [InlineData(new[] { 16777216 }, new[] { 1 })]
        [InlineData(new[] { 16777216, 1 }, new[] { 1, 1 })]
        [InlineData(new[] { 16777216, 16777216 }, new[] { 1, 1 })]
        public void Execute_with_multiple_deletes_should_split_batches_as_expected_when_using_write_commands_via_opquery(int[] requestSizes, int[] expectedBatchCounts)
        {
            RequireServer.Check().Supports(Feature.WriteCommands).DoesNotSupport(Feature.CommandMessage);
            DropCollection();

            using (EventContext.BeginOperation())
            {
                var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "delete" && e.OperationId == EventContext.OperationId);
                using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
                using (var session = NoCoreSession.NewHandle())
                using (var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster, session.Fork())))
                {
                    var requests = requestSizes.Select(size => CreateDeleteRequest(size));
                    var operation = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

                    var result = ExecuteOperation(operation, binding, async: false);

                    result.RequestCount.Should().Be(requestSizes.Length);
                    var commandStartedEvents = eventCapturer.Events.OfType<CommandStartedEvent>().ToList();
                    var actualBatchCounts = commandStartedEvents.Select(e => e.Command["deletes"].AsBsonArray.Count).ToList();
                    actualBatchCounts.Should().Equal(expectedBatchCounts);
                }
            }
        }

        [SkippableTheory]
        [InlineData(new[] { 1 }, new[] { 1 })]
        [InlineData(new[] { 1, 1 }, new[] { 2 })]
        [InlineData(new[] { 10000000, 10000000, 10000000, 10000000 }, new[] { 4 })]
        [InlineData(new[] { 10000000, 10000000, 10000000, 10000000, 7999885 }, new[] { 5 })]
        [InlineData(new[] { 10000000, 10000000, 10000000, 10000000, 7999886 }, new[] { 4, 1 })]
        public void Execute_with_multiple_inserts_should_split_batches_as_expected_when_using_write_commands_via_opmessage(int[] documentSizes, int[] expectedBatchCounts)
        {
            RequireServer.Check().Supports(Feature.WriteCommands, Feature.CommandMessage);
            DropCollection();

            using (EventContext.BeginOperation())
            {
                var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "insert" && e.OperationId == EventContext.OperationId);
                using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
                using (var session = NoCoreSession.NewHandle())
                using (var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster, session.Fork())))
                {
                    var documents = documentSizes.Select((size, index) => CreateDocument(index + 1, size)).ToList();
                    var requests = documents.Select(d => new InsertRequest(d)).ToList();
                    var operation = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

                    var result = ExecuteOperation(operation, binding, async: false);

                    result.InsertedCount.Should().Be(documents.Count);
                    var commandStartedEvents = eventCapturer.Events.OfType<CommandStartedEvent>().ToList();
                    var actualBatchCounts = commandStartedEvents.Select(e => e.Command["documents"].AsBsonArray.Count).ToList();
                    actualBatchCounts.Should().Equal(expectedBatchCounts);
                }
            }
        }

        [SkippableTheory]
        [InlineData(new[] { 1 }, new[] { 1 })]
        [InlineData(new[] { 1, 1 }, new[] { 2 })]
        [InlineData(new[] { 8388605, 8388605 }, new[] { 2 })]
        [InlineData(new[] { 8388605, 8388606 }, new[] { 1, 1 })]
        [InlineData(new[] { 16777216 }, new[] { 1 })]
        [InlineData(new[] { 16777216, 1 }, new[] { 1, 1 })]
        [InlineData(new[] { 16777216, 16777216 }, new[] { 1, 1 })]
        public void Execute_with_multiple_inserts_should_split_batches_as_expected_when_using_write_commands_via_opquery(int[] documentSizes, int[] expectedBatchCounts)
        {
            RequireServer.Check().Supports(Feature.WriteCommands).DoesNotSupport(Feature.CommandMessage);
            DropCollection();

            using (EventContext.BeginOperation())
            {
                var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "insert" && e.OperationId == EventContext.OperationId);
                using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
                using (var session = NoCoreSession.NewHandle())
                using (var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster, session.Fork())))
                {
                    var documents = documentSizes.Select((size, index) => CreateDocument(index + 1, size)).ToList();
                    var requests = documents.Select(d => new InsertRequest(d)).ToList();
                    var operation = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

                    var result = ExecuteOperation(operation, binding, async: false);

                    result.InsertedCount.Should().Be(documents.Count);
                    var commandStartedEvents = eventCapturer.Events.OfType<CommandStartedEvent>().ToList();
                    var actualBatchCounts = commandStartedEvents.Select(e => e.Command["documents"].AsBsonArray.Count).ToList();
                    actualBatchCounts.Should().Equal(expectedBatchCounts);
                }
            }
        }

        [SkippableTheory]
        [InlineData(new[] { 1 }, new[] { 1 })]
        [InlineData(new[] { 1, 1 }, new[] { 2 })]
        [InlineData(new[] { 10000000, 10000000, 10000000, 10000000 }, new[] { 4 })]
        [InlineData(new[] { 10000000, 10000000, 10000000, 10000000, 7999887 }, new[] { 5 })]
        [InlineData(new[] { 10000000, 10000000, 10000000, 10000000, 7999888 }, new[] { 4, 1 })]
        public void Execute_with_multiple_updates_should_split_batches_as_expected_when_using_write_commands_via_opmessage(int[] requestSizes, int[] expectedBatchCounts)
        {
            RequireServer.Check().Supports(Feature.WriteCommands, Feature.CommandMessage);
            DropCollection();

            using (EventContext.BeginOperation())
            {
                var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "update" && e.OperationId == EventContext.OperationId);
                using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
                using (var session = NoCoreSession.NewHandle())
                using (var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster, session.Fork())))
                {
                    var requests = requestSizes.Select(size => CreateUpdateRequest(size));
                    var operation = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

                    var result = ExecuteOperation(operation, binding, async: false);

                    var commandStartedEvents = eventCapturer.Events.OfType<CommandStartedEvent>().ToList();
                    var actualBatchCounts = commandStartedEvents.Select(e => e.Command["updates"].AsBsonArray.Count).ToList();
                    actualBatchCounts.Should().Equal(expectedBatchCounts);
                }
            }
        }

        [SkippableTheory]
        [InlineData(new[] { 1 }, new[] { 1 })]
        [InlineData(new[] { 1, 1 }, new[] { 2 })]
        [InlineData(new[] { 8388605, 8388605 }, new[] { 2 })]
        [InlineData(new[] { 8388605, 8388606 }, new[] { 1, 1 })]
        [InlineData(new[] { 16777216 }, new[] { 1 })]
        [InlineData(new[] { 16777216, 1 }, new[] { 1, 1 })]
        [InlineData(new[] { 16777216, 16777216 }, new[] { 1, 1 })]
        public void Execute_with_multiple_updates_should_split_batches_as_expected_when_using_write_commands_via_opquery(int[] requestSizes, int[] expectedBatchCounts)
        {
            RequireServer.Check().Supports(Feature.WriteCommands).DoesNotSupport(Feature.CommandMessage);
            DropCollection();

            using (EventContext.BeginOperation())
            {
                var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "update" && e.OperationId == EventContext.OperationId);
                using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
                using (var session = NoCoreSession.NewHandle())
                using (var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster, session.Fork())))
                {
                    var requests = requestSizes.Select(size => CreateUpdateRequest(size));
                    var operation = new BulkMixedWriteOperation(_collectionNamespace, requests, _messageEncoderSettings);

                    var result = ExecuteOperation(operation, binding, async: false);

                    result.RequestCount.Should().Be(requestSizes.Length);
                    var commandStartedEvents = eventCapturer.Events.OfType<CommandStartedEvent>().ToList();
                    var actualBatchCounts = commandStartedEvents.Select(e => e.Command["updates"].AsBsonArray.Count).ToList();
                    actualBatchCounts.Should().Equal(expectedBatchCounts);
                }
            }
        }

        // private methods
        private DeleteRequest CreateDeleteRequest(int size)
        {
            var filter = new BsonDocument("filler", "");
            var requestDocument = new BsonDocument
            {
                { "q", filter },
                { "limit", 1 }
            };
            var fillerSize = size - requestDocument.ToBson().Length;
            if (fillerSize > 0)
            {
                filter["filler"] = new string('x', fillerSize);
            }
            return new DeleteRequest(filter);
        }

        private BsonDocument CreateDocument(int id, int size)
        {
            var document = new BsonDocument { { "_id", id }, { "filler", "" } };
            var fillerSize = size - document.ToBson().Length;
            if (fillerSize > 0)
            {
                document["filler"] = new string('x', fillerSize);
            }
            return document;
        }

        private UpdateRequest CreateUpdateRequest(int size)
        {
            var filter = new BsonDocument("filler", "");
            var update = new BsonDocument("$set", new BsonDocument("x", 1));
            var requestDocument = new BsonDocument
            {
                { "q", filter },
                { "u", update }
            };
            var fillerSize = size - requestDocument.ToBson().Length;
            if (fillerSize > 0)
            {
                filter["filler"] = new string('x', fillerSize);
            }
            return new UpdateRequest(UpdateType.Update, filter, update);
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

        private List<BsonDocument> ReadAllFromCollection(IReadBinding binding)
        {
            var operation = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var cursor = ExecuteOperation(operation, binding, false);
            return ReadCursorToEnd(cursor);
        }
    }
}
