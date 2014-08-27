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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    public class DistinctOperationTests
    {
        private string _collectionName;
        private string _databaseName;
        private string _fieldName;
        private MessageEncoderSettings _messageEncoderSettings;
        private IBsonSerializer<int> _valueSerializer;

        [SetUp]
        public void Setup()
        {
            _databaseName = "foo";
            _collectionName = "bar";
            _fieldName = "a.b";
            _messageEncoderSettings = new MessageEncoderSettings();
            _valueSerializer = new Int32Serializer();
        }

        [Test]
        public void Constructor_should_throw_when_database_name_is_null()
        {
            Action act = () => new DistinctOperation<int>(null, _collectionName, _valueSerializer, _fieldName, _messageEncoderSettings);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_should_throw_when_collection_name_is_null_or_empty()
        {
            Action act = () => new DistinctOperation<int>(_databaseName, null, _valueSerializer, _fieldName, _messageEncoderSettings);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_should_throw_when_value_serializer_is_null()
        {
            Action act = () => new DistinctOperation<int>(_databaseName, _collectionName, null, _fieldName, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_field_name_is_null()
        {
            Action act = () => new DistinctOperation<int>(_databaseName, _collectionName, _valueSerializer, null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new DistinctOperation<int>(_databaseName, _collectionName, _valueSerializer, _fieldName, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void CreateCommand_should_create_the_correct_command()
        {
            var subject = new DistinctOperation<int>(_databaseName, _collectionName, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                Filter = new BsonDocument("x", 1),
                MaxTime = TimeSpan.FromSeconds(20),
            };

            var cmd = subject.CreateCommand();

            cmd.Should().Be("{ distinct: \"bar\", key: \"a.b\", query: {x: 1}, maxTimeMS: 20000 }");
        }
    }
}
