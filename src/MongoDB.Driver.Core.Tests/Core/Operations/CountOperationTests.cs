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
        private CollectionNamespace _collectionNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        [SetUp]
        public void Setup()
        {
            _collectionNamespace = "foo.bar";
            _messageEncoderSettings = new MessageEncoderSettings();
        }

        [Test]
        public void Constructor_should_throw_when_collection_namespace_is_null()
        {
            Action act = () => new CountOperation(null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new CountOperation(_collectionNamespace, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void CreateCommand_should_create_the_correct_command()
        {
            var subject = new CountOperation(_collectionNamespace, _messageEncoderSettings)
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
