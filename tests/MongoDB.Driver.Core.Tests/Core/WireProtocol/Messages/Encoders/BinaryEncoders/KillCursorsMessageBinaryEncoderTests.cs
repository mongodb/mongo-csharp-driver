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
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class KillCursorsMessageBinaryEncoderTests
    {
        #region static
        // static fields
        private static readonly long[] __cursorIds = new[] { 2L };
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly int __requestId = 1;
        private static readonly KillCursorsMessage __testMessage;
        private static readonly byte[] __testMessageBytes;

        // static constructor
        static KillCursorsMessageBinaryEncoderTests()
        {
            __testMessage = new KillCursorsMessage(__requestId, __cursorIds);

            __testMessageBytes = new byte[]
            {
                0, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                0, 0, 0, 0, // responseTo
                215, 7, 0, 0, // opcode = 2007
                0, 0, 0, 0, // reserved
                1, 0, 0, 0, // numberOfCursorIds
                2, 0, 0, 0, 0, 0, 0, 0 // cursorIds[0]
            };
            __testMessageBytes[0] = (byte)__testMessageBytes.Length;
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_stream_is_not_null()
        {
            using (var stream = new MemoryStream())
            {
                Action action = () => new KillCursorsMessageBinaryEncoder(stream, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_stream_is_null()
        {
            Action action = () => new KillCursorsMessageBinaryEncoder(null, __messageEncoderSettings);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var stream = new MemoryStream(__testMessageBytes))
            {
                var subject = new KillCursorsMessageBinaryEncoder(stream, __messageEncoderSettings);
                var message = subject.ReadMessage();
                message.CursorIds.Should().Equal(__cursorIds);
                message.RequestId.Should().Be(__testMessage.RequestId);
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var stream = new MemoryStream())
            {
                var subject = new KillCursorsMessageBinaryEncoder(stream, __messageEncoderSettings);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var stream = new MemoryStream())
            {
                var subject = new KillCursorsMessageBinaryEncoder(stream, __messageEncoderSettings);
                subject.WriteMessage(__testMessage);
                var bytes = stream.ToArray();
                bytes.Should().Equal(__testMessageBytes);
            }
        }
    }
}
