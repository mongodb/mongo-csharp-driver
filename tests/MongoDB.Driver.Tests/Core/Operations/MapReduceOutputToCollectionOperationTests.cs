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
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class MapReduceOutputToCollectionOperationTests : OperationTestBase
    {
        // fields
        private readonly BsonJavaScript _mapFunction;
        private CollectionNamespace _outputCollectionNamespace;
        private readonly BsonJavaScript _reduceFunction;

        // constructors
        public MapReduceOutputToCollectionOperationTests()
        {
            _mapFunction = "function() { emit(this.x, this.v); }";
            _outputCollectionNamespace = new CollectionNamespace(_databaseNamespace, _collectionNamespace + "Output");
            _reduceFunction = "function(key, values) { var sum = 0; for (var i = 0; i < values.length; i++) { sum += values[i]; }; return sum; }";
        }

        // test methods
        [Fact]
        public void constructor_should_initialize_instance()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.OutputCollectionNamespace.Should().BeSameAs(_outputCollectionNamespace);
            subject.MapFunction.Should().BeSameAs(_mapFunction);
            subject.ReduceFunction.Should().BeSameAs(_reduceFunction);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.BypassDocumentValidation.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.Filter.Should().BeNull();
            subject.FinalizeFunction.Should().BeNull();
#pragma warning disable 618
            subject.JavaScriptMode.Should().NotHaveValue();
#pragma warning restore 618
            subject.Limit.Should().NotHaveValue();
            subject.MaxTime.Should().NotHaveValue();
#pragma warning disable 618
            subject.NonAtomicOutput.Should().NotHaveValue();
            subject.OutputMode.Should().Be(MapReduceOutputMode.Replace);
#pragma warning restore 618
            subject.Scope.Should().BeNull();
            subject.Sort.Should().BeNull();
            subject.Verbose.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_should_throw_when_outputCollectionNamespace_is_null()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var exception = Record.Exception(() => new MapReduceOutputToCollectionOperation(_collectionNamespace, null, _mapFunction, _reduceFunction, _messageEncoderSettings));
#pragma warning restore CS0618 // Type or member is obsolete

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("outputCollectionNamespace");
        }

        [Theory]
        [ParameterAttributeData]
        public void BypassDocumentValidation_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            subject.BypassDocumentValidation = value;
            var result = subject.BypassDocumentValidation;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_should_get_and_set_value(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void NonAtomicOutput_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable 618
            subject.NonAtomicOutput = value;
            var result = subject.NonAtomicOutput;
#pragma warning restore 618

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void OutputCollectionNamespace_get_and_set_should_work(
            [Values("a", "b")]
            string collectionName)
        {
            var outputCollectionNamespace = new CollectionNamespace(_databaseNamespace, collectionName);
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            var result = subject.OutputCollectionNamespace;

            result.Should().BeSameAs(outputCollectionNamespace);
        }

        [Theory]
        [ParameterAttributeData]
        public void OutputMode_get_and_set_should_work(
#pragma warning disable CS0618 // Type or member is obsolete
            [Values(MapReduceOutputMode.Merge, MapReduceOutputMode.Reduce)]
            MapReduceOutputMode value)
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.OutputMode = value;
            var result = subject.OutputMode;
#pragma warning restore CS0618 // Type or member is obsolete

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ShardedOutput_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
#pragma warning disable 618
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.ShardedOutput = value;
            var result = subject.ShardedOutput;
#pragma warning restore 618

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_BypassDocumentValidation_is_set(
            [Values(null, false, true)]
            bool? bypassDocumentValidation)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                BypassDocumentValidation = bypassDocumentValidation
            };
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "mapReduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument { {"replace", _outputCollectionNamespace.CollectionName }, { "db", _databaseNamespace.DatabaseName } } },
                { "bypassDocumentValidation", () => bypassDocumentValidation.Value, bypassDocumentValidation.HasValue }
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
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                WriteConcern = writeConcern
            };
            var connectionDescription = OperationTestHelper.CreateConnectionDescription();
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session, connectionDescription);

            var expectedResult = new BsonDocument
            {
                { "mapReduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument { {"replace", _outputCollectionNamespace.CollectionName }, { "db", _databaseNamespace.DatabaseName } } },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateOutputOptions_should_return_expected_result()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete
            var subjectReflector = new Reflector(subject);

            var result = subjectReflector.CreateOutputOptions();

            var expectedResult = new BsonDocument
            {
                { "replace", _outputCollectionNamespace.CollectionName },
                { "db", _outputCollectionNamespace.DatabaseNamespace.DatabaseName }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateOutputOptions_should_return_expected_result_when_ShardedOutput_is_set(
            [Values(null, false, true)]
            bool? shardedOutput)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
#pragma warning disable 618
                ShardedOutput = shardedOutput
#pragma warning restore 618
            };
            var subjectReflector = new Reflector(subject);

            var result = subjectReflector.CreateOutputOptions();

            var expectedResult = new BsonDocument
            {
                { "replace", _outputCollectionNamespace.CollectionName },
                { "db", _outputCollectionNamespace.DatabaseNamespace.DatabaseName },
                { "sharded", () => shardedOutput.Value, shardedOutput.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateOutputOptions_should_return_expected_result_when_NonAtomicOutput_is_provided(
            [Values(null, false, true)]
            bool? nonAtomicOutput)
        {
#pragma warning disable 618
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                NonAtomicOutput = nonAtomicOutput
#pragma warning restore 618
            };
            var subjectReflector = new Reflector(subject);
            var expectedResult = new BsonDocument
            {
                { "replace", _outputCollectionNamespace.CollectionName },
                { "db", _outputCollectionNamespace.DatabaseNamespace.DatabaseName },
                { "nonAtomic", () => nonAtomicOutput.Value, nonAtomicOutput.HasValue }
            };

            var result = subjectReflector.CreateOutputOptions();

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            ExecuteOperation(subject, async);

            ReadAllFromCollection(_outputCollectionNamespace).Should().BeEquivalentTo(
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
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary);
            var filter = BsonDocument.Parse("{ y : 'a' }");
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Collation = collation,
                Filter = filter
            };

            ExecuteOperation(subject, async);

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
            ReadAllFromCollection(_outputCollectionNamespace).Should().BeEquivalentTo(expectedResults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Filter_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var filter = BsonDocument.Parse("{ y : 'a' }");
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Filter = filter
            };

            ExecuteOperation(subject, async);

            ReadAllFromCollection(_outputCollectionNamespace).Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 1, value : 1 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_FinalizeFunction_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var finalizeFunction = new BsonJavaScript("function(key, reduced) { return -reduced; }");
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                FinalizeFunction = finalizeFunction
            };

            ExecuteOperation(subject, async);

            ReadAllFromCollection(_outputCollectionNamespace).Should().BeEquivalentTo(
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
        //    RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
        //    EnsureTestData();
        //    var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
        //    {
        //        JavaScriptMode = javaScriptMode
        //    };

        //    ExecuteOperation(subject, async);

        //    // the results are the same either way, but at least we're smoke testing JavaScriptMode
        //    ReadAllFromCollection(_outputCollectionNamespace).Should().Equal(
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
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Limit = limit
            };

            ExecuteOperation(subject, async);

            var expectedResults = new[]
            {
                new BsonDocument { { "_id", 1 }, { "value", limit == 1 ? 1 : 3 } }
            };
            ReadAllFromCollection(_outputCollectionNamespace).Should().Equal(expectedResults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_MaxTime_is_set(
            [Values(null, 1000)]
            int? seconds,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var maxTime = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                MaxTime = maxTime
            };

            ExecuteOperation(subject, async);

            // results should be the same whether MaxTime was used or not
            ReadAllFromCollection(_outputCollectionNamespace).Should().BeEquivalentTo(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Scope_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var finalizeFunction = new BsonJavaScript("function(key, reduced) { return reduced + zeroFromScope; }");
            var scope = new BsonDocument("zeroFromScope", 0);
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                FinalizeFunction = finalizeFunction,
                Scope = scope
            };

            ExecuteOperation(subject, async);

            ReadAllFromCollection(_outputCollectionNamespace).Should().BeEquivalentTo(
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
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var sort = new BsonDocument("_id", direction);
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Limit = 2,
                Sort = sort
            };

            ExecuteOperation(subject, async);

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
            ReadAllFromCollection(_outputCollectionNamespace).Should().BeEquivalentTo(expectedResults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            var exception = Record.Exception(() => ExecuteOperation(subject, null, async));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("binding");
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterType(ClusterType.ReplicaSet);
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
#pragma warning restore CS0618 // Type or member is obsolete
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
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            VerifySessionIdWasSentWhenSupported(subject, "mapReduce", async);
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
            private readonly MapReduceOutputToCollectionOperation _instance;

            // constructor
            public Reflector(MapReduceOutputToCollectionOperation instance)
            {
                _instance = instance;
            }

            // methods
            public BsonDocument CreateOutputOptions()
            {
                var method = typeof(MapReduceOutputToCollectionOperation).GetMethod("CreateOutputOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                return (BsonDocument)method.Invoke(_instance, new object[0]);
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
