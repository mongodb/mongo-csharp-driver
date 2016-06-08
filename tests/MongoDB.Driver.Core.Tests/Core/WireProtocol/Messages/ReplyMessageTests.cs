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
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
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

        [Fact]
        public void Constructor_should_initialize_instance()
        {
            var subject = new ReplyMessage<BsonDocument>(true, _cursorId, false, _documents, _numberReturned, false, null, _requestId, _responseTo, _serializer, _startingFrom);
            subject.AwaitCapable.Should().BeTrue();
            subject.CursorId.Should().Be(_cursorId);
            subject.CursorNotFound.Should().BeFalse();
            subject.Documents.Should().BeSameAs(_documents);
            subject.NumberReturned.Should().Be(_numberReturned);
            subject.QueryFailure.Should().BeFalse();
            subject.QueryFailureDocument.Should().BeNull();
            subject.RequestId.Should().Be(_requestId);
            subject.ResponseTo.Should().Be(_responseTo);
            subject.Serializer.Should().BeSameAs(_serializer);
            subject.StartingFrom.Should().Be(_startingFrom);
        }

        [Fact]
        public void Constructor_with_cursor_not_found_should_initialize_instance()
        {
            var subject = new ReplyMessage<BsonDocument>(true, _cursorId, true, null, 0, false, null, _requestId, _responseTo, _serializer, 0);
            subject.AwaitCapable.Should().BeTrue();
            subject.CursorId.Should().Be(_cursorId);
            subject.CursorNotFound.Should().BeTrue();
            subject.Documents.Should().BeNull();
            subject.NumberReturned.Should().Be(0);
            subject.QueryFailure.Should().BeFalse();
            subject.QueryFailureDocument.Should().BeNull();
            subject.RequestId.Should().Be(_requestId);
            subject.ResponseTo.Should().Be(_responseTo);
            subject.Serializer.Should().BeSameAs(_serializer);
            subject.StartingFrom.Should().Be(0);
        }

        [Fact]
        public void Constructor_with_both_documents_nor_queryFailureDocument_should_throw()
        {
            Action action = () => new ReplyMessage<BsonDocument>(true, _cursorId, false, _documents, _numberReturned, false, _queryFailureDocument, _requestId, _responseTo, null, _startingFrom);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Constructor_with_neither_documents_nor_queryFailureDocument_should_throw()
        {
            Action action = () => new ReplyMessage<BsonDocument>(true, _cursorId, false, null, _numberReturned, false, null, _requestId, _responseTo, null, _startingFrom);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Constructor_with_null_serializer_should_throw()
        {
            Action action = () => new ReplyMessage<BsonDocument>(true, _cursorId, false, _documents, _numberReturned, false, null, _requestId, _responseTo, null, _startingFrom);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_queryFailure_should_initialize_instance()
        {
            var subject = new ReplyMessage<BsonDocument>(true, _cursorId, false, null, 0, true, _queryFailureDocument, _requestId, _responseTo, _serializer, 0);
            subject.AwaitCapable.Should().BeTrue();
            subject.CursorId.Should().Be(_cursorId);
            subject.CursorNotFound.Should().BeFalse();
            subject.Documents.Should().BeNull();
            subject.NumberReturned.Should().Be(0);
            subject.QueryFailure.Should().BeTrue();
            subject.QueryFailureDocument.Should().Be(_queryFailureDocument);
            subject.RequestId.Should().Be(_requestId);
            subject.ResponseTo.Should().Be(_responseTo);
            subject.Serializer.Should().BeSameAs(_serializer);
            subject.StartingFrom.Should().Be(0);
        }

        [Fact]
        public void GetEncoder_should_return_encoder()
        {
            var subject = new ReplyMessage<BsonDocument>(true, _cursorId, false, _documents, _numberReturned, false, null, _requestId, _responseTo, _serializer, _startingFrom);
            var mockEncoderFactory = new Mock<IMessageEncoderFactory>();
            var encoder = new Mock<IMessageEncoder>().Object;
            mockEncoderFactory.Setup(f => f.GetReplyMessageEncoder(_serializer)).Returns(encoder);

            var result = subject.GetEncoder(mockEncoderFactory.Object);

            result.Should().BeSameAs(encoder);
        }
    }
}
