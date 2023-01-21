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
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class AggregateToCollectionOperationTests : OperationTestBase
    {
        private static readonly BsonDocument[] __pipeline = new[]
        {
                BsonDocument.Parse("{ $match : { _id : 1 } }"),
                BsonDocument.Parse("{ $out : \"awesome\" }")
        };

        [Fact]
        public void Constructor_with_databaseNamespace_should_create_a_valid_instance()
        {
            var subject = new AggregateToCollectionOperation(_databaseNamespace, __pipeline, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeNull();
            subject.DatabaseNamespace.Should().BeSameAs(_databaseNamespace);
            subject.Pipeline.Should().Equal(__pipeline);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.AllowDiskUse.Should().NotHaveValue();
            subject.BypassDocumentValidation.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.MaxTime.Should().NotHaveValue();
            subject.WriteConcern.Should().BeNull();
        }

        [Fact]
        public void Constructor_with_collectionNamespace_should_create_a_valid_instance()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.DatabaseNamespace.Should().BeSameAs(_collectionNamespace.DatabaseNamespace);
            subject.Pipeline.Should().Equal(__pipeline);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.AllowDiskUse.Should().NotHaveValue();
            subject.BypassDocumentValidation.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.MaxTime.Should().NotHaveValue();
            subject.ReadConcern.Should().BeNull();
            subject.WriteConcern.Should().BeNull();
        }

        [Theory]
        [InlineData("{ $out : 'collection' }", "{ $out : 'collection' }")]
        [InlineData("{ $out : { db : 'database', coll : 'collection' } }", "{ $out : 'collection' }")]
        [InlineData("{ $out : { db : 'differentdatabase', coll : 'collection' } }", "{ $out : { db : 'differentdatabase', coll : 'collection' } }")]
        [InlineData("{ $out : { s3 : { } } }", "{ $out : { s3 : { } } }")]
        public void Constructor_should_simplify_out_stage_when_possible(string outStageJson, string expectedOutStageJson)
        {
            var databaseNamespace = new DatabaseNamespace("database");
            var pipeline = new[] { BsonDocument.Parse(outStageJson) };
            var messageEncoderSettings = new MessageEncoderSettings();

            var subject = new AggregateToCollectionOperation(databaseNamespace, pipeline, messageEncoderSettings);

            subject.Pipeline.Last().Should().Be(BsonDocument.Parse(expectedOutStageJson));
        }

        [Fact]
        public void Constructor_should_throw_when_databaseNamespace_is_null()
        {
            var exception = Record.Exception(() => new AggregateToCollectionOperation((DatabaseNamespace)null, __pipeline, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("databaseNamespace");
        }

        [Fact]
        public void Constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new AggregateToCollectionOperation((CollectionNamespace)null, __pipeline, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void Constructor_should_throw_when_pipeline_is_null()
        {
            var exception = Record.Exception(() => new AggregateToCollectionOperation(_collectionNamespace, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void Constructor_should_throw_when_pipeline_does_not_end_with_out()
        {
            var exception = Record.Exception(() => new AggregateToCollectionOperation(_collectionNamespace, Enumerable.Empty<BsonDocument>(), _messageEncoderSettings));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void Constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new AggregateToCollectionOperation(_collectionNamespace, __pipeline, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Theory]
        [ParameterAttributeData]
        public void AllowDiskUse_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            subject.AllowDiskUse = value;
            var result = subject.AllowDiskUse;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void BypassDocumentValidation_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            subject.BypassDocumentValidation = value;
            var result = subject.BypassDocumentValidation;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US")]
            string locale)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void Comment_get_and_set_should_work()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = (BsonValue)"test";

            subject.Comment = value;
            var result = subject.Comment;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void Hint_get_and_set_should_work()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = new BsonDocument("x", 1);

            subject.Hint = value;
            var result = subject.Hint;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void Let_get_and_set_should_work()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = new BsonDocument("x", "y");

            subject.Let = value;
            var result = subject.Let;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(-10000, 0, 1, 10000, 99999)] long maxTimeTicks)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
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
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_get_and_set_should_work([Values(ReadConcernLevel.Local, ReadConcernLevel.Majority)] ReadConcernLevel level)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = new ReadConcern(new Optional<ReadConcernLevel?>(level));

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void ReadPreference_get_and_set_should_work()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = new ReadPreference(ReadPreferenceMode.Primary);

            subject.ReadPreference = value;
            var result = subject.ReadPreference;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(1, 2)]
            int w)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var value = new WriteConcern(w);

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [InlineData(WireVersion.Server49, false)]
        [InlineData(WireVersion.Server50, true)]
        public void CanUseSecondary_should_return_expected_result(int maxWireVersion, bool expectedResult)
        {
            var subject = new AggregateToCollectionOperation.MayUseSecondary(ReadPreference.Secondary);
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);

            var serverDescription = new ServerDescription(serverId, endPoint, wireVersionRange: new Range<int>(0, maxWireVersion));

            var result = subject.CanUseSecondary(serverDescription);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_AllowDiskUse_is_set(
            [Values(null, false, true)]
            bool? allowDiskUse)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "allowDiskUse", () => allowDiskUse.Value, allowDiskUse != null },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_BypassDocumentValidation_is_set(
            [Values(null, false, true)]
            bool? bypassDocumentValidation)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                BypassDocumentValidation = bypassDocumentValidation
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "bypassDocumentValidation", () => bypassDocumentValidation.Value, bypassDocumentValidation != null },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Collation_is_set(
            [Values(null, "en_US")]
            string locale)
        {
            var collation = locale == null ? null : new Collation(locale);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Collation = collation
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "collation", () => collation.ToBsonDocument(), collation != null },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Comment_is_set(
            [Values(null, "test")]
            string comment)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Comment = comment,
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "cursor", new BsonDocument() },
                { "comment", () => comment, comment != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Hint_is_set(
            [Values(null, "{x: 1}")]
            string hintJson)
        {
            var hint = hintJson == null ? null : BsonDocument.Parse(hintJson);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Hint = hint
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "cursor", new BsonDocument() },
                { "hint", () => hint, hint != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Let_is_set(
            [Values(null, "{ y : 'z' }")]
            string letJson)
        {
            var let = letJson == null ? null : BsonDocument.Parse(letJson);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Let = let
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "cursor", new BsonDocument() },
                { "let", () => let, let != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(-10000, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(9999, 1)]
        [InlineData(10000, 1)]
        [InlineData(10001, 2)]
        public void CreateCommand_should_return_expected_result_when_MaxTime_is_set(long maxTimeTicks, int expectedMaxTimeMS)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                MaxTime = TimeSpan.FromTicks(maxTimeTicks)
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "maxTimeMS", expectedMaxTimeMS },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
            result["maxTimeMS"].BsonType.Should().Be(BsonType.Int32);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReadConcern_is_set(
            [Values(ReadConcernLevel.Majority)] ReadConcernLevel readConcernLevel,
            [Values(false, true)] bool withReadConcern)
        {
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);
            if (withReadConcern)
            {
                subject.ReadConcern = new ReadConcern(readConcernLevel);
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "readConcern", () => subject.ReadConcern.ToBsonDocument(), withReadConcern },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(__pipeline) },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null },
                { "cursor", new BsonDocument() }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values("$out", "$merge")] string lastStageName,
            [Values(false, true)] bool usingDifferentOutputDatabase,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            var pipeline = new List<BsonDocument> { BsonDocument.Parse("{ $match : { _id : 1 } }") };
            var inputDatabaseName = _databaseNamespace.DatabaseName;
            var inputCollectionName = _collectionNamespace.CollectionName;
            var outputDatabaseName = usingDifferentOutputDatabase ? $"{inputDatabaseName}-outputdatabase" : inputDatabaseName;
            var outputCollectionName = $"{inputCollectionName}-outputcollection";
            switch (lastStageName)
            {
                case "$out":
                    if (usingDifferentOutputDatabase)
                    {
                        RequireServer.Check().Supports(Feature.AggregateOutToDifferentDatabase);
                        pipeline.Add(BsonDocument.Parse($"{{ $out : {{ db : '{outputDatabaseName}', coll : '{outputCollectionName}' }} }}"));
                    }
                    else
                    {
                        pipeline.Add(BsonDocument.Parse($"{{ $out : '{outputCollectionName}' }}"));
                    }
                    break;

                case "$merge":
                    RequireServer.Check().Supports(Feature.AggregateMerge);
                    if (usingDifferentOutputDatabase)
                    {
                        pipeline.Add(BsonDocument.Parse($"{{ $merge : {{ into : {{ db : '{outputDatabaseName}', coll : '{outputCollectionName}' }} }} }}"));
                    }
                    else
                    {
                        pipeline.Add(BsonDocument.Parse($"{{ $merge : {{ into : '{outputDatabaseName}' }} }}"));
                    }
                    break;

                default:
                    throw new Exception($"Unexpected lastStageName: \"{lastStageName}\".");
            }
            EnsureTestData();
            if (usingDifferentOutputDatabase)
            {
                EnsureDatabaseExists(outputDatabaseName);
            }
            var subject = new AggregateToCollectionOperation(_collectionNamespace, pipeline, _messageEncoderSettings);

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(new DatabaseNamespace(outputDatabaseName), outputCollectionName), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_AllowDiskUse_is_set(
            [Values(null, false, true)]
            bool? allowDiskUse,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = allowDiskUse
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_BypassDocumentValidation_is_set(
            [Values(null, false, true)]
            bool? bypassDocumentValidation,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                BypassDocumentValidation = bypassDocumentValidation
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Collation_is_set(
            [Values(false, true)]
            bool caseSensitive,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var pipeline = new[]
            {
                BsonDocument.Parse("{ $match : { x : \"x\" } }"),
                BsonDocument.Parse("{ $out : \"awesome\" }")
            };
            var collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, pipeline, _messageEncoderSettings)
            {
                Collation = collation
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(caseSensitive ? 1 : 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_maxTime_is_exceeded(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var pipeline = new[]
            {
                BsonDocument.Parse("{ $match : { x : \"x\" } }"),
                BsonDocument.Parse("{ $out : \"awesome\" }")
            };
            var subject = new AggregateToCollectionOperation(_collectionNamespace, pipeline, _messageEncoderSettings)
            {
                MaxTime = TimeSpan.FromSeconds(9001)
            };

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Comment_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Comment = "test"
            };

            using (var profile = Profile(_collectionNamespace.DatabaseNamespace))
            {
                ExecuteOperation(subject, async);
                var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

                result.Should().NotBeNull();

                var profileEntries = profile.Find(new BsonDocument("command.aggregate", new BsonDocument("$exists", true)));
                profileEntries.Should().HaveCount(1);
                profileEntries[0]["command"]["comment"].Should().Be(subject.Comment);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Hint_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                Hint = "_id_"
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Let_is_set_with_match_expression(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateOptionsLet);
            EnsureTestData();
            var pipeline = new[]
            {
                BsonDocument.Parse("{ $match : { $expr : { $eq : [ '$x', '$$y'] } } }"),
                BsonDocument.Parse("{ $out : \"awesome\" }")
            };
            var subject = new AggregateToCollectionOperation(_collectionNamespace, pipeline, _messageEncoderSettings)
            {
                Let = new BsonDocument("y", "x")
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().BeEquivalentTo(new[]
            {
                new BsonDocument { { "_id", 1 }, { "x", "x" } }
            });
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Let_is_set_with_project(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateOptionsLet);
            EnsureTestData();
            var pipeline = new[]
            {
                BsonDocument.Parse("{ $project : { y : '$$z' } }"),
                BsonDocument.Parse("{ $out : \"awesome\" }")
            };
            var subject = new AggregateToCollectionOperation(_collectionNamespace, pipeline, _messageEncoderSettings)
            {
                Let = new BsonDocument("z", "x")
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().BeEquivalentTo(new[]
            {
                new BsonDocument { { "_id", 1 }, { "y", "x" } },
                new BsonDocument { { "_id", 2 }, { "y", "x" } }
            });
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_MaxTime_is_set(
            [Values(null, 1000)]
            int? milliseconds,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var maxTime = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value);
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            ExecuteOperation(subject, async);
            var result = ReadAllFromCollection(new CollectionNamespace(_databaseNamespace, "awesome"), async);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterType(ClusterType.ReplicaSet);
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new AggregateToCollectionOperation(_collectionNamespace, __pipeline, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "aggregate", async);
        }

        // private methods
        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(new BsonDocument { { "_id", 1 }, { "x", "x" } });
                Insert(new BsonDocument { { "_id", 2 }, { "x", "X" } });
            });
        }
    }
}
