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
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class KillCursorsMessageTests
    {
        private readonly IEnumerable<long> _cursorIds = new long[] { 1 };
        private readonly int _requestId = 1;

        [Fact]
        public void Constructor_should_initialize_instance()
        {
            var subject = new KillCursorsMessage(_requestId, _cursorIds);
            subject.CursorIds.Should().Equal(_cursorIds);
            subject.RequestId.Should().Be(1);
        }

        [Fact]
        public void Constructor_with_null_cursorIds_should_throw()
        {
            Action action = () => new KillCursorsMessage(_requestId, null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetEncoder_should_return_encoder()
        {
            var subject = new KillCursorsMessage(_requestId, _cursorIds);
            var mockEncoderFactory = new Mock<IMessageEncoderFactory>();
            var encoder = new Mock<IMessageEncoder>().Object;
            mockEncoderFactory.Setup(f => f.GetKillCursorsMessageEncoder()).Returns(encoder);

            var result = subject.GetEncoder(mockEncoderFactory.Object);

            result.Should().BeSameAs(encoder);
        }
    }
}
