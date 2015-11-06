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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class FindOperationTests : OperationTestBase
    {
        // public methods
        [Test]
        public void AllowPartialResults_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.AllowPartialResults = value;
            var result = subject.AllowPartialResults;

            result.Should().Be(value);
        }

        [Test]
        public void BatchSize_get_and_set_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.BatchSize = value;
            var result = subject.BatchSize;

            result.Should().Be(value);
        }

        [Test]
        public void BatchSize_set_should_throw_when_value_is_invalid(
            [Values(-1)]
            int value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Action action = () => subject.BatchSize = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void CollectionNamespace_get_should_return_expected_result(
            [Values("a", "b")]
            string collectionName)
        {
            var databaseNamespace = new DatabaseNamespace("test");
            var collectionNamespace = new CollectionNamespace(databaseNamespace, collectionName);
            var subject = new FindOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().Be(collectionNamespace);
        }

        [Test]
        public void Comment_get_and_set_should_work(
            [Values(null, "a", "b")]
            string value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Comment = value;
            var result = subject.Comment;

            result.Should().Be(value);
        }

        [Test]
        public void constructor_should_initialize_instance()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.ResultSerializer.Should().NotBeNull();
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);

            subject.AllowPartialResults.Should().NotHaveValue();
            subject.BatchSize.Should().NotHaveValue();
            subject.Comment.Should().BeNull();
            subject.CursorType.Should().Be(CursorType.NonTailable);
            subject.Filter.Should().BeNull();
            subject.FirstBatchSize.Should().NotHaveValue();
            subject.Hint.Should().BeNull();
            subject.Limit.Should().NotHaveValue();
            subject.Max.Should().BeNull();
            subject.MaxScan.Should().NotHaveValue();
            subject.MaxTime.Should().NotHaveValue();
            subject.Min.Should().BeNull();
            subject.Modifiers.Should().BeNull();
            subject.NoCursorTimeout.Should().NotHaveValue();
            subject.OplogReplay.Should().NotHaveValue();
            subject.Projection.Should().BeNull();
            subject.ReadConcern.Should().Be(ReadConcern.Default);
            subject.ResultSerializer.Should().Be(BsonDocumentSerializer.Instance);
            subject.ReturnKey.Should().NotHaveValue();
            subject.ShowRecordId.Should().NotHaveValue();
            subject.SingleBatch.Should().NotHaveValue();
            subject.Skip.Should().NotHaveValue();
            subject.Snapshot.Should().NotHaveValue();
            subject.Sort.Should().BeNull();
        }

        [Test]
        public void constructor_should_throw_when_collection_namespace_is_null()
        {
            Action action = () => new FindOperation<BsonDocument>(null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Test]
        public void constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action action = () => new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageEncoderSettings");
        }

        [Test]
        public void constructor_should_throw_when_result_serializer_is_null()
        {
            Action action = () => new FindOperation<BsonDocument>(_collectionNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("resultSerializer");
        }

        [Test]
        public void CreateFindCommandOperation_should_return_expected_result()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.AllowPartialResults = true;
            subject.BatchSize = 1;
            subject.Comment = "comment";
            subject.CursorType = CursorType.Tailable;
            subject.Filter = new BsonDocument("filter", 1);
            subject.FirstBatchSize = 2;
            subject.Hint = "x_1";
            subject.Limit = 3;
            subject.Max = new BsonDocument("max", 1);
            subject.MaxScan = 4;
            subject.MaxAwaitTime = TimeSpan.FromSeconds(2);
            subject.MaxTime = TimeSpan.FromSeconds(1);
            subject.Min = new BsonDocument("min", 1);
            subject.NoCursorTimeout = true;
            subject.OplogReplay = true;
            subject.Projection = new BsonDocument("projection", 1);
            subject.ReadConcern = ReadConcern.Local;
            subject.ReturnKey = true;
            subject.ShowRecordId = true;
            subject.SingleBatch = true;
            subject.Skip = 6;
            subject.Snapshot = true;
            subject.Sort = new BsonDocument("sort", 1);

            var result = subject.CreateFindCommandOperation();

            result.AllowPartialResults.Should().Be(subject.AllowPartialResults);
            result.BatchSize.Should().Be(subject.BatchSize);
            result.CollectionNamespace.Should().Be(subject.CollectionNamespace);
            result.Comment.Should().Be(subject.Comment);
            result.CursorType.Should().Be(subject.CursorType);
            result.Filter.Should().Be(subject.Filter);
            result.FirstBatchSize.Should().Be(subject.FirstBatchSize);
            result.Hint.Should().Be(subject.Hint);
            result.Limit.Should().Be(subject.Limit);
            result.Max.Should().Be(subject.Max);
            result.MaxScan.Should().Be(subject.MaxScan);
            result.MaxAwaitTime.Should().Be(subject.MaxAwaitTime);
            result.MaxTime.Should().Be(subject.MaxTime);
            result.MessageEncoderSettings.Should().BeSameAs(subject.MessageEncoderSettings);
            result.Min.Should().Be(subject.Min);
            result.NoCursorTimeout.Should().Be(subject.NoCursorTimeout);
            result.OplogReplay.Should().Be(subject.OplogReplay);
            result.Projection.Should().Be(subject.Projection);
            result.ReadConcern.Should().Be(subject.ReadConcern);
            result.ResultSerializer.Should().Be(subject.ResultSerializer);
            result.ReturnKey.Should().Be(subject.ReturnKey);
            result.ShowRecordId.Should().Be(subject.ShowRecordId);
            result.SingleBatch.Should().Be(subject.SingleBatch);
            result.Skip.Should().Be(subject.Skip);
            result.Snapshot.Should().Be(subject.Snapshot);
            result.Sort.Should().Be(subject.Sort);
        }

        [Test]
        public void CreateFindCommandOperation_should_return_expected_result_when_modifiers_are_provided()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                ReadConcern = ReadConcern.Majority
            };
            subject.Modifiers = new BsonDocument
            {
                { "$hint", "x_1" },
                { "$max", new BsonDocument("max", 1) },
                { "$maxScan", 1 },
                { "$maxTimeMS", 2000 },
                { "$min", new BsonDocument("min", 1) },
                { "$orderby", new BsonDocument("sort", 1) },
                { "$showDiskLoc", true },
                { "$snapshot", true }
            };

            var result = subject.CreateFindCommandOperation();

            result.Hint.Should().Be(subject.Modifiers["$hint"]);
            result.Max.Should().Be(subject.Modifiers["$max"].AsBsonDocument);
            result.MaxScan.Should().Be(subject.Modifiers["$maxScan"].AsInt32);
            result.MaxTime.Should().Be(TimeSpan.FromMilliseconds(subject.Modifiers["$maxTimeMS"].AsInt32));
            result.Min.Should().Be(subject.Modifiers["$min"].AsBsonDocument);
            result.ReadConcern.Should().Be(subject.ReadConcern);
            result.ShowRecordId.Should().Be(subject.Modifiers["$showDiskLoc"].AsBoolean);
            result.Snapshot.Should().Be(subject.Modifiers["$snapshot"].AsBoolean);
            result.Sort.Should().Be(subject.Modifiers["$orderby"].AsBsonDocument);
        }

        public void CreateFindOpcodeOperation_should_return_expected_result()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.AllowPartialResults = true;
            subject.BatchSize = 1;
            subject.Comment = "comment";
            subject.CursorType = CursorType.Tailable;
            subject.Filter = new BsonDocument("filter", 1);
            subject.FirstBatchSize = 2;
            subject.Hint = "x_1";
            subject.Limit = 3;
            subject.Max = new BsonDocument("max", 1);
            subject.MaxScan = 4;
            subject.MaxTime = TimeSpan.FromSeconds(1);
            subject.Min = new BsonDocument("min", 1);
            subject.NoCursorTimeout = true;
            subject.OplogReplay = true;
            subject.Projection = new BsonDocument("projection", 1);
            subject.ReadConcern = ReadConcern.Local;
            subject.ReturnKey = true;
            subject.ShowRecordId = true;
            subject.SingleBatch = false;
            subject.Skip = 6;
            subject.Snapshot = true;
            subject.Sort = new BsonDocument("sort", 1);

            var result = subject.CreateFindOpcodeOperation();

            result.AllowPartialResults.Should().Be(subject.AllowPartialResults);
            result.BatchSize.Should().Be(subject.BatchSize);
            result.CollectionNamespace.Should().Be(subject.CollectionNamespace);
            result.Comment.Should().Be(subject.Comment);
            result.CursorType.Should().Be(subject.CursorType);
            result.Filter.Should().Be(subject.Filter);
            result.FirstBatchSize.Should().Be(subject.FirstBatchSize);
            result.Hint.Should().Be(subject.Hint);
            result.Limit.Should().Be(subject.Limit);
            result.Max.Should().Be(subject.Max);
            result.MaxScan.Should().Be(subject.MaxScan);
            result.MaxTime.Should().Be(subject.MaxTime);
            result.MessageEncoderSettings.Should().BeSameAs(subject.MessageEncoderSettings);
            result.Min.Should().Be(subject.Min);
            result.Modifiers.Should().Be(subject.Modifiers);
            result.NoCursorTimeout.Should().Be(subject.NoCursorTimeout);
            result.OplogReplay.Should().Be(subject.OplogReplay);
            result.Projection.Should().Be(subject.Projection);
            result.ResultSerializer.Should().Be(subject.ResultSerializer);
            result.ShowRecordId.Should().Be(subject.ShowRecordId);
            result.Skip.Should().Be(subject.Skip);
            result.Snapshot.Should().Be(subject.Snapshot);
            result.Sort.Should().Be(subject.Sort);
        }

        public void CreateFindOpcodeOperation_should_return_expected_result_when_singleBatch_is_true()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Limit = 1;
            subject.SingleBatch = true;

            var result = subject.CreateFindOpcodeOperation();

            result.Limit.Should().Be(-subject.Limit);
        }

        [Test]
        public void CursorType_get_and_set_should_work(
            [Values(CursorType.NonTailable, CursorType.Tailable)]
            CursorType value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CursorType = value;
            var result = subject.CursorType;

            result.Should().Be(value);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_should_find_all_the_documents_matching_the_query(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(5);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_should_find_all_the_documents_matching_the_query_when_split_across_batches(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                BatchSize = 2
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(5);
        }

        [Test]
        [RequiresServer("EnsureTestData")]
        public void Execute_should_find_documents_matching_options(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Comment = "funny",
                Filter = BsonDocument.Parse("{ y : 1 }"),
                Limit = 4,
                MaxTime = TimeSpan.FromSeconds(20),
                Projection = BsonDocument.Parse("{ y : 1 }"),
                Skip = 1,
                Sort = BsonDocument.Parse("{ _id : -1 }")
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(1);
        }

        [Test]
        [RequiresServer("EnsureTestData", VersionLessThan = "3.1.0")]
        public void Execute_should_raise_an_error_when_an_unsupported_read_concern_is_specified(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                ReadConcern = ReadConcern.Majority
            };

            Action act = () => ExecuteOperation(subject, async);
            act.ShouldThrow<MongoClientException>();
        }

        [Test]
        public void ExecuteAsync_should_throw_when_binding_is_null()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Func<Task> action = () => subject.ExecuteAsync(null, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Test]
        public void Filter_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().Be(value);
        }

        [Test]
        public void FirstBatchSize_get_and_set_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.FirstBatchSize = value;
            var result = subject.FirstBatchSize;

            result.Should().Be(value);
        }

        [Test]
        public void FirstBatchSize_set_should_throw_when_value_is_invalid(
            [Values(-1)]
            int value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Action action = () => subject.FirstBatchSize = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void Hint_get_and_set_should_work(
            [Values(null, "{ value : \"b_1\" }", "{ value : { b : 1 } }")]
            string json)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json)["value"];

            subject.Hint = value;
            var result = subject.Hint;

            result.Should().Be(value);
        }

        [Test]
        public void Limit_get_and_set_should_work(
            [Values(-2, -1, 0, 1, 2)]
            int? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Test]
        public void Max_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Max = value;
            var result = subject.Max;

            result.Should().Be(value);
        }

        [Test]
        public void MaxScan_get_and_set_should_work(
            [Values(null, 1)]
            int? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.MaxScan = value;
            var result = subject.MaxScan;

            result.Should().Be(value);
        }

        [Test]
        public void MaxAwaitTime_get_and_set_should_work(
            [Values(null, 1)]
            int? seconds)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = seconds == null ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds.Value);

            subject.MaxAwaitTime = value;
            var result = subject.MaxAwaitTime;

            result.Should().Be(value);
        }

        [Test]
        public void MaxTime_get_and_set_should_work(
            [Values(null, 1)]
            int? seconds)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = seconds == null ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds.Value);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Test]
        public void MessageEncoderSettings_get_should_return_expected_result(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.Standard)]
            GuidRepresentation guidRepresentation)
        {
            var messageEncoderSettings = new MessageEncoderSettings { { "GuidRepresentation", guidRepresentation } };
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeEquivalentTo(messageEncoderSettings);
        }

        [Test]
        public void Min_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Min = value;
            var result = subject.Min;

            result.Should().Be(value);
        }

        [Test]
        public void Modifiers_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Modifiers = value;
            var result = subject.Modifiers;

            result.Should().Be(value);
        }

        [Test]
        public void NoCursorTimeout_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.NoCursorTimeout = value;
            var result = subject.NoCursorTimeout;

            result.Should().Be(value);
        }

        [Test]
        public void OplogReplay_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.OplogReplay = value;
            var result = subject.OplogReplay;

            result.Should().Be(value);
        }

        [Test]
        public void Projection_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 1 }")]
            string json)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Projection = value;
            var result = subject.Projection;

            result.Should().Be(value);
        }

        [Test]
        public void ReadConcern_get_and_set_should_work()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ReadConcern = ReadConcern.Local;
            var result = subject.ReadConcern;

            result.Should().Be(ReadConcern.Local);
        }

        [Test]
        public void ResultSerializer_get_should_return_expected_result()
        {
            var resultSerializer = new BsonDocumentSerializer();
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, resultSerializer, _messageEncoderSettings);

            var result = subject.ResultSerializer;

            result.Should().Be(resultSerializer);
        }

        [Test]
        public void ReturnKey_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ReturnKey = value;
            var result = subject.ReturnKey;

            result.Should().Be(value);
        }

        [Test]
        public void ShowRecordId_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ShowRecordId = value;
            var result = subject.ShowRecordId;

            result.Should().Be(value);
        }

        [Test]
        public void SingleBatch_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.SingleBatch = value;
            var result = subject.SingleBatch;

            result.Should().Be(value);
        }

        [Test]
        public void Skip_get_and_set_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Skip = value;
            var result = subject.Skip;

            result.Should().Be(value);
        }

        [Test]
        public void Skip_set_should_throw_when_value_is_invalid(
            [Values(-1)]
            int value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Action action = () => subject.Skip = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void Snapshot_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Snapshot = value;
            var result = subject.Snapshot;

            result.Should().Be(value);
        }

        [Test]
        public void Sort_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : -1 }")]
            string json)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Sort = value;
            var result = subject.Sort;

            result.Should().Be(value);
        }

        // private methods
        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(
                    new BsonDocument { { "_id", 1 }, { "y", 1 } },
                    new BsonDocument { { "_id", 2 }, { "y", 1 } },
                    new BsonDocument { { "_id", 3 }, { "y", 2 } },
                    new BsonDocument { { "_id", 4 }, { "y", 2 } },
                    new BsonDocument { { "_id", 5 }, { "y", 3 } });
            });
        }
    }
}
