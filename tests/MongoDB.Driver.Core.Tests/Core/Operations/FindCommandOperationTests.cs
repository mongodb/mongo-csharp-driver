/* Copyright 2015-2016 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class FindCommandOperationTests : OperationTestBase
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void AllowPartialResults_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.BatchSize = value;
            var result = subject.BatchSize;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void BatchSize_set_should_throw_when_value_is_invalid(
            [Values(-2, -1)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() => subject.BatchSize = value);

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Comment_get_and_set_should_work(
            [Values(null, "a", "b")]
            string value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Comment = value;
            var result = subject.Comment;

            result.Should().BeSameAs(value);
        }


        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            subject.NoCursorTimeout.Should().NotHaveValue();
            subject.OplogReplay.Should().NotHaveValue();
            subject.Projection.Should().BeNull();
            subject.ReadConcern.Should().BeSameAs(ReadConcern.Default);
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
            var exception = Record.Exception(() => new FindCommandOperation<BsonDocument>(null, BsonDocumentSerializer.Instance, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void constructor_should_throw_when_resultSerializer_is_null()
        {
            var exception = Record.Exception(() => new FindCommandOperation<BsonDocument>(_collectionNamespace, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("resultSerializer");
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_AllowPartialResults_is_set(
            [Values(null, false, true)]
            bool? allowPartialResults,
            [Values(ServerType.Standalone, ServerType.ShardRouter)]
            ServerType serverType)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                AllowPartialResults = allowPartialResults
            };

            var result = subject.CreateCommand(null, serverType);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "allowPartialResults", () => allowPartialResults.Value, allowPartialResults.HasValue && serverType == ServerType.ShardRouter }
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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Comment_is_set(
            [Values(null, "a", "b")]
            string comment)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Comment = comment
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "comment", () => comment, comment != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_CursorType_is_Set(
            [Values(CursorType.NonTailable, CursorType.Tailable, CursorType.TailableAwait)]
            CursorType cursorType)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                CursorType = cursorType
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "tailable", true, cursorType == CursorType.Tailable || cursorType == CursorType.TailableAwait },
                { "awaitData", true, cursorType == CursorType.TailableAwait }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Filter_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string filterString)
        {
            var filter = filterString == null ? null : BsonDocument.Parse(filterString);
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Filter = filter
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "filter", filter, filter != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_FirstBatchSize_is_set(
            [Values(null, 0, 1)]
            int? firstBatchSize)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                FirstBatchSize = firstBatchSize
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "batchSize", () => firstBatchSize.Value, firstBatchSize.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Hint_is_set(
            [Values(null, "{ hint : 'x_1' }", "{ hint : { x : 1 } }")]
            string hintString)
        {
            var hint = hintString == null ? null : BsonDocument.Parse(hintString)["hint"];
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Hint = hint
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "hint", hint, hint != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Limit_is_set(
            [Values(null, -2, -1, 0, 1, 2)]
            int? limit,
            [Values(null, false, true)]
            bool? singleBatch)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Limit = limit,
                SingleBatch = singleBatch
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "limit", () => Math.Abs(limit.Value), limit.HasValue && limit.Value != 0 },
                { "singleBatch", () => limit < 0 || singleBatch.Value, limit < 0 || singleBatch.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Max_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string maxString)
        {
            var max = maxString == null ? null : BsonDocument.Parse(maxString);
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Max = max
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "max", max, max != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxScan_is_set(
            [Values(null, 1, 2)]
            int? maxScan)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                MaxScan = maxScan
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "maxScan", () => maxScan.Value, maxScan.HasValue }
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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "maxTimeMS", () => seconds.Value * 1000, seconds.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Min_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string minString)
        {
            var min = minString == null ? null : BsonDocument.Parse(minString);
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Min = min
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "min", min, min != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_NoCursorTimeout_is_set(
            [Values(null, false, true)]
            bool? noCursorTimeout)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                NoCursorTimeout = noCursorTimeout
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "noCursorTimeout", () => noCursorTimeout.Value, noCursorTimeout.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_OplogReplay_is_set(
            [Values(null, false, true)]
            bool? oplogReplay)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                OplogReplay = oplogReplay
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "oplogReplay", () => oplogReplay.Value, oplogReplay.HasValue }
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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Projection = projection
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "projection", projection, projection != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var readConcern = level.HasValue ? new ReadConcern(level.Value) : ReadConcern.Default;
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var result = subject.CreateCommand(Feature.ReadConcern.FirstSupportedVersion, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "readConcern", () => readConcern.ToBsonDocument(), readConcern != null && !readConcern.IsServerDefault }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReturnKey_is_set(
            [Values(null, false, true)]
            bool? returnKey)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                ReturnKey = returnKey
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "returnKey", () => returnKey.Value, returnKey.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ShowRecordId_is_set(
            [Values(null, false, true)]
            bool? showRecordId)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                ShowRecordId = showRecordId
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "showRecordId", () => showRecordId.Value, showRecordId.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Skip_is_set(
            [Values(null, 0, 1, 2)]
            int? skip)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Skip = skip
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "skip", () => skip.Value, skip.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Snapshot_is_set(
            [Values(null, false, true)]
            bool? snapshot)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Snapshot = snapshot
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "snapshot", () => snapshot.Value, snapshot.HasValue }
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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Sort = sort
            };

            var result = subject.CreateCommand(null, 0);

            var expectedResult = new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "sort", sort, sort != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set_but_not_supported()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.Collation.LastNotSupportedVersion, 0));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void CreateCommand_should_throw_when_ReadConcern_is_set_but_not_supported()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Local)
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.ReadConcern.LastNotSupportedVersion, 0));

            exception.Should().BeOfType<MongoClientException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CursorType_get_and_set_should_work(
            [Values(CursorType.NonTailable, CursorType.Tailable)]
            CursorType value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            RequireServer.Check().Supports(Feature.FindCommand);
            EnsureTestData();
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor);

            result.Should().HaveCount(5);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_find_all_the_documents_matching_the_query_when_split_across_batches(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.FindCommand);
            EnsureTestData();
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                BatchSize = 2
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor);

            result.Should().HaveCount(5);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_find_documents_matching_options(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.FindCommand);
            EnsureTestData();
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

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor);

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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
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
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() => ExecuteOperation(subject, (IReadBinding)null, async));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("binding");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set_and_not_suppported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.FindCommand).DoesNotSupport(Feature.Collation);
            EnsureTestData();
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = new Collation("en_US", caseLevel: false, strength: CollationStrength.Primary),
                Filter = BsonDocument.Parse("{ x : 'd' }")
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void FirstBatchSize_get_and_set_should_work(
            [Values(null, 0, 1, 2)]
            int? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.FirstBatchSize = value;
            var result = subject.FirstBatchSize;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void FirstBatchSize_set_should_throw_when_value_is_invalid(
            [Values(-2, -1)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.FirstBatchSize = value; });

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Hint_get_and_set_should_work(
            [Values(null, "{ hint : 'x_1' }", "{ hint : { x : 1 } }")]
            string valueString)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString)["hint"];

            subject.Hint = value;
            var result = subject.Hint;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Limit_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Max = value;
            var result = subject.Max;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxScan_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.MaxScan = value;
            var result = subject.MaxScan;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxScan_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            int? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.MaxScan = value; });

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxAwaitTime_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = seconds == null ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds.Value);

            subject.MaxAwaitTime = value;
            var result = subject.MaxAwaitTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = seconds == null ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds.Value);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Min = value;
            var result = subject.Min;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void NoCursorTimeout_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.OplogReplay = value;
            var result = subject.OplogReplay;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Projection_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ y : 1 }")]
            string json)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);

            subject.Projection = value;
            var result = subject.Projection;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_get_and_set_should_work(
            [Values(ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel level)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = new ReadConcern(level);

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().Be(value);
        }

        [Fact]
        public void ReadConcern_set_should_throw_when_value_is_null()
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.ReadConcern = null; });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReturnKey_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

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
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.SingleBatch = value;
            var result = subject.SingleBatch;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Skip_get_and_set_should_work(
            [Values(null, 0, 1, 2)]
            int? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Skip = value;
            var result = subject.Skip;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Skip_set_should_throw_when_value_is_invalid(
            [Values(-2, -1)]
            int value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            var exception = Record.Exception(() => subject.Skip = value);

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Snapshot_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            subject.Snapshot = value;
            var result = subject.Snapshot;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sort_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ y : 1 }")]
            string valueString)
        {
            var subject = new FindCommandOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Sort = value;
            var result = subject.Sort;

            result.Should().BeSameAs(value);
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
