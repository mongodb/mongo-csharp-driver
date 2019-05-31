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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    /// <summary>
    /// Represents a Json encoder for a CompressedMessage.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.Core.WireProtocol.Messages.Encoders.IMessageEncoder" />
    public class CompressedMessageJsonEncoder : MessageJsonEncoderBase, IMessageEncoder
    {
        private readonly MessageEncoderSettings _encoderSettings;
        private readonly IMessageEncoderSelector _originalEncoderSelector;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedMessageJsonEncoder"/> class.
        /// </summary>
        /// <param name="textReader">The text reader.</param>
        /// <param name="textWriter">The text writer.</param>
        /// <param name="originalEncoderSelector">The original encoder selector.</param>
        /// <param name="encoderSettings">The encoder settings.</param>
        public CompressedMessageJsonEncoder(TextReader textReader, TextWriter textWriter, IMessageEncoderSelector originalEncoderSelector, MessageEncoderSettings encoderSettings)
            : base(textReader, textWriter, encoderSettings)
        {
            _originalEncoderSelector = originalEncoderSelector;
            _encoderSettings = encoderSettings;
        }

        // public methods
        /// <summary>
        /// Reads the message.
        /// </summary>
        /// <returns>A message.</returns>
        public CompressedMessage ReadMessage()
        {
            var reader = CreateJsonReader();
            var context = BsonDeserializationContext.CreateRoot(reader);
            var messageDocument = BsonDocumentSerializer.Instance.Deserialize(context);

            var opcode = (Opcode)messageDocument["opcode"].ToInt32();
            if (opcode != Opcode.Compressed)
            {
                throw new FormatException($"Command message invalid opcode: \"{opcode}\".");
            }

            var compressorId = (CompressorType)messageDocument["compressorId"].ToInt32();
            var compressedMessage = messageDocument["compressedMessage"].AsString;

            using (var originalTextReader = new StringReader(compressedMessage))
            {
                var jsonEncoderFactory = new JsonMessageEncoderFactory(originalTextReader, _encoderSettings);
                var originalEncoder = _originalEncoderSelector.GetEncoder(jsonEncoderFactory);
                var originalMessage = originalEncoder.ReadMessage();

                return new CompressedMessage(originalMessage, null, compressorId);
            }
        }

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void WriteMessage(CompressedMessage message)
        {
            Ensure.IsNotNull(message, nameof(message));

            var writer = CreateJsonWriter();

            writer.WriteStartDocument();
            writer.WriteInt32("opcode", (int)Opcode.Compressed);
            writer.WriteInt32("compressorId", (int)message.CompressorType);

            using (var originalWriter = new StringWriter())
            {
                var jsonEncoderFactory = new JsonMessageEncoderFactory(originalWriter, _encoderSettings);
                var originalEncoder = _originalEncoderSelector.GetEncoder(jsonEncoderFactory);
                originalEncoder.WriteMessage(message.OriginalMessage);
                writer.WriteString("compressedMessage", originalWriter.ToString());
            }

            writer.WriteEndDocument();
        }

        /// <inheritdoc />
        MongoDBMessage IMessageEncoder.ReadMessage()
        {
            return ReadMessage();
        }

        /// <inheritdoc />
        void IMessageEncoder.WriteMessage(MongoDBMessage message)
        {
            WriteMessage((CompressedMessage)message);
        }
    }
}