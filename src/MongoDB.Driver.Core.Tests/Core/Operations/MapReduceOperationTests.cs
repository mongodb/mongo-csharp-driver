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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class MapReduceOperationTests : OperationTestBase
    {
        // fields
        private readonly BsonJavaScript _mapFunction = "map";
        private readonly BsonJavaScript _reduceFunction = "reduce";
        private readonly IBsonSerializer<BsonDocument> _resultSerializer = BsonDocumentSerializer.Instance;

        // test methods
        [Test]
        public void constructor_should_initialize_instance()
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MapFunction.Should().BeSameAs(_mapFunction);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Filter.Should().BeNull();
            subject.ReduceFunction.Should().BeSameAs(_reduceFunction);
            subject.ResultSerializer.Should().BeSameAs(_resultSerializer);
        }

        [Test]
        public void constructor_should_throw_when_resultSerializer_is_null()
        {
            Action action = () => new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("resultSerializer");
        }

        [Test]
        public void CreateOutputOptions_should_return_expected_result()
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);
            var subjectReflector = new Reflector(subject);
            var expectedResult = new BsonDocument("inline", 1);

            var result = subjectReflector.CreateOutputOptions();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer]
        public async Task ExecuteAsync_should_return_expected_results()
        {
            await EnsureTestDataAsync();

            var mapFunction = "function() { emit(this.x, this.v); }";
            var reduceFunction = "function(key, values) { var sum = 0; for (var i = 0; i < values.length; i++) { sum += values[i]; }; return sum; }";
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, mapFunction, reduceFunction, _resultSerializer, _messageEncoderSettings);
            var expectedResults = new List<BsonDocument>
            {
                new BsonDocument { {"_id", 1 }, { "value", 3 } },
                new BsonDocument { {"_id", 2 }, { "value", 4 } },
            };

            var cursor = await ExecuteOperationAsync(subject);
            var results = await cursor.ToListAsync();

            results.Should().Equal(expectedResults);
        }

        [Test]
        public void ExecuteAsync_should_throw_when_binding_is_null()
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);

            Func<Task> act = () => subject.ExecuteAsync(null, CancellationToken.None);

            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Test]
        public void ResultSerializer_should_get_value()
        {
            var subject = new MapReduceOperation<BsonDocument>(_collectionNamespace, _mapFunction, _reduceFunction, _resultSerializer, _messageEncoderSettings);

            var result = subject.ResultSerializer;

            result.Should().BeSameAs(_resultSerializer);
        }

        // helper methods
        private async Task EnsureTestDataAsync()
        {
            await DropCollectionAsync();
            await InsertAsync(
                new BsonDocument { { "_id", 1 }, { "x", 1 }, { "v", 1 } },
                new BsonDocument { { "_id", 2 }, { "x", 1 }, { "v", 2 } },
                new BsonDocument { { "_id", 3 }, { "x", 2 }, { "v", 4 } });        
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
