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
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class DeleteMessageTests
    {
        private readonly CollectionNamespace _collectionNamespace = new CollectionNamespace("database", "collection");
        private readonly bool _isMulti = true;
        private readonly BsonDocument _query = new BsonDocument("x", 1);
        private readonly int _requestId = 1;

        [Fact]
        public void Constructor_should_initialize_instance()
        {
            var subject = new DeleteMessage(_requestId, _collectionNamespace, _query, _isMulti);
            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.IsMulti.Should().Be(_isMulti);
            subject.Query.Should().Be(_query);
            subject.RequestId.Should().Be(_requestId);
        }

        [Fact]
        public void Constructor_with_null_collectionName_should_throw()
        {
            Action action = () => new DeleteMessage(_requestId, null, _query, _isMulti);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_null_query_should_throw()
        {
            Action action = () => new DeleteMessage(_requestId, _collectionNamespace, null, _isMulti);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetEncoder_should_return_encoder()
        {
            var subject = new DeleteMessage(1, _collectionNamespace, new BsonDocument("x", 1), true);
            var mockEncoderFactory = new Mock<IMessageEncoderFactory>();
            var encoder = new Mock<IMessageEncoder>().Object;
            mockEncoderFactory.Setup(f => f.GetDeleteMessageEncoder()).Returns(encoder);

            var result = subject.GetEncoder(mockEncoderFactory.Object);

            result.Should().BeSameAs(encoder);
        }
    }
}
