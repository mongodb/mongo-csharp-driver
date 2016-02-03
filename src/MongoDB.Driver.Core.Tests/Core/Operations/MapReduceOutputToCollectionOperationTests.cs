/* Copyright 2013-2015 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using System.Reflection;
using System.Threading;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class MapReduceOutputToCollectionOperationTests : OperationTestBase
    {
        // fields
        private readonly BsonJavaScript _mapFunction = "map";
        private CollectionNamespace _outputCollectionNamespace;
        private readonly BsonJavaScript _reduceFunction = "reduce";

        // setup methods
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            _outputCollectionNamespace = new CollectionNamespace(_databaseNamespace, _collectionNamespace + "Output");
        }

        // test methods
        [Test]
        public void BypassDocumentValidation_should_get_and_set_value()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = true;

            subject.BypassDocumentValidation = value;
            var result = subject.BypassDocumentValidation;

            result.Should().Be(value);
        }

        [Test]
        public void constructor_should_initialize_instance()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            subject.BypassDocumentValidation.Should().NotHaveValue();
            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.OutputCollectionNamespace.Should().BeSameAs(_outputCollectionNamespace);
            subject.MapFunction.Should().BeSameAs(_mapFunction);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Filter.Should().BeNull();
            subject.ReduceFunction.Should().BeSameAs(_reduceFunction);
            subject.OutputMode.Should().Be(MapReduceOutputMode.Replace);
        }

        [Test]
        public void constructor_should_throw_when_outputCollectionNamespace_is_null()
        {
            Action action = () => new MapReduceOutputToCollectionOperation(_collectionNamespace, null, _mapFunction, _reduceFunction, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("outputCollectionNamespace");
        }

        [Test]
        public void CreateOutputOptions_should_return_expected_result()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var subjectReflector = new Reflector(subject);
            var expectedResult = new BsonDocument
            {
                { "replace", _outputCollectionNamespace.CollectionName },
                { "db", _outputCollectionNamespace.DatabaseNamespace.DatabaseName }
            };

            var result = subjectReflector.CreateOutputOptions();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateOutputOptions_should_return_expected_result_when_ShardedOutput_is_provided()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                ShardedOutput = true
            };
            var subjectReflector = new Reflector(subject);
            var expectedResult = new BsonDocument
            {
                { "replace", _outputCollectionNamespace.CollectionName },
                { "db", _outputCollectionNamespace.DatabaseNamespace.DatabaseName },
                { "sharded", true }
            };

            var result = subjectReflector.CreateOutputOptions();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateOutputOptions_should_return_expected_result_when_NonAtomicOutput_is_provided()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings)
            {
                NonAtomicOutput = true
            };
            var subjectReflector = new Reflector(subject);
            var expectedResult = new BsonDocument
            {
                { "replace", _outputCollectionNamespace.CollectionName },
                { "db", _outputCollectionNamespace.DatabaseNamespace.DatabaseName },
                { "nonAtomic", true }
            };

            var result = subjectReflector.CreateOutputOptions();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer(ClusterTypes = ClusterTypes.StandaloneOrReplicaSet)]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            EnsureTestData();

            var mapFunction = "function() { emit(this.x, this.v); }";
            var reduceFunction = "function(key, values) { var sum = 0; for (var i = 0; i < values.length; i++) { sum += values[i]; }; return sum; }";
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, mapFunction, reduceFunction, _messageEncoderSettings)
            {
                BypassDocumentValidation = true
            };
            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { {"_id", 1 }, { "value", 3 } },
                new BsonDocument { {"_id", 2 }, { "value", 4 } },
            };

            var response = ExecuteOperation(subject, async);

            response["ok"].ToBoolean().Should().BeTrue();

            var documents = ReadAllFromCollection(_outputCollectionNamespace, async);
            documents.Should().BeEquivalentTo(expectedDocuments);
        }

        [Test]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            Action action = () => ExecuteOperation(subject, null, async);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Test]
        public void Filter_should_get_and_set_value()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = new BsonDocument("_id", 1);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().Be(value);
        }
			
        [Test]
        public void NonAtomicOutput_should_get_and_set_value()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = true;

            subject.NonAtomicOutput = value;
            var result = subject.NonAtomicOutput;

            result.Should().Be(value);
        }

        [Test]
        public void OutputCollectionNamespace_should_get__value()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);

            var result = subject.OutputCollectionNamespace;

            result.Should().BeSameAs(_outputCollectionNamespace);
        }

        [Test]
        public void OutputMode_should_get_and_set_value()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = MapReduceOutputMode.Merge;

            subject.OutputMode = value;
            var result = subject.OutputMode;

            result.Should().Be(value);
        }

        [Test]
        public void ShardedOutput_should_get_and_set_value()
        {
            var subject = new MapReduceOutputToCollectionOperation(_collectionNamespace, _outputCollectionNamespace, _mapFunction, _reduceFunction, _messageEncoderSettings);
            var value = true;

            subject.ShardedOutput = value;
            var result = subject.ShardedOutput;

            result.Should().Be(value);
        }

        // helper methods
        private void EnsureTestData()
        {
            DropCollection();
            Insert(
                new BsonDocument { { "_id", 1 }, { "x", 1 }, { "v", 1 } },
                new BsonDocument { { "_id", 2 }, { "x", 1 }, { "v", 2 } },
                new BsonDocument { { "_id", 3 }, { "x", 2 }, { "v", 4 } });
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
            public BsonDocument CreateOutputOptions()
            {
                var method = typeof(MapReduceOutputToCollectionOperation).GetMethod("CreateOutputOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                return (BsonDocument)method.Invoke(_instance, new object[0]);
            }
        }
    }
}
