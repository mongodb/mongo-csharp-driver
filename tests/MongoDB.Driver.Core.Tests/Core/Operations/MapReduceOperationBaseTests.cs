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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class MapReduceOperationBaseTests : OperationTestBase
    {
        // fields
        private readonly BsonJavaScript _mapFunction = "map";
        private readonly BsonJavaScript _reduceFunction = "reduce";

        // test methods
        [Theory]
        [ParameterAttributeData]
        public void Collation_should_get_and_set_value(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void CollectionNamespace_should_get_value(
            [Values("a", "b")]
            string collectionName)
        {
            var collectionNamespace = new CollectionNamespace(_collectionNamespace.DatabaseNamespace, collectionName);
            var subject = new FakeMapReduceOperation(collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(collectionNamespace);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MapFunction.Should().BeSameAs(_mapFunction);
            subject.ReduceFunction.Should().BeSameAs(_reduceFunction);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.Filter.Should().BeNull();
            subject.FinalizeFunction.Should().BeNull();
            subject.JavaScriptMode.Should().NotHaveValue();
            subject.Limit.Should().NotHaveValue();
            subject.MaxTime.Should().NotHaveValue();
            subject.Scope.Should().BeNull();
            subject.Sort.Should().BeNull();
            subject.Verbose.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new FakeMapReduceOperation(null, _mapFunction, _reduceFunction, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_mapFunction_is_null()
        {
            var exception = Record.Exception(() => new FakeMapReduceOperation(_collectionNamespace, null, _reduceFunction, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("mapFunction");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction,  null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Fact]
        public void constructor_should_throw_when_reduceFunction_is_null()
        {
            var exception = Record.Exception(() => new FakeMapReduceOperation(_collectionNamespace, _mapFunction, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("reduceFunction");
        }

        [Fact]
        public void CreateCommand_should_return_the_expected_result()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Collation_is_provided(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var collation = locale == null ? null : new Collation(locale);
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_provided_but_not_supported()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.Collation.LastNotSupportedVersion));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Filter_is_provided(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string filterString)
        {
            var filter = filterString == null ? null : BsonDocument.Parse(filterString);
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                Filter = filter
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "query", filter, filter != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_FinalizeFunction_is_provided(
            [Values(null, "a", "b")]
            string code)
        {
            var finalizeFunction = code == null ? null : new BsonJavaScript(code);
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                FinalizeFunction = finalizeFunction
            };
            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "finalize", finalizeFunction, finalizeFunction != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_JavaScriptMode_is_provided(
            [Values(null, false, true)]
            bool? javaScriptMode)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                JavaScriptMode = javaScriptMode
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "jsMode", () => javaScriptMode.Value, javaScriptMode.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Limit_is_provided(
            [Values(null, 1L, 2L)]
            long? limit)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                Limit = limit
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "limit", () => limit.Value, limit.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_MaxTime_is_provided(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var maxTime = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "maxTimeMS", () => seconds.Value * 1000, seconds.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Scope_is_provided(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string scopeString)
        {
            var scope = scopeString == null ? null : BsonDocument.Parse(scopeString);
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                Scope = scope
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "scope", scope, scope != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Sort_is_provided(
            [Values(null, "{ x : 1 }", "{ x : -1 }")]
            string sortString)
        {
            var sort = sortString == null ? null : BsonDocument.Parse(sortString);
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                Sort = sort
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "sort", sort, sort != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_the_expected_result_when_Verbose_is_provided(
            [Values(null, false, true)]
            bool? verbose)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                Verbose = verbose
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "mapreduce", _collectionNamespace.CollectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "verbose", () => verbose.Value, verbose.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_should_get_and_set_value(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void FinalizeFunction_should_get_and_set_value(
            [Values(null, "a", "b")]
            string code)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = code == null ? null : new BsonJavaScript(code );

            subject.FinalizeFunction = value;
            var result = subject.FinalizeFunction;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void JavaScriptMode_should_get_and_set_value(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.JavaScriptMode = value;
            var result = subject.JavaScriptMode;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Limit_should_get_and_set_value(
            [Values(null, 0L, 1L)]
            long? value)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MapFunction_should_get_value(
            [Values("a", "b")]
            string code)
        {
            var mapFunction = new BsonJavaScript(code);
            var subject = new FakeMapReduceOperation(_collectionNamespace, mapFunction, _reduceFunction, _messageEncoderSettings);

            var result = subject.MapFunction;

            result.Should().BeSameAs(mapFunction);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_should_get_and_set_value(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            int seconds)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = TimeSpan.FromSeconds(seconds);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Fact]
        public void MessageEncoderSettings_should_get_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        [Fact]
        public void ReduceFunction_should_get_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            var result = subject.ReduceFunction;

            result.Should().BeSameAs(_reduceFunction);
        }

        [Theory]
        [ParameterAttributeData]
        public void Scope_should_get_and_set_value(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Scope = value;
            var result = subject.Scope;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sort_should_get_and_set_value(
            [Values(null, "{ x : 1 }", "{ x : -1 }")]
            string valueString)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Sort = value;
            var result = subject.Sort;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Verbose_should_get_and_set_value(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.Verbose = value;
            var result = subject.Verbose;

            result.Should().Be(value);
        }

        // nested types
        private class FakeMapReduceOperation : MapReduceOperationBase
        {
            public FakeMapReduceOperation(
                CollectionNamespace collectionNamespace,
                BsonJavaScript mapFunction,
                BsonJavaScript reduceFunction,
                MessageEncoderSettings messageEncoderSettings
                )
                : base(collectionNamespace, mapFunction, reduceFunction, messageEncoderSettings)
            {
            }

            protected override BsonDocument CreateOutputOptions()
            {
                return new BsonDocument("fake", 1);
            }
        }
    }
}
