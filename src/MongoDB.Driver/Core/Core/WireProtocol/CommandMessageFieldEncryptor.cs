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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    internal class CommandMessageFieldEncryptor
    {
        // private fields
        private readonly byte[] _buffer = new byte[1024];
        private readonly IBinaryCommandFieldEncryptor _commandFieldEncryptor;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public CommandMessageFieldEncryptor(IBinaryCommandFieldEncryptor commandFieldEncryptor, MessageEncoderSettings messageEncoderSettings)
        {
            _commandFieldEncryptor = commandFieldEncryptor;
            _messageEncoderSettings = messageEncoderSettings;
        }

        // public static methods
        public CommandRequestMessage EncryptFields(string databaseName, CommandRequestMessage unencryptedRequestMessage, CancellationToken cancellationToken)
        {
            var unencryptedCommandBytes = GetUnencryptedCommandBytes(unencryptedRequestMessage);
            var encryptedCommandBytes = _commandFieldEncryptor.EncryptFields(databaseName, unencryptedCommandBytes, cancellationToken);
            return CreateEncryptedRequestMessage(unencryptedRequestMessage, encryptedCommandBytes);
        }

        public async Task<CommandRequestMessage> EncryptFieldsAsync(string databaseName, CommandRequestMessage unencryptedRequestMessage, CancellationToken cancellationToken)
        {
            var unencryptedCommandBytes = GetUnencryptedCommandBytes(unencryptedRequestMessage);
            var encryptedCommandBytes = await _commandFieldEncryptor.EncryptFieldsAsync(databaseName, unencryptedCommandBytes, cancellationToken).ConfigureAwait(false);
            return CreateEncryptedRequestMessage(unencryptedRequestMessage, encryptedCommandBytes);
        }

        // private static methods
        private byte[] CombineCommandMessageSectionsIntoSingleDocument(Stream stream)
        {
            using (var inputStream = new BsonStreamAdapter(stream, ownsStream: false))
            using (var memoryStream = new MemoryStream())
            using (var outputStream = new BsonStreamAdapter(memoryStream, ownsStream: false))
            {
                var messageStartPosition = inputStream.Position;
                var messageLength = inputStream.ReadInt32();
                var messageEndPosition = messageStartPosition + messageLength;
                var requestId = inputStream.ReadInt32();
                var responseTo = inputStream.ReadInt32();
                var opcode = inputStream.ReadInt32();
                var flags = (OpMsgFlags)inputStream.ReadInt32();
                if (flags.HasFlag(OpMsgFlags.ChecksumPresent))
                {
                    messageEndPosition -= 4; // ignore checksum
                }

                CopyType0Section(inputStream, outputStream);
                outputStream.Position -= 1;
                while (inputStream.Position < messageEndPosition)
                {
                    CopyType1Section(inputStream, outputStream);
                }
                outputStream.WriteByte(0);
                outputStream.BackpatchSize(0);

                return memoryStream.ToArray();
            }
        }

        private void CopyBsonDocument(BsonStream inputStream, BsonStream outputStream)
        {
            var documentLength = inputStream.ReadInt32();
            inputStream.Position -= 4;
            CopyBytes(inputStream, outputStream, documentLength);
        }

        private void CopyBytes(BsonStream inputStream, BsonStream outputStream, int count)
        {
            while (count > 0)
            {
                var chunkSize = Math.Min(count, _buffer.Length);
                inputStream.ReadBytes(_buffer, 0, chunkSize);
                outputStream.WriteBytes(_buffer, 0, chunkSize);
                count -= chunkSize;
            }
        }

        private void CopyType0Section(BsonStream inputStream, BsonStream outputStream)
        {
            var payloadType = (PayloadType)inputStream.ReadByte();
            if (payloadType != PayloadType.Type0)
            {
                throw new FormatException("Expected first section to be of type 0.");
            }

            CopyBsonDocument(inputStream, outputStream);
        }

        private void CopyType1Section(BsonStream inputStream, BsonStream outputStream)
        {
            var payloadType = (PayloadType)inputStream.ReadByte();
            if (payloadType != PayloadType.Type1)
            {
                throw new FormatException("Expected subsequent sections to be of type 1.");
            }

            var sectionStartPosition = inputStream.Position;
            var sectionSize = inputStream.ReadInt32();
            var sectionEndPosition = sectionStartPosition + sectionSize;
            var identifier = inputStream.ReadCString(Utf8Encodings.Lenient);

            outputStream.WriteByte((byte)BsonType.Array);
            outputStream.WriteCString(identifier);
            var arrayStartPosition = outputStream.Position;
            outputStream.WriteInt32(0); // array length will be backpatched
            var index = 0;
            while (inputStream.Position < sectionEndPosition)
            {
                outputStream.WriteByte((byte)BsonType.Document);
                outputStream.WriteCString(index.ToString());
                CopyBsonDocument(inputStream, outputStream);
                index++;
            }
            outputStream.WriteByte(0);
            outputStream.BackpatchSize(arrayStartPosition);
        }

        private CommandRequestMessage CreateEncryptedRequestMessage(CommandRequestMessage unencryptedRequestMessage, byte[] encryptedDocumentBytes)
        {
            var encryptedDocument = new RawBsonDocument(encryptedDocumentBytes);
            var encryptedSections = new[] { new Type0CommandMessageSection<RawBsonDocument>(encryptedDocument, RawBsonDocumentSerializer.Instance) };
            var unencryptedCommandMessage = unencryptedRequestMessage.WrappedMessage;
            var encryptedCommandMessage = new CommandMessage(
                unencryptedCommandMessage.RequestId,
                unencryptedCommandMessage.ResponseTo,
                encryptedSections,
                unencryptedCommandMessage.MoreToCome);
            return new CommandRequestMessage(encryptedCommandMessage, unencryptedRequestMessage.ShouldBeSent);
        }

        private byte[] GetUnencryptedCommandBytes(CommandRequestMessage unencryptedRequestMessage)
        {
            using (var stream = new MemoryStream())
            {
                WriteUnencryptedRequestMessageToStream(stream, unencryptedRequestMessage);
                stream.Position = 0;
                return CombineCommandMessageSectionsIntoSingleDocument(stream);
            }
        }

        private void WriteUnencryptedRequestMessageToStream(
            Stream stream,
            CommandRequestMessage unencryptedRequestMessage)
        {
            var clonedMessageEncoderSettings = _messageEncoderSettings.Clone();
            var encoderFactory = new BinaryMessageEncoderFactory(stream, clonedMessageEncoderSettings, compressorSource: null);
            var encoder = encoderFactory.GetCommandRequestMessageEncoder();
            encoder.WriteMessage(unencryptedRequestMessage);
        }
    }
}
