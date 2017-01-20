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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
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
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.OutputCollectionNamespace.Should().BeSameAs(_outputCollectionNamespace);
            subject.MapFunction.Should().BeSameAs(_mapFunction);
            subject.ReduceFunction.Should().BeSameAs(_reduceFunction);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.BypassDocumentValidation.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.Filter.Should().BeNull();
            subject.FinalizeFunction.Should().BeNull();
            subject.JavaScriptMode.Should().NotHaveValue();
            subject.Limit.Should().NotHaveValue();
            subject.MaxTime.Should().NotHaveValue();
            subject.NonAtomicOutput.Should().NotHaveValue();
            subject.OutputMode.Should().Be(MapReduceOutputMode.Replace);
            subject.Scope.Should().BeNull();
            subject.Sort.Should().BeNull();
            subject.Verbose.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_should_throw_when_outputCollectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new MapReduceOutputToCollectionOperation(_collectionNamespace, null, _mapFunction, _reduceFunction, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("outputCollectionNamespace");
        }

        [Theory]
        [ParameterAttributeData]
        public void BypassDocumentValidation_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

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
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
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
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.NonAtomicOutput = value;
            var result = subject.NonAtomicOutput;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void OutputCollectionNamespace_get_and_set_should_work(
            [Values("a", "b")]
            string collectionName)
        {
            var outputCollectionNamespace = new CollectionNamespace(_databaseNamespace, collectionName);
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            var result = subject.OutputCollectionNamespace;

            result.Should().BeSameAs(outputCollectionNamespace);
        }

        [Theory]
        [ParameterAttributeData]
        public void OutputMode_get_and_set_should_work(
            [Values(MapReduceOutputMode.Merge, MapReduceOutputMode.Reduce)]
            MapReduceOutputMode value)
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.OutputMode = value;
            var result = subject.OutputMode;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ShardedOutput_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.ShardedOutput = value;
            var result = subject.ShardedOutput;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_BypassDocumentValidation_is_set(
            [Values(null, false, true)]
            bool? bypassDocumentValidation,
            [Values(false, true)]
            bool useServerVersionSupportingBypassDocumentValidation)
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                BypassDocumentValidation = bypassDocumentValidation
            };
            var subjectReflector = new Reflector(subject);
            var serverVersion = Feature.BypassDocumentValidation.SupportedOrNotSupportedVersion(useServerVersionSupportingBypassDocumentValidation);

            var result = subjectReflector.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument { {"replace", _outputCollectionNamespace.CollectionName }, { "db", _databaseNamespace.DatabaseName } } },
                { "bypassDocumentValidation", () => bypassDocumentValidation.Value, bypassDocumentValidation.HasValue && Feature.BypassDocumentValidation.IsSupported(serverVersion) }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w,
            [Values(false, true)]
            bool isWriteConcernSupported)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var subjectReflector = new Reflector(subject);
            var serverVersion = Feature.CommandsThatWriteAcceptWriteConcern.SupportedOrNotSupportedVersion(isWriteConcernSupported);

            var result = subjectReflector.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument { {"replace", _outputCollectionNamespace.CollectionName }, { "db", _databaseNamespace.DatabaseName } } },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null && isWriteConcernSupported }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateOutputOptions_should_return_expected_result()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
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
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                ShardedOutput = shardedOutput
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
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                NonAtomicOutput = nonAtomicOutput
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

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            ExecuteOperation(subject, async);

            ReadAllFromCollection(_outputCollectionNamespace).Should().Equal(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Collation_is_set(
            [Values(false, true)]
            bool caseSensitive,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).Supports(Feature.Collation);
            EnsureTestData();
            var collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary);
            var filter = BsonDocument.Parse("{ y : 'a' }");
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
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
            ReadAllFromCollection(_outputCollectionNamespace).Should().Equal(expectedResults);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Filter_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var filter = BsonDocument.Parse("{ y : 'a' }");
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                Filter = filter
            };

            ExecuteOperation(subject, async);

            ReadAllFromCollection(_outputCollectionNamespace).Should().Equal(
                BsonDocument.Parse("{ _id : 1, value : 1 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_FinalizeFunction_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var finalizeFunction = new BsonJavaScript("function(key, reduced) { return -reduced; }");
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                FinalizeFunction = finalizeFunction
            };

            ExecuteOperation(subject, async);

            ReadAllFromCollection(_outputCollectionNamespace).Should().Equal(
                BsonDocument.Parse("{ _id : 1, value : -3 }"),
                BsonDocument.Parse("{ _id : 2, value : -4 }"));
        }

        // TODO: figure out why test fails when JavaScriptMode = true (server bug?)

        //[SkippableTheory]
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

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Limit_is_set(
            [Values(1, 2)]
            long limit,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
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

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_MaxTime_is_set(
            [Values(null, 1000)]
            int? seconds,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).Supports(Feature.MaxTime);
            EnsureTestData();
            var maxTime = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            ExecuteOperation(subject, async);

            // results should be the same whether MaxTime was used or not
            ReadAllFromCollection(_outputCollectionNamespace).Should().Equal(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Scope_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            EnsureTestData();
            var finalizeFunction = new BsonJavaScript("function(key, reduced) { return reduced + zeroFromScope; }");
            var scope = new BsonDocument("zeroFromScope", 0);
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                FinalizeFunction = finalizeFunction,
                Scope = scope
            };

            ExecuteOperation(subject, async);

            ReadAllFromCollection(_outputCollectionNamespace).Should().Equal(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [SkippableTheory]
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
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
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
            ReadAllFromCollection(_outputCollectionNamespace).Should().Equal(expectedResults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            var exception = Record.Exception(() => ExecuteOperation(subject, null, async));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("binding");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
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
            private readonly MapReduceOutputToCollectionOperation _instance;

            // constructor
            public Reflector(MapReduceOutputToCollectionOperation instance)
            {
                _instance = instance;
            }

            // methods
            public BsonDocument CreateCommand(SemanticVersion serverVersion)
            {
                var method = typeof(MapReduceOutputToCollectionOperation).GetMethod("CreateCommand", BindingFlags.NonPublic | BindingFlags.Instance);
                return (BsonDocument)method.Invoke(_instance, new object[] { serverVersion });
            }

            public BsonDocument CreateOutputOptions()
            {
                var method = typeof(MapReduceOutputToCollectionOperation).GetMethod("CreateOutputOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                return (BsonDocument)method.Invoke(_instance, new object[0]);
            }
        }
    }
}
