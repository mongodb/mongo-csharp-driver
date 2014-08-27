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
    public class CountOperationTests
    {
        private string _databaseName;
        private string _collectionName;
        private MessageEncoderSettings _messageEncoderSettings;

        [SetUp]
        public void Setup()
        {
            _databaseName = "foo";
            _collectionName = "bar";
            _messageEncoderSettings = new MessageEncoderSettings();
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void Constructor_should_throw_when_database_name_is_null_or_empty(string name)
        {
            Action act = () => new CountOperation(name, _collectionName, _messageEncoderSettings);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void Constructor_should_throw_when_collection_name_is_null_or_empty(string name)
        {
            Action act = () => new CountOperation(_databaseName, name, _messageEncoderSettings);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new CountOperation(_databaseName, _collectionName, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void CreateCommand_should_create_the_correct_command()
        {
            var subject = new CountOperation(_databaseName, _collectionName, _messageEncoderSettings)
            {
                Filter = new BsonDocument("x", 1),
                Hint = "funny",
                Limit = 10,
                MaxTime = TimeSpan.FromSeconds(20),
                Skip = 30
            };

            var cmd = subject.CreateCommand();

            cmd.Should().Be("{ count: \"bar\", query: {x: 1}, limit: 10, skip: 30, hint: \"funny\", maxTimeMS: 20000 }");
        }
    }
}
