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
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class FindOperationTests : OperationTestBase
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void AllowPartialResults_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.AllowPartialResults = value;
            var result = subject.AllowPartialResults;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void BatchSize_get_and_set_should_work(
            [Values(null, 0, 1, 2)]
            int? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.BatchSize = value; });

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
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

        [Theory]
        [ParameterAttributeData]
        public void Comment_get_and_set_should_work(
            [Values(null, "a", "b")]
            string value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Comment = value;
            var result = subject.Comment;

            result.Should().Be(value);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.ResultSerializer.Should().BeSameAs(BsonDocumentSerializer.Instance);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.AllowPartialResults.Should().NotHaveValue();
            subject.BatchSize.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.Comment.Should().BeNull();
            subject.CursorType.Should().Be(CursorType.NonTailable);
            subject.Filter.Should().BeNull();
            subject.FirstBatchSize.Should().NotHaveValue();
            subject.Hint.Should().BeNull();
            subject.Limit.Should().NotHaveValue();
            subject.Max.Should().BeNull();
            subject.MaxAwaitTime.Should().NotHaveValue();
            subject.MaxScan.Should().NotHaveValue();
            subject.MaxTime.Should().NotHaveValue();
            subject.Min.Should().BeNull();
            subject.Modifiers.Should().BeNull();
            subject.NoCursorTimeout.Should().NotHaveValue();
            subject.OplogReplay.Should().NotHaveValue();
            subject.Projection.Should().BeNull();
            subject.ReadConcern.Should().Be(ReadConcern.Default);
            subject.ReturnKey.Should().NotHaveValue();
            subject.ShowRecordId.Should().NotHaveValue();
            subject.SingleBatch.Should().NotHaveValue();
            subject.Skip.Should().NotHaveValue();
            subject.Snapshot.Should().NotHaveValue();
            subject.Sort.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => { new FindOperation<BsonDocument>(null, BsonDocumentSerializer.Instance, _messageEncoderSettings); });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => { new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, null); });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void constructor_should_throw_when_resultSerializer_is_null()
        {
            var exception = Record.Exception(() => { new FindOperation<BsonDocument>(_collectionNamespace, null, _messageEncoderSettings); });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("resultSerializer");
        }

        [Fact]
        public void CreateFindCommandOperation_should_return_expected_result()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                AllowPartialResults = true,
                BatchSize = 1,
                Collation = new Collation("en_US"),
                Comment = "comment",
                CursorType = CursorType.Tailable,
                Filter = new BsonDocument("filter", 1),
                FirstBatchSize = 2,
                Hint = "x_1",
                Limit = 3,
                Max = new BsonDocument("max", 1),
                MaxAwaitTime = TimeSpan.FromSeconds(2),
                MaxScan = 4,
                MaxTime = TimeSpan.FromSeconds(1),
                Min = new BsonDocument("min", 1),
                NoCursorTimeout = true,
                OplogReplay = true,
                Projection = new BsonDocument("projection", 1),
                ReadConcern = ReadConcern.Local,
                ReturnKey = true,
                ShowRecordId = true,
                SingleBatch = true,
                Skip = 6,
                Snapshot = true,
                Sort = new BsonDocument("sort", 1)
            };

            var result = subject.CreateFindCommandOperation();

            result.AllowPartialResults.Should().Be(subject.AllowPartialResults);
            result.BatchSize.Should().Be(subject.BatchSize);
            result.Collation.Should().BeSameAs(subject.Collation);
            result.CollectionNamespace.Should().BeSameAs(subject.CollectionNamespace);
            result.Comment.Should().BeSameAs(subject.Comment);
            result.CursorType.Should().Be(subject.CursorType);
            result.Filter.Should().BeSameAs(subject.Filter);
            result.FirstBatchSize.Should().Be(subject.FirstBatchSize);
            result.Hint.Should().BeSameAs(subject.Hint);
            result.Limit.Should().Be(subject.Limit);
            result.Max.Should().BeSameAs(subject.Max);
            result.MaxAwaitTime.Should().Be(subject.MaxAwaitTime);
            result.MaxScan.Should().Be(subject.MaxScan);
            result.MaxTime.Should().Be(subject.MaxTime);
            result.MessageEncoderSettings.Should().BeSameAs(subject.MessageEncoderSettings);
            result.Min.Should().BeSameAs(subject.Min);
            result.NoCursorTimeout.Should().Be(subject.NoCursorTimeout);
            result.OplogReplay.Should().Be(subject.OplogReplay);
            result.Projection.Should().BeSameAs(subject.Projection);
            result.ReadConcern.Should().BeSameAs(subject.ReadConcern);
            result.ResultSerializer.Should().BeSameAs(subject.ResultSerializer);
            result.ReturnKey.Should().Be(subject.ReturnKey);
            result.ShowRecordId.Should().Be(subject.ShowRecordId);
            result.SingleBatch.Should().Be(subject.SingleBatch);
            result.Skip.Should().Be(subject.Skip);
            result.Snapshot.Should().Be(subject.Snapshot);
            result.Sort.Should().BeSameAs(subject.Sort);
        }

        [Fact]
        public void CreateFindCommandOperation_should_return_expected_result_when_modifiers_are_provided()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Modifiers = new BsonDocument
                {
                    { "$comment", "comment" },
                    { "$hint", "x_1" },
                    { "$max", new BsonDocument("max", 1) },
                    { "$maxScan", 1 },
                    { "$maxTimeMS", 2000 },
                    { "$min", new BsonDocument("min", 1) },
                    { "$orderby", new BsonDocument("sort", 1) },
                    { "$returnKey", true },
                    { "$showDiskLoc", true },
                    { "$snapshot", true }
                }
            };

            var result = subject.CreateFindCommandOperation();

            result.Comment.Should().Be(subject.Modifiers["$comment"].AsString);
            result.Hint.Should().Be(subject.Modifiers["$hint"]);
            result.Max.Should().Be(subject.Modifiers["$max"].AsBsonDocument);
            result.MaxScan.Should().Be(subject.Modifiers["$maxScan"].AsInt32);
            result.MaxTime.Should().Be(TimeSpan.FromMilliseconds(subject.Modifiers["$maxTimeMS"].AsInt32));
            result.Min.Should().Be(subject.Modifiers["$min"].AsBsonDocument);
            result.ReturnKey.Should().Be(subject.Modifiers["$returnKey"].ToBoolean());
            result.ShowRecordId.Should().Be(subject.Modifiers["$showDiskLoc"].AsBoolean);
            result.Snapshot.Should().Be(subject.Modifiers["$snapshot"].AsBoolean);
            result.Sort.Should().Be(subject.Modifiers["$orderby"].AsBsonDocument);
        }

        [Fact]
        public void CreateFindOpcodeOperation_should_return_expected_result()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                AllowPartialResults = true,
                BatchSize = 1,
                Comment = "comment",
                CursorType = CursorType.Tailable,
                Filter = new BsonDocument("filter", 1),
                FirstBatchSize = 2,
                Hint = "x_1",
                Limit = 3,
                Max = new BsonDocument("max", 1),
                MaxScan = 4,
                MaxTime = TimeSpan.FromSeconds(1),
                Min = new BsonDocument("min", 1),
                Modifiers = new BsonDocument("modifiers", 1),
                NoCursorTimeout = true,
                OplogReplay = true,
                Projection = new BsonDocument("projection", 1),
                ReturnKey = true,
                ShowRecordId = true,
                SingleBatch = false,
                Skip = 6,
                Snapshot = true,
                Sort = new BsonDocument("sort", 1)
            };

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

        [Fact]
        public void CreateFindOpcodeOperation_should_return_expected_result_when_singleBatch_is_true()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Limit = 1,
                SingleBatch = true
            };

            var result = subject.CreateFindOpcodeOperation();

            result.Limit.Should().Be(-subject.Limit);
        }

        [Fact]
        public void CreateFindOpcodeOperation_should_throw_when_Collation_is_set()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => { subject.CreateFindOpcodeOperation(); });

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void CreateFindOpcodeOperation_should_throw_when_ReadConcern_is_not_server_default()
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Local)
            };

            var exception = Record.Exception(() => { subject.CreateFindOpcodeOperation(); });

            exception.Should().BeOfType<MongoClientException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CursorType_get_and_set_should_work(
            [Values(CursorType.NonTailable, CursorType.Tailable)]
            CursorType value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.CursorType = value;
            var result = subject.CursorType;

            result.Should().Be(value);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_find_all_the_documents_matching_the_query(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(5);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_find_all_the_documents_matching_the_query_when_read_preference_is_used(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var cursor = ExecuteOperation(subject, ReadPreference.PrimaryPreferred, async); // note: SecondaryPreferred doesn't test $readPreference because it is encoded as slaveOk = true
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(5);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_find_all_the_documents_matching_the_query_when_max_staleness_is_used(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.MaxStaleness);
            EnsureTestData();
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var readPreference = new ReadPreference(ReadPreferenceMode.SecondaryPreferred, maxStaleness: TimeSpan.FromSeconds(90));

            // the count could be short temporarily until replication catches up
            List<BsonDocument> result = null;
            SpinWait.SpinUntil(() =>
            {
                var cursor = ExecuteOperation(subject, readPreference, async);
                result = ReadCursorToEnd(cursor, async);
                return result.Count >= 5;
            },
            TimeSpan.FromSeconds(10));

            result.Should().HaveCount(5);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_find_all_the_documents_matching_the_query_when_split_across_batches(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                BatchSize = 2
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(5);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_find_documents_matching_options(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
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

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Collation_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.FindCommand, Feature.Collation);
            EnsureTestData();
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = new Collation("en_US", caseLevel: false, strength: CollationStrength.Primary),
                Filter = BsonDocument.Parse("{ x : 'd' }")
            };

            var cursor = ExecuteOperation(subject, async);

            var result = ReadCursorToEnd(cursor);
            result.Should().HaveCount(2);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set_but_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.Collation);
            EnsureTestData();
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => { ExecuteOperation(subject, async); });

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_ReadConcern_is_set_but_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.ReadConcern);
            EnsureTestData();
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                ReadConcern = ReadConcern.Majority
            };

            async = false;
            var exception = Record.Exception(() => { ExecuteOperation(subject, async); });

            exception.Should().BeOfType<MongoClientException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    subject.ExecuteAsync(null, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Execute(null, CancellationToken.None);
                }
            });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("binding");
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string valueString)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

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
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.FirstBatchSize = value; });

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Hint_get_and_set_should_work(
            [Values(null, "{ hint : \"b_1\" }", "{ hint : { b : 1 } }")]
            string valueString)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString)["hint"];

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
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string valueString)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

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
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.MaxScan = value;
            var result = subject.MaxScan;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
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

        [Theory]
        [ParameterAttributeData]
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

        [Theory]
        [ParameterAttributeData]
        public void MessageEncoderSettings_get_should_return_expected_result(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.Standard)]
            GuidRepresentation guidRepresentation)
        {
            var messageEncoderSettings = new MessageEncoderSettings { { "GuidRepresentation", guidRepresentation } };
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(messageEncoderSettings);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string valueString)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Min = value;
            var result = subject.Min;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Modifiers_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 2 }")]
            string valueString)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

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
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.OplogReplay = value;
            var result = subject.OplogReplay;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Projection_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : 1 }")]
            string valueString)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Projection = value;
            var result = subject.Projection;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_get_and_set_should_work(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = level.HasValue ? new ReadConcern(level.Value) : ReadConcern.Default;

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().Be(value);
        }

        [Fact]
        public void ResultSerializer_get_should_return_expected_result()
        {
            var resultSerializer = new BsonDocumentSerializer();
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, resultSerializer, _messageEncoderSettings);

            var result = subject.ResultSerializer;

            result.Should().BeSameAs(resultSerializer);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReturnKey_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ReturnKey = value;
            var result = subject.ReturnKey;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ShowRecordId_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.ShowRecordId = value;
            var result = subject.ShowRecordId;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void SingleBatch_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.SingleBatch = value;
            var result = subject.SingleBatch;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Skip_get_and_set_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.Skip = value; });

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Snapshot_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Snapshot = value;
            var result = subject.Snapshot;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sort_get_and_set_should_work(
            [Values(null, "{ a : 1 }", "{ b : -1 }")]
            string valueString)
        {
            var subject = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

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
                    new BsonDocument { { "_id", 1 }, { "x", "a" }, { "y", 1 } },
                    new BsonDocument { { "_id", 2 }, { "x", "b" }, { "y", 1 } },
                    new BsonDocument { { "_id", 3 }, { "x", "c" }, { "y", 2 } },
                    new BsonDocument { { "_id", 4 }, { "x", "d" }, { "y", 2 } },
                    new BsonDocument { { "_id", 5 }, { "x", "D" }, { "y", 3 } });
            });
        }
    }
}
