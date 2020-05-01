/* Copyright 2015-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class FindOpcodeOperationTests : OperationTestBase
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void AllowPartialResults_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.AllowPartialResults = value;
            var result = subject.AllowPartialResults;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void BatchSize_get_and_set_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.BatchSize = value;
            var result = subject.BatchSize;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void BatchSize_set_should_throw_when_value_is_invalid(
            [Values(-1)]
            int value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Action action = () => subject.BatchSize = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void CollectionNamespace_get_should_return_expected_result(
            [Values("a", "b")]
            string collectionName)
        {
            var databaseNamespace = new DatabaseNamespace("test");
            var collectionNamespace = new CollectionNamespace(databaseNamespace, collectionName);
            var subject = new FindOpcodeOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().Be(collectionNamespace);
        }

        [Theory]
        [ParameterAttributeData]
        public void Comment_get_and_set_should_work(
            [Values(null, "a", "b")]
            string value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Comment = value;
            var result = subject.Comment;

            result.Should().Be(value);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.ResultSerializer.Should().Be(BsonDocumentSerializer.Instance);
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
#pragma warning disable 618
            subject.MaxScan.Should().NotHaveValue();
#pragma warning restore
            subject.MaxTime.Should().NotHaveValue();
            subject.Modifiers.Should().BeNull();
            subject.Min.Should().BeNull();
            subject.NoCursorTimeout.Should().NotHaveValue();
#pragma warning disable 618
            subject.OplogReplay.Should().NotHaveValue();
#pragma warning restore 618
            subject.Projection.Should().BeNull();
            subject.ShowRecordId.Should().NotHaveValue();
            subject.Skip.Should().NotHaveValue();
#pragma warning disable 618
            subject.Snapshot.Should().NotHaveValue();
#pragma warning restore
            subject.Sort.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_collection_namespace_is_null()
        {
            Action action = () => new FindOpcodeOperation<BsonDocument>(null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action action = () => new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void constructor_should_throw_when_result_serializer_is_null()
        {
            Action action = () => new FindOpcodeOperation<BsonDocument>(_collectionNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("resultSerializer");
        }

        [Theory]
        [InlineData(-10000, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(9999, 1)]
        [InlineData(10000, 1)]
        [InlineData(10001, 2)]
        public void CreateWrappedQuery_should_create_the_correct_query_when_not_connected_to_a_shard_router(long maxTimeTicks, int expectedMaxTimeMS)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Comment = "funny",
                Filter = BsonDocument.Parse("{x: 1}"),
                MaxTime = TimeSpan.FromTicks(maxTimeTicks),
                Modifiers = BsonDocument.Parse("{$comment: \"notfunny\", $snapshot: true}"),
                Projection = BsonDocument.Parse("{y: 1}"),
                Sort = BsonDocument.Parse("{a: 1}")
            };

            var expectedResult = new BsonDocument
            {
                { "$query", BsonDocument.Parse("{x: 1}") },
                { "$orderby", BsonDocument.Parse("{a: 1}") },
                { "$comment", "funny" },
                { "$maxTimeMS", expectedMaxTimeMS },
                { "$snapshot", true }
            };


            var result = subject.CreateWrappedQuery(ServerType.ReplicaSetArbiter, ReadPreference.Secondary, out var slaveOk);

            result.Should().Be(expectedResult);
            result["$maxTimeMS"].BsonType.Should().Be(BsonType.Int32);
            slaveOk.Should().BeTrue();
        }

        [Theory]
        [InlineData(-10000, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(9999, 1)]
        [InlineData(10000, 1)]
        [InlineData(10001, 2)]
        public void CreateWrappedQuery_should_create_the_correct_query_when_connected_to_a_shard_router(long maxTimeTicks, int expectedMaxTimeMS)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Comment = "funny",
                Filter = BsonDocument.Parse("{x: 1}"),
                MaxTime = TimeSpan.FromTicks(maxTimeTicks),
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
                { "$maxTimeMS", expectedMaxTimeMS },
                { "$snapshot", true }
            };

            var result = subject.CreateWrappedQuery(ServerType.ShardRouter, ReadPreference.Secondary, out var slaveOk);

            result.Should().Be(expectedResult);
            result["$maxTimeMS"].BsonType.Should().Be(BsonType.Int32);
            slaveOk.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void CursorType_get_and_set_should_work(
            [Values(CursorType.NonTailable, CursorType.Tailable)]
            CursorType value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CursorType = value;
            var result = subject.CursorType;

            result.Should().Be(value);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_maxTime_is_exceeded(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.MaxTime = TimeSpan.FromSeconds(9001);

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
        }

        [SkippableFact]
        public async Task ExecuteAsync_should_find_all_the_documents_matching_the_query()
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var cursor = await ExecuteOperationAsync(subject);
            var result = await ReadCursorToEndAsync(cursor);

            result.Should().HaveCount(5);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public async Task ExecuteAsync_should_find_all_the_documents_matching_the_query_when_limit_is_used(
            [Values(1, 5, 6, 12)] int limit)
        {
            RequireServer.Check().VersionLessThan("3.2.0");
            var collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestMethod(
                className: GetType().Name,
                methodName: nameof(ExecuteAsync_should_find_all_the_documents_matching_the_query_when_limit_is_used));
            for (var id = 1; id <= limit + 1; id++)
            {
                var document = new BsonDocument { { "id", id }, { "filler", new string('x', 1000000) } }; // about 1MB big
                var requests = new[] { new InsertRequest(document) };
                var insertOperation = new BulkMixedWriteOperation(collectionNamespace, requests, new MessageEncoderSettings());
                ExecuteOperation(insertOperation);
            }
            var subject = new FindOpcodeOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Limit = limit
            };

            using (var cursor = await ExecuteOperationAsync(subject))
            {
                var result = await ReadCursorToEndAsync(cursor);

                result.Should().HaveCount(limit);
            }
        }

        [SkippableFact]
        public async Task ExecuteAsync_should_find_all_the_documents_matching_the_query_when_split_across_batches()
        {
            RequireServer.Check().VersionLessThan("3.5.0"); // TODO: should work against 3.6.0 but doesn't in Evergreen (works locally)
            EnsureTestData();
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                BatchSize = 2
            };

            using (var cursor = await ExecuteOperationAsync(subject))
            {
                var result = await ReadCursorToEndAsync(cursor);

                result.Should().HaveCount(5);
            }
        }

        [SkippableFact]
        public async Task ExecuteAsync_should_find_documents_matching_options()
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Comment = "funny",
                Filter = BsonDocument.Parse("{ y : 1 }"),
                Limit = 4,
                MaxTime = TimeSpan.FromSeconds(20),
                Projection = BsonDocument.Parse("{ y : 1 }"),
                Skip = 1,
                Sort = BsonDocument.Parse("{ _id : -1 }")
            };

            var cursor = await ExecuteOperationAsync(subject);
            var result = await ReadCursorToEndAsync(cursor);

            result.Should().HaveCount(1);
        }

        [Fact]
        public void ExecuteAsync_should_throw_when_binding_is_null()
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Func<Task> action = () => subject.ExecuteAsync(binding: null, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void FirstBatchSize_get_and_set_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.FirstBatchSize = value;
            var result = subject.FirstBatchSize;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void FirstBatchSize_set_should_throw_when_value_is_invalid(
            [Values(-1)]
            int value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Action action = () => subject.FirstBatchSize = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Hint_get_and_set_should_work(
            [Values(null, "{ value : \"b_1\" }", "{ value : { b : 1 } }")]
            string json)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json)["value"];

            subject.Hint = value;
            var result = subject.Hint;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Limit_get_and_set_should_work(
            [Values(-2, -1, 0, 1, 2)]
            int? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Max = value;
            var result = subject.Max;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxScan_get_and_set_should_work(
            [Values(null, 1)]
            int? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

#pragma warning disable 618
            subject.MaxScan = value;
            var result = subject.MaxScan;
#pragma warning restore

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(-10000, 0, 1, 10000, 99999)] long maxTimeTicks)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [Values(-10001, -9999, -1)] long maxTimeTicks)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void MessageEncoderSettings_get_should_return_expected_result(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.Standard)]
            GuidRepresentation guidRepresentation)
        {
            var messageEncoderSettings = new MessageEncoderSettings { { "GuidRepresentation", guidRepresentation } };
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeEquivalentTo(messageEncoderSettings);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Min = value;
            var result = subject.Min;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Modifiers_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Modifiers = value;
            var result = subject.Modifiers;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void NoCursorTimeout_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.NoCursorTimeout = value;
            var result = subject.NoCursorTimeout;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void OplogReplay_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

#pragma warning disable 618
            subject.OplogReplay = value;
            var result = subject.OplogReplay;
#pragma warning restore 618

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Projection_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 1 }")]
            string json)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Projection = value;
            var result = subject.Projection;

            result.Should().Be(value);
        }

        [Fact]
        public void ResultSerializer_get_should_return_expected_result()
        {
            var resultSerializer = new BsonDocumentSerializer();
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, resultSerializer, _messageEncoderSettings);

            var result = subject.ResultSerializer;

            result.Should().Be(resultSerializer);
        }

        [Theory]
        [ParameterAttributeData]
        public void ShowRecordId_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ShowRecordId = value;
            var result = subject.ShowRecordId;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Skip_get_and_set_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Skip = value;
            var result = subject.Skip;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Skip_set_should_throw_when_value_is_invalid(
            [Values(-1)]
            int value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Action action = () => subject.Skip = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Snapshot_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

#pragma warning disable 618
            subject.Snapshot = value;
            var result = subject.Snapshot;
#pragma warning restore

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sort_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : -1 }")]
            string json)
        {
            var subject = new FindOpcodeOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
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
