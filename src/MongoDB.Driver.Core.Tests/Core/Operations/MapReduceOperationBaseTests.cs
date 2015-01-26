/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class MapReduceOperationBaseTests
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace = new CollectionNamespace(new DatabaseNamespace("databaseName"), "collectionName");
        private readonly BsonJavaScript _mapFunction = "map";
        private readonly MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();
        private readonly BsonDocument _query = new BsonDocument("query", 1);
        private readonly BsonJavaScript _reduceFunction = "reduce";

        // test methods
        [Test]
        public void CollectionNamespace_should_get_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(_collectionNamespace);
        }

        [Test]
        public void constructor_should_initialize_instance()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.FinalizeFunction.Should().BeNull();
            subject.JavaScriptMode.Should().NotHaveValue();
            subject.Limit.Should().NotHaveValue();
            subject.MapFunction.Should().BeSameAs(_mapFunction);
            subject.MaxTime.Should().NotHaveValue();
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Filter.Should().BeSameAs(_query);
            subject.ReduceFunction.Should().BeSameAs(_reduceFunction);
            subject.Scope.Should().BeNull();
            subject.Sort.Should().BeNull();
            subject.Verbose.Should().NotHaveValue();
        }

        [Test]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            Action action = () => new FakeMapReduceOperation(null, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Test]
        public void constructor_should_throw_when_mapFunction_is_null()
        {
            Action action = () => new FakeMapReduceOperation(_collectionNamespace, null, _reduceFunction, _query, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("mapFunction");
        }

        [Test]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            Action action = () => new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageEncoderSettings");
        }

        [Test]
        public void constructor_should_throw_when_filter_is_null()
        {
            Action action = () => new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filter");
        }

        [Test]
        public void constructor_should_throw_when_reduceFunction_is_null()
        {
            Action action = () => new FakeMapReduceOperation(_collectionNamespace, _mapFunction, null, _query, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("reduceFunction");
        }

        [Test]
        public void CreateCommand_should_return_the_expected_result()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "mapreduce", "collectionName" },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "query", _query }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_the_expected_result_when_FinalizeFunction_is_provided()
        {
            var finalizeFunction = new BsonJavaScript("finalize");
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings)
            {
                FinalizeFunction = finalizeFunction
            };
            var expectedResult = new BsonDocument
            {
                { "mapreduce", "collectionName" },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "query", _query },
                { "finalize", finalizeFunction }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_the_expected_result_when_JavaScriptMode_is_provided()
        {
            var javaScriptMode = true;
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings)
            {
                JavaScriptMode = javaScriptMode
            };
            var expectedResult = new BsonDocument
            {
                { "mapreduce", "collectionName" },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "query", _query },
                { "jsMode", javaScriptMode }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_the_expected_result_when_Limit_is_provided()
        {
            var limit = 1L;
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings)
            {
                Limit = limit
            };
            var expectedResult = new BsonDocument
            {
                { "mapreduce", "collectionName" },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "query", _query },
                { "limit", limit }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_the_expected_result_when_MaxTime_is_provided()
        {
            var maxTime = TimeSpan.FromSeconds(1.5);
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };
            var expectedResult = new BsonDocument
            {
                { "mapreduce", "collectionName" },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "query", _query },
                { "maxTimeMS", 1500.0 }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_the_expected_result_when_Scope_is_provided()
        {
            var scope = new BsonDocument("scope", 1);
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings)
            {
                Scope = scope
            };
            var expectedResult = new BsonDocument
            {
                { "mapreduce", "collectionName" },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "query", _query },
                { "scope", scope }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_the_expected_result_when_Sort_is_provided()
        {
            var sort = new BsonDocument("sort", 1);
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings)
            {
                Sort = sort
            };
            var expectedResult = new BsonDocument
            {
                { "mapreduce", "collectionName" },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "query", _query },
                { "sort", sort }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_the_expected_result_when_Verbose_is_provided()
        {
            var verbose = true;
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings)
            {
                Verbose = verbose
            };
            var expectedResult = new BsonDocument
            {
                { "mapreduce", "collectionName" },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out", new BsonDocument("fake", 1) },
                { "query", _query },
                { "verbose", verbose }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void FinalizeFunction_should_get_and_set_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);
            var value = new BsonJavaScript("finalize");

            subject.FinalizeFunction = value;
            var result = subject.FinalizeFunction;

            result.Should().BeSameAs(value);
        }

        [Test]
        public void JavaScriptMode_should_get_and_set_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);
            var value = true;

            subject.JavaScriptMode = value;
            var result = subject.JavaScriptMode;

            result.Should().Be(value);
        }

        [Test]
        public void Limit_should_get_and_set_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);
            var value = 1L;

            subject.Limit = value;
            var result = subject.Limit;

            result.Should().Be(value);
        }

        [Test]
        public void MapFunction_should_get_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);

            var result = subject.MapFunction;

            result.Should().BeSameAs(_mapFunction);
        }

        [Test]
        public void MaxTime_should_get_and_set_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);
            var value = TimeSpan.FromSeconds(1.5);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Test]
        public void MessageEncoderSettings_should_get_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        public void Filter_should_get_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);

            var result = subject.Filter;

            result.Should().BeSameAs(_query);
        }

        [Test]
        public void ReduceFunction_should_get_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);

            var result = subject.ReduceFunction;

            result.Should().BeSameAs(_reduceFunction);
        }

        [Test]
        public void Scope_should_get_and_set_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);
            var value = new BsonDocument("scope", 1);

            subject.Scope = value;
            var result = subject.Scope;

            result.Should().Be(value);
        }

        [Test]
        public void Sort_should_get_and_set_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);
            var value = new BsonDocument("sort", 1);

            subject.Sort = value;
            var result = subject.Sort;

            result.Should().Be(value);
        }

        [Test]
        public void Verbose_should_get_and_set_value()
        {
            var subject = new FakeMapReduceOperation(_collectionNamespace, _mapFunction, _reduceFunction, _query, _messageEncoderSettings);
            var value = true;

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
                BsonDocument query,
                MessageEncoderSettings messageEncoderSettings
                )
                : base(collectionNamespace, mapFunction, reduceFunction, query, messageEncoderSettings)
            {
            }

            protected override BsonDocument CreateOutputOptions()
            {
                return new BsonDocument("fake", 1);
            }
        }
    }
}
