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
    public class GetMoreMessageTests
    {
        private readonly int _batchSize = 1;
        private readonly string _collectionName = "collection";
        private readonly long _cursorId = 2;
        private readonly string _databaseName = "database";
        private readonly int _requestId = 3;

        [Test]
        public void Constructor_should_initialize_instance()
        {
            var subject = new GetMoreMessage(_requestId, _databaseName, _collectionName, _cursorId, _batchSize);
            subject.BatchSize.Should().Be(_batchSize);
            subject.CursorId.Should().Be(_cursorId);
            subject.CollectionName.Should().Be(_collectionName);
            subject.DatabaseName.Should().Be(_databaseName);
            subject.RequestId.Should().Be(_requestId);
        }

        [Test]
        public void Constructor_with_negative_batchSize_should_throw()
        {
            Action action = () => new GetMoreMessage(_requestId, _databaseName, _collectionName, _cursorId, -1);
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Constructor_with_null_collectionName_should_throw()
        {
            Action action = () => new GetMoreMessage(_requestId, _databaseName, null, _cursorId, _batchSize);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_null_databaseName_should_throw()
        {
            Action action = () => new GetMoreMessage(_requestId, null, _collectionName, _cursorId, _batchSize);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetEncoder_should_return_encoder()
        {
            var mockEncoder = Substitute.For<IMessageEncoder<GetMoreMessage>>();
            var mockEncoderFactory = Substitute.For<IMessageEncoderFactory>();
            mockEncoderFactory.GetGetMoreMessageEncoder().Returns(mockEncoder);

            var subject = new GetMoreMessage(_requestId, _databaseName, _collectionName, _cursorId, _batchSize);
            var encoder = subject.GetEncoder(mockEncoderFactory);
            encoder.Should().BeSameAs(mockEncoder);
        }
    }
}
