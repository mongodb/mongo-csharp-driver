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
    public class QueryMessageTests
    {
        private readonly int _batchSize = 1;
        private readonly CollectionNamespace _collectionNamespace = new CollectionNamespace("database", "collection");
        private readonly BsonDocument _fields = new BsonDocument("x", 1);
        private readonly BsonDocument _query = new BsonDocument("y", 2);
        private readonly IElementNameValidator _queryValidator = NoOpElementNameValidator.Instance;
        private readonly int _requestId = 2;
        private readonly int _skip = 3;

        [Theory]
        [InlineData(false, false, false, false, false, false)]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)]
        public void Constructor_should_initialize_instance(bool awaitData, bool noCursorTimeout, bool oplogReplay, bool partialOk, bool slaveOk, bool tailableCursor)
        {
            var subject = new QueryMessage(_requestId, _collectionNamespace, _query, _fields, _queryValidator, _skip, _batchSize, slaveOk, partialOk, noCursorTimeout, oplogReplay, tailableCursor, awaitData);
            subject.AwaitData.Should().Be(awaitData);
            subject.BatchSize.Should().Be(_batchSize);
            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.Fields.Should().Be(_fields);
            subject.NoCursorTimeout.Should().Be(noCursorTimeout);
            subject.OplogReplay.Should().Be(oplogReplay);
            subject.PartialOk.Should().Be(partialOk);
            subject.Query.Should().Be(_query);
            subject.RequestId.Should().Be(_requestId);
            subject.SlaveOk.Should().Be(slaveOk);
            subject.TailableCursor.Should().Be(tailableCursor);
        }

        [Fact]
        public void Constructor_with_negative_skip_should_throw()
        {
            Action action = () => new QueryMessage(_requestId, _collectionNamespace, _query, _fields, _queryValidator, -1, _batchSize, false, false, false, false, false, false);
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_with_null_collectionNamespace_should_throw()
        {
            Action action = () => new QueryMessage(_requestId, null, _query, _fields, _queryValidator, _skip, _batchSize, false, false, false, false, false, false);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_null_query_should_throw()
        {
            Action action = () => new QueryMessage(_requestId, _collectionNamespace, null, _fields, _queryValidator, _skip, _batchSize, false, false, false, false, false, false);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetEncoder_should_return_encoder()
        {
            var subject = new QueryMessage(_requestId, _collectionNamespace, _query, _fields, _queryValidator, _skip, _batchSize, false, false, false, false, false, false);
            var mockEncoderFactory = new Mock<IMessageEncoderFactory>();
            var encoder = new Mock<IMessageEncoder>().Object;
            mockEncoderFactory.Setup(f => f.GetQueryMessageEncoder()).Returns(encoder);

            var result = subject.GetEncoder(mockEncoderFactory.Object);

            result.Should().BeSameAs(encoder);
        }
    }
}
