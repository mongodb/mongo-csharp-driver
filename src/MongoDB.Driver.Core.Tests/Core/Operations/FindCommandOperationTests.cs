/* Copyright 2015 MongoDB Inc.
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
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class FindCommandOperationTests : OperationTestBase
    {
        // public methods
        [Test]
        public void AllowPartialResults_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.AllowPartialResults = value;
            var result = subject.AllowPartialResults;

            result.Should().Be(value);
        }

        [Test]
        public void BatchSize_get_and_set_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.BatchSize = value;
            var result = subject.BatchSize;

            result.Should().Be(value);
        }

        [Test]
        public void BatchSize_set_should_throw_when_value_is_invalid(
            [Values(-1)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            var subject = new FindCommandOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().Be(collectionNamespace);
        }

        [Test]
        public void Comment_get_and_set_should_work(
            [Values(null, "a", "b")]
            string value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Comment = value;
            var result = subject.Comment;

            result.Should().Be(value);
        }


        [Test]
        public void constructor_should_initialize_instance()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            Action action = () => new FindCommandOperation<BsonDocument>(null, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Test]
        public void constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action action = () => new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageEncoderSettings");
        }

        [Test]
        public void constructor_should_throw_when_result_serializer_is_null()
        {
            Action action = () => new FindCommandOperation<BsonDocument>(_collectionNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("resultSerializer");
        }

        [Test]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}' }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_allowPartialResults_is_provided(
            [Values(false, true)]
            bool value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.AllowPartialResults = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription(type: ServerType.ShardRouter);

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', allowPartialResults : {(value ? "true" : "false")} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_comment_is_provided(
            [Values("a", "b")]
            string value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Comment = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', comment : '{value}' }}");
        }

        [TestCase(CursorType.Tailable, "")]
        [TestCase(CursorType.TailableAwait, ", awaitData : true")]
        public void CreateCommand_should_return_expected_result_when_cursor_is_tailableAwait(CursorType value, string awaitJson)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.CursorType = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', tailable : true{awaitJson} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_filter_is_provided(
            [Values("{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Filter = BsonDocument.Parse(json);
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', filter : {json} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_firstBatchSize_is_provided(
            [Values(0, 1)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.FirstBatchSize = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', batchSize : {value} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_hint_is_provided(
            [Values("{ value : 'b_1' }", "{ value : { b : 1 } }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Hint = BsonDocument.Parse(json)["value"];
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', hint : {subject.Hint.ToJson()} }}");
        }

        [TestCase(-1, ", limit : 1, singleBatch : true")]
        [TestCase(0, "")]
        [TestCase(1, ", limit : 1")]
        [TestCase(2, ", limit : 2")]
        public void CreateCommand_should_return_expected_result_when_limit_is_provided(int value, string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Limit = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}'{json} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_max_is_provided(
            [Values("{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Max = BsonDocument.Parse(json);
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', max : {json} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_maxScan_is_provided(
            [Values(1, 2)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.MaxScan = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', maxScan : {value} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_maxTime_is_provided(
            [Values(1, 2)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.MaxTime = TimeSpan.FromSeconds(value);
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', maxTimeMS : {value * 1000} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_min_is_provided(
            [Values("{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Min = BsonDocument.Parse(json);
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', min : {json} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_noCursorTimeout_is_provided(
            [Values(false, true)]
            bool value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.NoCursorTimeout = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', noCursorTimeout : {(value ? "true" : "false")} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_oplogReplay_is_provided(
            [Values(false, true)]
            bool value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.OplogReplay = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', oplogReplay : {(value ? "true" : "false")} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_projection_is_provided(
            [Values("{ a : 1 }", "{ b : 1 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Projection = BsonDocument.Parse(json);
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', projection : {json} }}");
        }

        [Test]
        [Category("ReadConcern")]
        public void CreateCommand_should_return_expected_result_when_readConcern_is_provided(
            [Values("{level: 'local'}", "{level: 'majority'}")]
            string readConcernJson)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.ReadConcern = ReadConcern.FromBsonDocument(BsonDocument.Parse(readConcernJson));
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', readConcern : {readConcernJson} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_readPreference_is_provided(
            [Values(ReadPreferenceMode.PrimaryPreferred, ReadPreferenceMode.Secondary)]
            ReadPreferenceMode value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var readPreference = new ReadPreference(value);
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription(type: ServerType.ShardRouter);

            var result = reflector.CreateCommand(serverDescription, readPreference);

            var mode = value.ToString();
            var camelCaseMode = char.ToLower(mode[0]) + mode.Substring(1);
            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', readPreference : {{ mode : '{camelCaseMode}' }} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_returnKey_is_provided(
            [Values(false, true)]
            bool value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.ReturnKey = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', returnKey : {(value ? "true" : "false")} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_showRecordId_is_provided(
            [Values(false, true)]
            bool value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.ShowRecordId = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', showRecordId : {(value ? "true" : "false")} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_singleBatch_is_provided(
            [Values(false, true)]
            bool value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.SingleBatch = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', singleBatch : {(value ? "true" : "false")} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_skip_is_provided(
            [Values(0, 1)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Skip = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', skip : {value} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_snapshot_is_provided(
            [Values(false, true)]
            bool value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Snapshot = value;
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', snapshot : {(value ? "true" : "false")} }}");
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_sort_is_provided(
            [Values("{ a : 1 }", "{ b : -1 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            subject.Sort = BsonDocument.Parse(json);
            var reflector = new Reflector(subject);
            var serverDescription = CreateServerDescription();

            var result = reflector.CreateCommand(serverDescription, null);

            result.Should().Be($"{{ find : '{_collectionNamespace.CollectionName}', sort : {json} }}");
        }

        [Test]
        public void CursorType_get_and_set_should_work(
            [Values(CursorType.NonTailable, CursorType.Tailable)]
            CursorType value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CursorType = value;
            var result = subject.CursorType;

            result.Should().Be(value);
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "3.1.5")]
        public async Task ExecuteAsync_should_find_all_the_documents_matching_the_query()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var cursor = await ExecuteOperationAsync(subject);
            var result = await ReadCursorToEndAsync(cursor);

            result.Should().HaveCount(5);
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "3.1.5")]
        public async Task ExecuteAsync_should_find_all_the_documents_matching_the_query_when_split_across_batches()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                BatchSize = 2
            };

            var cursor = await ExecuteOperationAsync(subject);
            var result = await ReadCursorToEndAsync(cursor);

            result.Should().HaveCount(5);
        }

        [Test]
        [RequiresServer("EnsureTestData", MinimumVersion = "3.1.5")]
        public async Task ExecuteAsync_should_find_documents_matching_options()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
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

        [Test]
        public void ExecuteAsync_should_throw_when_binding_is_null()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Func<Task> action = () => subject.ExecuteAsync(null, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Test]
        public void Filter_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.FirstBatchSize = value;
            var result = subject.FirstBatchSize;

            result.Should().Be(value);
        }

        [Test]
        public void FirstBatchSize_set_should_throw_when_value_is_invalid(
            [Values(-1)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Action action = () => subject.FirstBatchSize = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void Hint_get_and_set_should_work(
            [Values(null, "{ value : 'b_1' }", "{ value : { b : 1 } }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Test]
        public void Max_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.MaxScan = value;
            var result = subject.MaxScan;

            result.Should().Be(value);
        }

        [Test]
        public void MaxAwaitTime_get_and_set_should_work(
            [Values(null, 1)]
            int? seconds)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeEquivalentTo(messageEncoderSettings);
        }

        [Test]
        public void Min_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Min = value;
            var result = subject.Min;

            result.Should().Be(value);
        }

        [Test]
        public void NoCursorTimeout_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.NoCursorTimeout = value;
            var result = subject.NoCursorTimeout;

            result.Should().Be(value);
        }

        [Test]
        public void OplogReplay_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.OplogReplay = value;
            var result = subject.OplogReplay;

            result.Should().Be(value);
        }

        [Test]
        public void Projection_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 1 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Projection = value;
            var result = subject.Projection;

            result.Should().Be(value);
        }

        [Test]
        public void ReadConcern_get_and_set_should_work()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ReadConcern = ReadConcern.Majority;
            var result = subject.ReadConcern;

            result.Should().Be(ReadConcern.Majority);
        }

        [Test]
        public void ResultSerializer_get_should_return_expected_result()
        {
            var resultSerializer = new BsonDocumentSerializer();
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, resultSerializer, _messageEncoderSettings);

            var result = subject.ResultSerializer;

            result.Should().Be(resultSerializer);
        }

        [Test]
        public void ReturnKey_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ReturnKey = value;
            var result = subject.ReturnKey;

            result.Should().Be(value);
        }

        [Test]
        public void ShowRecordId_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ShowRecordId = value;
            var result = subject.ShowRecordId;

            result.Should().Be(value);
        }

        [Test]
        public void SingleBatch_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.SingleBatch = value;
            var result = subject.SingleBatch;

            result.Should().Be(value);
        }

        [Test]
        public void Skip_get_and_set_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Skip = value;
            var result = subject.Skip;

            result.Should().Be(value);
        }

        [Test]
        public void Skip_set_should_throw_when_value_is_invalid(
            [Values(-1)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            Action action = () => subject.Skip = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void Snapshot_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Snapshot = value;
            var result = subject.Snapshot;

            result.Should().Be(value);
        }

        [Test]
        public void Sort_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : -1 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Sort = value;
            var result = subject.Sort;

            result.Should().Be(value);
        }

        // private methods
        private ServerDescription CreateServerDescription(ServerType type = ServerType.Standalone)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            return new ServerDescription(serverId, endPoint, type: type, version: new SemanticVersion(3, 2, 0));
        }

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

        // nested types
        private class Reflector
        {
            // private fields
            private FindCommandOperation<BsonDocument> _instance;

            // constructors
            public Reflector(FindCommandOperation<BsonDocument> instance)
            {
                _instance = instance;
            }

            // public methods
            public BsonDocument CreateCommand(ServerDescription serverDescription, ReadPreference readPreference)
            {
                var methodInfo = _instance.GetType().GetMethod("CreateCommand", BindingFlags.NonPublic | BindingFlags.Instance);
                return (BsonDocument)methodInfo.Invoke(_instance, new object[] { serverDescription, readPreference });
            }
        }
    }
}
