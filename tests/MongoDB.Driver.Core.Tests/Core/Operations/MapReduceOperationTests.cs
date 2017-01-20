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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
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
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MapFunction.Should().BeSameAs(_mapFunction);
            subject.ReduceFunction.Should().BeSameAs(_reduceFunction);
            subject.ResultSerializer.Should().BeSameAs(_resultSerializer);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.Filter.Should().BeNull();
            subject.FinalizeFunction.Should().BeNull();
            subject.JavaScriptMode.Should().NotHaveValue();
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
            var exception = Record.Exception(() => new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("resultSerializer");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_get_and_set_should_work(
            [Values(ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel level)
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
            var value = new ReadConcern(level);

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().Be(value);
        }

        [Fact]
        public void ReadConcern_set_should_throw_when_value_is_null()
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);

            var exception = Record.Exception(() => subject.ReadConcern = null);

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("value");
        }

        [Fact]
        public void ResultSerializer_should_get_value()
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);

            var result = subject.ResultSerializer;

            result.Should().BeSameAs(_resultSerializer);
        }

        [Fact]
        public void CreateOutputOptions_should_return_expected_result()
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
            var subjectReflector = new Reflector(subject);

            var result = subjectReflector.CreateOutputOptions();

            result.Should().Be("{ inline : 1 }");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().Equal(
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
            RequireServer.Check().Supports(Feature.Collation);
            EnsureTestData();
            var collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary);
            var filter = BsonDocument.Parse("{ y : 'a' }");
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
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
            results.Should().Equal(expectedResults);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Filter_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var filter = BsonDocument.Parse("{ y : 'a' }");
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
            {
                Filter = filter
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().Equal(
                BsonDocument.Parse("{ _id : 1, value : 1 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_FinalizeFunction_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var finalizeFunction = new BsonJavaScript("function(key, reduced) { return -reduced; }");
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
            {
                FinalizeFunction = finalizeFunction
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().Equal(
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

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Limit_is_set(
            [Values(1, 2)]
            long limit,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
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

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_MaxTime_is_set(
            [Values(null, 1000)]
            int? seconds,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.MaxTime);
            EnsureTestData();
            var maxTime = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            // results should be the same whether MaxTime was used or not
            results.Should().Equal(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Local)] // only use values that are valid on StandAlone servers
            ReadConcernLevel? readConcernLevel,
            [Values(false, true)]
            bool async)
       {
            RequireServer.Check().Supports(Feature.ReadConcern);
            EnsureTestData();
            var readConcern = new ReadConcern(readConcernLevel);
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            // results should be the same whether ReadConcern was used or not
            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().Equal(
                BsonDocument.Parse("{ _id : 1, value : 3 }"),
                BsonDocument.Parse("{ _id : 2, value : 4 }"));
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_ResultSerializer_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var resultSerializer = new ElementDeserializer<double>("value", new DoubleSerializer());
            var subject = new MapReduceOperation<double>(_collectionNamespace, _mapFunction, _reduceFunction, resultSerializer, _messageEncoderSettings);

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().Equal(3, 4);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_results_when_Scope_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var finalizeFunction = new BsonJavaScript("function(key, reduced) { return reduced + zeroFromScope; }");
            var scope = new BsonDocument("zeroFromScope", 0);
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
            {
                FinalizeFunction = finalizeFunction,
                Scope = scope
            };

            var cursor = ExecuteOperation(subject, async);
            var results = ReadCursorToEnd(cursor, async);

            results.Should().Equal(
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
            RequireServer.Check();
            EnsureTestData();
            var sort = new BsonDocument("_id", direction);
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
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
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);

            var exception = Record.Exception(() => ExecuteOperation(subject, (IReadBinding)null, async));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("binding");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_ReadConcern_is_set_but_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.ReadConcern);
            EnsureTestData();
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Local)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoClientException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var readConcern = level.HasValue ? new ReadConcern(level.Value) : ReadConcern.Default;
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var result = subject.CreateCommand(Feature.ReadConcern.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("inline", 1) },
                { "readConcern", () => readConcern.ToBsonDocument(), !readConcern.IsServerDefault }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_ReadConcern_is_set_but_not_supported()
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings)
            {
                ReadConcern = ReadConcern.Majority
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.ReadConcern.LastNotSupportedVersion));

            exception.Should().BeOfType<MongoClientException>();
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
        }
    }
}
