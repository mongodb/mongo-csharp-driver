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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class UpdateMessageTests
    {
        private readonly CollectionNamespace _collectionNamespace = new CollectionNamespace("database", "collection");
        private readonly BsonDocument _query = new BsonDocument("x", 1);
        private readonly int _requestId = 1;
        private readonly BsonDocument _update = new BsonDocument("$set", new BsonDocument("y", 2));
        private readonly IElementNameValidator _updateValidator = NoOpElementNameValidator.Instance;

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void Constructor_should_initialize_instance(bool isMulti, bool isUpsert)
        {
            var subject = new UpdateMessage(_requestId, _collectionNamespace, _query, _update, _updateValidator, isMulti, isUpsert);
            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.IsMulti.Should().Be(isMulti);
            subject.IsUpsert.Should().Be(isUpsert);
            subject.Query.Should().Be(_query);
            subject.Update.Should().Be(_update);
            subject.RequestId.Should().Be(_requestId);
        }

        [Fact]
        public void Constructor_with_null_collectionNamespace_should_throw()
        {
            Action action = () => new UpdateMessage(_requestId, null, _query, _update, _updateValidator, false, false);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_null_query_should_throw()
        {
            Action action = () => new UpdateMessage(_requestId, _collectionNamespace, null, _update, _updateValidator, false, false);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_null_update_should_throw()
        {
            Action action = () => new UpdateMessage(_requestId, _collectionNamespace, _query, null, _updateValidator, false, false);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetEncoder_should_return_encoder()
        {
            var subject = new UpdateMessage(_requestId, _collectionNamespace, _query, _update, _updateValidator, false, false);
            var mockEncoderFactory = new Mock<IMessageEncoderFactory>();
            var encoder = new Mock<IMessageEncoder>().Object;
            mockEncoderFactory.Setup(f => f.GetUpdateMessageEncoder()).Returns(encoder);

            var result = subject.GetEncoder(mockEncoderFactory.Object);

            result.Should().BeSameAs(encoder);
        }
    }
}
