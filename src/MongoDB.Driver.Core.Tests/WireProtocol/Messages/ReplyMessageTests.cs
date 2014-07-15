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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.WireProtocol.Messages
{
    [TestFixture]
    public class ReplyMessageTests
    {
        private readonly long _cursorId = 2;
        private readonly List<BsonDocument> _documents = new List<BsonDocument> { new BsonDocument("x", 1) };
        private readonly int _numberReturned = 1; // should match _documents.Count
        private readonly BsonDocument _queryFailureDocument = new BsonDocument("y", 2);
        private readonly int _requestId = 3;
        private readonly int _responseTo = 4;
        private readonly IBsonSerializer<BsonDocument> _serializer = BsonDocumentSerializer.Instance;
        private readonly int _startingFrom = 5;

        [Test]
        public void Constructor_should_initialize_instance()
        {
            var message = new ReplyMessage<BsonDocument>(_cursorId, false, _documents, _numberReturned, false, null, _requestId, _responseTo, _serializer, _startingFrom);
            message.CursorId.Should().Be(_cursorId);
            message.CursorNotFound.Should().BeFalse();
            message.Documents.Should().BeSameAs(_documents);
            message.NumberReturned.Should().Be(_numberReturned);
            message.QueryFailure.Should().BeFalse();
            message.QueryFailureDocument.Should().BeNull();
            message.RequestId.Should().Be(_requestId);
            message.ResponseTo.Should().Be(_responseTo);
            message.Serializer.Should().BeSameAs(_serializer);
            message.StartingFrom.Should().Be(_startingFrom);
        }

        [Test]
        public void Constructor_with_cursor_not_found_should_initialize_instance()
        {
            var message = new ReplyMessage<BsonDocument>(_cursorId, true, null, 0, false, null, _requestId, _responseTo, _serializer, 0);
            message.CursorId.Should().Be(_cursorId);
            message.CursorNotFound.Should().BeTrue();
            message.Documents.Should().BeNull();
            message.NumberReturned.Should().Be(0);
            message.QueryFailure.Should().BeFalse();
            message.QueryFailureDocument.Should().BeNull();
            message.RequestId.Should().Be(_requestId);
            message.ResponseTo.Should().Be(_responseTo);
            message.Serializer.Should().BeSameAs(_serializer);
            message.StartingFrom.Should().Be(0);
        }

        [Test]
        public void Constructor_with_both_documents_nor_queryFailureDocument_should_throw()
        {
            Action action = () => new ReplyMessage<BsonDocument>(_cursorId, false, _documents, _numberReturned, false, _queryFailureDocument, _requestId, _responseTo, null, _startingFrom);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_with_neither_documents_nor_queryFailureDocument_should_throw()
        {
            Action action = () => new ReplyMessage<BsonDocument>(_cursorId, false, null, _numberReturned, false, null, _requestId, _responseTo, null, _startingFrom);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_with_null_serializer_should_throw()
        {
            Action action = () => new ReplyMessage<BsonDocument>(_cursorId, false, _documents, _numberReturned, false, null, _requestId, _responseTo, null, _startingFrom);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_queryFailure_should_initialize_instance()
        {
            var message = new ReplyMessage<BsonDocument>(_cursorId, false, null, 0, true, _queryFailureDocument, _requestId, _responseTo, _serializer, 0);
            message.CursorId.Should().Be(_cursorId);
            message.CursorNotFound.Should().BeFalse();
            message.Documents.Should().BeNull();
            message.NumberReturned.Should().Be(0);
            message.QueryFailure.Should().BeTrue();
            message.QueryFailureDocument.Equals(_queryFailureDocument).Should().BeTrue();
            message.RequestId.Should().Be(_requestId);
            message.ResponseTo.Should().Be(_responseTo);
            message.Serializer.Should().BeSameAs(_serializer);
            message.StartingFrom.Should().Be(0);
        }

        [Test]
        public void GetEncoder_should_return_encoder()
        {
            var mockEncoder = Substitute.For<IMessageEncoder<ReplyMessage<BsonDocument>>>();
            var mockEncoderFactory = Substitute.For<IMessageEncoderFactory>();
            mockEncoderFactory.GetReplyMessageEncoder(_serializer).Returns(mockEncoder);

            var message = new ReplyMessage<BsonDocument>(_cursorId, false, _documents, _numberReturned, false, null, _requestId, _responseTo, _serializer, _startingFrom);
            var encoder = message.GetEncoder(mockEncoderFactory);
            encoder.Should().BeSameAs(mockEncoder);
        }
    }
}
