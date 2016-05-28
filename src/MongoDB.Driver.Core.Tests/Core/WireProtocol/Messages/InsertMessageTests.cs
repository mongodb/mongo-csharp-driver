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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class InsertMessageTests
    {
        private readonly CollectionNamespace _collectionNamespace = new CollectionNamespace("database", "collection");
        private readonly bool _continueOnError = true;
        private readonly BatchableSource<BsonDocument> _documentSource = new BatchableSource<BsonDocument>(Enumerable.Empty<BsonDocument>());
        private readonly int _maxBatchCount = 1;
        private readonly int _maxMessageSize = 2;
        private readonly int _requestId = 3;
        private readonly IBsonSerializer<BsonDocument> _serializer = BsonDocumentSerializer.Instance;

        [Fact]
        public void Constructor_should_initialize_instance()
        {
            var subject = new InsertMessage<BsonDocument>(_requestId,  _collectionNamespace, _serializer, _documentSource, _maxBatchCount, _maxMessageSize, _continueOnError);
            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.ContinueOnError.Should().Be(_continueOnError);
            subject.DocumentSource.Should().BeSameAs(_documentSource);
            subject.MaxBatchCount.Should().Be(_maxBatchCount);
            subject.MaxMessageSize.Should().Be(_maxMessageSize);
            subject.RequestId.Should().Be(_requestId);
            subject.Serializer.Should().BeSameAs(_serializer);
        }

        [Fact]
        public void Constructor_with_negative_maxBatchCount_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, _collectionNamespace, _serializer, _documentSource, -1, _maxMessageSize, _continueOnError);
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_with_negative_maxMessageSize_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, _collectionNamespace, _serializer, _documentSource, _maxBatchCount, -1, _continueOnError);
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_with_null_collectionNamespace_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, null, _serializer, _documentSource, _maxBatchCount, _maxMessageSize, _continueOnError);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_null_documents_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, _collectionNamespace, _serializer, null, _maxBatchCount, _maxMessageSize, _continueOnError);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_null_serializer_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, _collectionNamespace, null, _documentSource, _maxBatchCount, _maxMessageSize, _continueOnError);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetEncoder_should_return_encoder()
        {
            var subject = new InsertMessage<BsonDocument>(_requestId, _collectionNamespace, _serializer, _documentSource, _maxBatchCount, _maxMessageSize, _continueOnError);
            var mockEncoderFactory = new Mock<IMessageEncoderFactory>();
            var encoder = new Mock<IMessageEncoder>().Object;
            mockEncoderFactory.Setup(f => f.GetInsertMessageEncoder(_serializer)).Returns(encoder);

            var result = subject.GetEncoder(mockEncoderFactory.Object);

            result.Should().BeSameAs(encoder);
        }
    }
}
