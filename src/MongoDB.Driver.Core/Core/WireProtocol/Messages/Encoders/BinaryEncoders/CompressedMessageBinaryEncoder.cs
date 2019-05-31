/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    /// <summary>
    /// Represents a binary encoder for a compressed message.
    /// </summary>
    public sealed class CompressedMessageBinaryEncoder : MessageBinaryEncoderBase, IMessageEncoder
    {
        private readonly ICompressorSource _compressorSource;
        private readonly MessageEncoderSettings _encoderSettings;
        private readonly IMessageEncoderSelector _originalEncoderSelector;
        private const int MessageHeaderLength = 16;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedMessageBinaryEncoder" /> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="originalEncoderSelector">The original encoder selector.</param>
        /// <param name="compressorSource">The compressor source.</param>
        /// <param name="encoderSettings">The encoder settings.</param>
        public CompressedMessageBinaryEncoder(
            Stream stream,
            IMessageEncoderSelector originalEncoderSelector,
            ICompressorSource compressorSource,
            MessageEncoderSettings encoderSettings)
            : base(stream, encoderSettings)
        {
            _compressorSource = Ensure.IsNotNull(compressorSource, nameof(compressorSource));
            _encoderSettings = encoderSettings; // can be null
            _originalEncoderSelector = originalEncoderSelector;
        }

        /// <summary>
        /// Reads the message.
        /// </summary>
        /// <returns>A message.</returns>
        public CompressedMessage ReadMessage()
        {
            var reader = CreateBinaryReader();
            var stream = reader.BsonStream;

            var messageStartPosition = stream.Position;
            var messageLength = stream.ReadInt32();
            EnsureMessageLengthIsValid(messageLength);
            var requestId = stream.ReadInt32();
            var responseTo = stream.ReadInt32();
            var opcode = (Opcode)stream.ReadInt32();
            EnsureOpcodeIsValid(opcode);
            var originalOpcode = (Opcode)stream.ReadInt32();
            var uncompressedSize = stream.ReadInt32();
            var compressorType = (CompressorType)stream.ReadByte();
            var compressor = _compressorSource.Get(compressorType);

            using (var uncompressedBuffer = new MultiChunkBuffer(new OutputBufferChunkSource(BsonChunkPool.Default)))
            using (var uncompressedStream = new ByteBufferStream(uncompressedBuffer, ownsBuffer: false))
            {
                uncompressedStream.WriteInt32(uncompressedSize + MessageHeaderLength);
                uncompressedStream.WriteInt32(requestId);
                uncompressedStream.WriteInt32(responseTo);
                uncompressedStream.WriteInt32((int)originalOpcode);
                compressor.Decompress(stream, uncompressedStream);
                uncompressedStream.Position = 0;
                uncompressedBuffer.MakeReadOnly();

                var originalMessageEncoderFactory = new BinaryMessageEncoderFactory(uncompressedStream, _encoderSettings, _compressorSource);
                var originalMessageEncoder = _originalEncoderSelector.GetEncoder(originalMessageEncoderFactory);
                var originalMessage = originalMessageEncoder.ReadMessage();

                return new CompressedMessage(originalMessage, null, compressorType);
            }
        }

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void WriteMessage(CompressedMessage message)
        {
            Ensure.IsNotNull(message, nameof(message));

            var writer = CreateBinaryWriter();
            var stream = writer.BsonStream;

            var uncompressedMessageStream = message.OriginalMessageStream;
            var uncompressedMessageLength = uncompressedMessageStream.ReadInt32();
            var requestId = uncompressedMessageStream.ReadInt32();
            var responseTo = uncompressedMessageStream.ReadInt32();
            var originalOpcode = (Opcode)uncompressedMessageStream.ReadInt32();

            var compressorType = message.CompressorType;
            var compressor = _compressorSource.Get(compressorType);

            var messageStartPosition = stream.Position;
            stream.WriteInt32(0); // messageLength
            stream.WriteInt32(requestId);
            stream.WriteInt32(responseTo);
            stream.WriteInt32((int)Opcode.Compressed);
            stream.WriteInt32((int)originalOpcode);
            stream.WriteInt32(uncompressedMessageLength - MessageHeaderLength);
            stream.WriteByte((byte)compressorType);
            compressor.Compress(uncompressedMessageStream, stream);
            stream.BackpatchSize(messageStartPosition);
        }

        MongoDBMessage IMessageEncoder.ReadMessage()
        {
            return ReadMessage();
        }

        void IMessageEncoder.WriteMessage(MongoDBMessage message)
        {
            WriteMessage((CompressedMessage)message);
        }

        // private methods
        private void EnsureMessageLengthIsValid(int messageLength)
        {
            if (messageLength < 0)
            {
                throw new FormatException("Command message length is negative.");
            }
        }

        private void EnsureOpcodeIsValid(Opcode opcode)
        {
            if (opcode != Opcode.Compressed)
            {
                throw new FormatException("Command message opcode is not OP_COMPRESSED.");
            }
        }
    }
}