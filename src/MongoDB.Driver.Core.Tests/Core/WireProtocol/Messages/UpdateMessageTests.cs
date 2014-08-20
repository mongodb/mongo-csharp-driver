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
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.WireProtocol.Messages
{
    [TestFixture]
    public class UpdateMessageTests
    {
        private readonly string _collectionName = "collection";
        private readonly string _databaseName = "database";
        private readonly BsonDocument _query = new BsonDocument("x", 1);
        private readonly int _requestId = 1;
        private readonly BsonDocument _update = new BsonDocument("$set", new BsonDocument("y", 2));

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void Constructor_should_initialize_instance(bool isMulti, bool isUpsert)
        {
            var subject = new UpdateMessage(_requestId, _databaseName, _collectionName, _query, _update, isMulti, isUpsert);
            subject.CollectionName.Should().Be(_collectionName);
            subject.DatabaseName.Should().Be(_databaseName);
            subject.IsMulti.Should().Be(isMulti);
            subject.IsUpsert.Should().Be(isUpsert);
            subject.Query.Should().Be(_query);
            subject.Update.Should().Be(_update);
            subject.RequestId.Should().Be(_requestId);
        }

        [Test]
        public void Constructor_with_null_collectionName_should_throw()
        {
            Action action = () => new UpdateMessage(_requestId, _databaseName, null, _query, _update, false, false);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_null_databaseName_should_throw()
        {
            Action action = () => new UpdateMessage(_requestId, null, _collectionName, _query, _update, false, false);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_null_query_should_throw()
        {
            Action action = () => new UpdateMessage(_requestId, _databaseName, _collectionName, null, _update, false, false);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_null_update_should_throw()
        {
            Action action = () => new UpdateMessage(_requestId, _databaseName, _collectionName, _query, null, false, false);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetEncoder_should_return_encoder()
        {
            var mockEncoder = Substitute.For<IMessageEncoder<UpdateMessage>>();
            var mockEncoderFactory = Substitute.For<IMessageEncoderFactory>();
            mockEncoderFactory.GetUpdateMessageEncoder().Returns(mockEncoder);

            var subject = new UpdateMessage(_requestId, _databaseName, _collectionName, _query, _update, false, false);
            var encoder = subject.GetEncoder(mockEncoderFactory);
            encoder.Should().BeSameAs(mockEncoder);
        }
    }
}
