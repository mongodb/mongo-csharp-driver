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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class MapReduceOperationTests : OperationTestBase
    {
        // fields
        private readonly BsonJavaScript _mapFunction;
        private readonly BsonJavaScript _reduceFunction;
        private readonly IBsonSerializer<BsonDocument> _resultSerializer;

        // constructors
        public MapReduceOperationTests()
        {
            _mapFunction = "function() { emit(this.x, this.v); }";
            _reduceFunction = "function(key, values) { var sum = 0; for (var i = 0; i < values.length; i++) { sum += values[i]; }; return sum; }";
            _resultSerializer = BsonDocumentSerializer.Instance;
        }

        // test methods
        [Fact]
        public void constructor_should_initialize_instance()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MapFunction.Should().BeSameAs(_mapFunction);
            subject.ReduceFunction.Should().BeSameAs(_reduceFunction);
            subject.ResultSerializer.Should().BeSameAs(_resultSerializer);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.Filter.Should().BeNull();
            subject.FinalizeFunction.Should().BeNull();
#pragma warning disable 618
            subject.JavaScriptMode.Should().NotHaveValue();
#pragma warning restore 618
            subject.Limit.Should().NotHaveValue();
            subject.MaxTime.Should().NotHaveValue();
            subject.ReadConcern.Should().BeSameAs(ReadConcern.Default);
            subject.Scope.Should().BeNull();
            subject.Sort.Should().BeNull();
            subject.Verbose.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_should_throw_when_resultSerializer_is_null()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var exception = Record.Exception(() => new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, null, _messageEncoderSettings));
#pragma warning restore CS0618 // Type or member is obsolete

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("resultSerializer");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_get_and_set_should_work(
            [Values(ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel level)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete
            var value = new ReadConcern(level);

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().Be(value);
        }

        [Fact]
        public void ReadConcern_set_should_throw_when_value_is_null()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            var exception = Record.Exception(() => subject.ReadConcern = null);

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("value");
        }

        [Fact]
        public void ResultSerializer_should_get_value()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            var result = subject.ResultSerializer;

            result.Should().BeSameAs(_resultSerializer);
        }

        [Fact]
        public void CreateOutputOptions_should_return_expected_result()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete
            var subjectReflector = new Reflector(subject);

            var result = subjectReflector.CreateOutputOptions();

            result.Should().Be("{ inline : 1 }");
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Collation_is_set(
            [Values(false, true)]
            bool caseSensitive,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary);
            var filter = BsonDocument.Parse("{ y : 'a' }");
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Collation = collation,
                Filter = filter
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            BsonDocument[] expectedResults;
            if (caseSensitive)
            {
                expectedResults = new[]
                {
                    BsonDocument.Parse("{ _id : 1, value : 1 }"),
                    BsonDocument.Parse("{ _id : 2, value : 4 }")
                };
            }
            else
            {
                expectedResults = new[]
                {
                    BsonDocument.Parse("{ _id : 1, value : 3 }"),
                    BsonDocument.Parse("{ _id : 2, value : 4 }")
                };
            }
            results.Should().BeEquivalentTo(expectedResults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Filter_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var filter = BsonDocument.Parse("{ y : 'a' }");
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Filter = filter
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 1, value : 1 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_FinalizeFunction_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var finalizeFunction = new BsonJavaScript("function(key, reduced) { return -reduced; }");
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                FinalizeFunction = finalizeFunction
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 1, value : -3 }"),
                BsonDocument.Parse("{ _id : 2, value : -4 }"));
        }

        // TODO: figure out why test fails when JavaScriptMode = true (server bug?)

        //[Theory]
        //[ParameterAttributeData]
        //public void Execute_should_return_expected_results_when_JavaScriptMode_is_set(
        //    [Values(null, false, true)]
        //    bool? javaScriptMode,
        //    [Values(false, true)]
        //    bool async)
        //{
        //    RequireServer.Check();
        //    EnsureTestData();
        //    var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
        //    {
        //        JavaScriptMode = javaScriptMode
        //    };

        //    var cursor = ExecuteOperation(subject, async);
        //    var results = ReadCursorToEnd(cursor, async);

        //    // the results are the same either way, but at least we're smoke testing JavaScriptMode
        //    results.Should().Equal(
        //        BsonDocument.Parse("{ _id : 1, value : 3 }"),
        //        BsonDocument.Parse("{ _id : 2, value : 4 }"));
        //}

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Limit_is_set(
            [Values(1, 2)]
            long limit,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Limit = limit
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            var expectedResults = new[]
            {
                new BsonDocument { { "_id", 1 }, { "value", limit == 1 ? 1 : 3 } }
            };
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_MaxTime_is_set(
            [Values(null, 1000)]
            int? seconds,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var maxTime = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                MaxTime = maxTime
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            // results should be the same whether MaxTime was used or not
            results.Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Local)] // only use values that are valid on StandAlone servers
            ReadConcernLevel? readConcernLevel,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var readConcern = new ReadConcern(readConcernLevel);
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                ReadConcern = readConcern
            };

            // results should be the same whether ReadConcern was used or not
            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_ResultSerializer_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var resultSerializer = new ElementDeserializer<double>("value", new DoubleSerializer());
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<double>(_collectionNamespace, _mapFunction, _reduceFunction, resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Sort();
            results.Should().Equal(3, 4);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Scope_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var finalizeFunction = new BsonJavaScript("function(key, reduced) { return reduced + zeroFromScope; }");
            var scope = new BsonDocument("zeroFromScope", 0);
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                FinalizeFunction = finalizeFunction,
                Scope = scope
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Sort_is_set(
            [Values(1, -1)]
            int direction,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var sort = new BsonDocument("_id", direction);
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Limit = 2,
                Sort = sort
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            BsonDocument[] expectedResults;
            if (direction == 1)
            {
                expectedResults = new[]
                {
                    BsonDocument.Parse("{ _id : 1, value : 3 }")
                };
            }
            else
            {
                expectedResults = new[]
                {
                    BsonDocument.Parse("{ _id : 1, value : 2 }"),
                    BsonDocument.Parse("{ _id : 2, value : 4 }")
                };
            }
            results.Should().BeEquivalentTo(expectedResults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            var exception = Record.Exception(() => ExecuteOperation(subject, (IReadBinding)null, async));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("binding");
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_maxTime_is_exceeded(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.MaxTime = TimeSpan.FromSeconds(9001);

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureTestData();
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            VerifySessionIdWasSentWhenSupported(subject, "mapReduce", async);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var readConcern = level.HasValue ? new ReadConcern(level.Value) : ReadConcern.Default;
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                ReadConcern = readConcern
            };
            var session = OperationTestHelper.CreateSession();
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "mapReduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("inline", 1) },
                { "readConcern", () => readConcern.ToBsonDocument(), !readConcern.IsServerDefault }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_using_causal_consistency(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var readConcern = level.HasValue ? new ReadConcern(level.Value) : ReadConcern.Default;
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                ReadConcern = readConcern
            };
            var session = OperationTestHelper.CreateSession(isCausallyConsistent: true, operationTime: new BsonTimestamp(100));
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(supportsSessions: true);

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedReadConcernDocument = readConcern.ToBsonDocument();
            expectedReadConcernDocument["afterClusterTime"] = new BsonTimestamp(100);

            var expectedResult = new BsonDocument
            {
                { "mapReduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("inline", 1) },
                { "readConcern", expectedReadConcernDocument }
            };
            result.Should().Be(expectedResult);
        }

        // helper methods
        private void EnsureTestData()
        {
            DropCollection();
            Insert(
                new BsonDocument { { "_id", 1 }, { "x", 1 }, { "v", 1 }, { "y", "a" } },
                new BsonDocument { { "_id", 2 }, { "x", 1 }, { "v", 2 }, { "y", "A" } },
                new BsonDocument { { "_id", 3 }, { "x", 2 }, { "v", 4 }, { "y", "a" } });
        }

        // nested types
        private class Reflector
        {
            // fields
#pragma warning disable CS0618 // Type or member is obsolete
            private readonly MapReduceOperation<BsonDocument> _instance;

            // constructor
            public Reflector(MapReduceOperation<BsonDocument> instance)
            {
                _instance = instance;
            }

            // methods
            public BsonDocument CreateOutputOptions()
            {
                var method = typeof(MapReduceOperation<BsonDocument>).GetMethod("CreateOutputOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                return (BsonDocument)method.Invoke(_instance, new object[0]);
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
