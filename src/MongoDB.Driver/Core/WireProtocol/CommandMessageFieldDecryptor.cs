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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    internal class CommandMessageFieldDecryptor
    {
        // private fields
        private readonly IBinaryDocumentFieldDecryptor _documentFieldDecryptor;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public CommandMessageFieldDecryptor(IBinaryDocumentFieldDecryptor documentFieldDecryptor, MessageEncoderSettings messageEncoderSettings)
        {
            _documentFieldDecryptor = documentFieldDecryptor;
            _messageEncoderSettings = messageEncoderSettings;
        }

        // public methods
        public CommandResponseMessage DecryptFields(CommandResponseMessage encryptedResponseMessage, CancellationToken cancellationToken)
        {
            var encryptedDocumentBytes = GetEncryptedDocumentBytes(encryptedResponseMessage);
            var unencryptedDocumentBytes = _documentFieldDecryptor.DecryptFields(encryptedDocumentBytes, cancellationToken);
            return CreateUnencryptedResponseMessage(encryptedResponseMessage, unencryptedDocumentBytes);
        }

        public async Task<CommandResponseMessage> DecryptFieldsAsync(CommandResponseMessage encryptedResponseMessage, CancellationToken cancellationToken)
        {
            var encryptedDocumentBytes = GetEncryptedDocumentBytes(encryptedResponseMessage);
            var unencryptedDocumentBytes = await _documentFieldDecryptor.DecryptFieldsAsync(encryptedDocumentBytes, cancellationToken).ConfigureAwait(false);
            return CreateUnencryptedResponseMessage(encryptedResponseMessage, unencryptedDocumentBytes);
        }

        // private methods
        private CommandResponseMessage CreateUnencryptedResponseMessage(CommandResponseMessage encryptedResponseMessage, byte[] unencryptedDocumentBytes)
        {
            var unencryptedDocument = new RawBsonDocument(unencryptedDocumentBytes);
            var unencryptedSections = new[] { new Type0CommandMessageSection<RawBsonDocument>(unencryptedDocument, RawBsonDocumentSerializer.Instance) };
            var encryptedCommandMessage = encryptedResponseMessage.WrappedMessage;
            var unencryptedCommandMessage = new CommandMessage(
                encryptedCommandMessage.RequestId,
                encryptedCommandMessage.ResponseTo,
                unencryptedSections,
                encryptedCommandMessage.MoreToCome);
            return new CommandResponseMessage(unencryptedCommandMessage);
        }

        private byte[] GetEncryptedDocumentBytes(CommandResponseMessage encryptedResponseMessage)
        {
            var encryptedCommandMessage = encryptedResponseMessage.WrappedMessage;
            var encryptedSections = encryptedCommandMessage.Sections;
            var encryptedType0Section = (Type0CommandMessageSection<RawBsonDocument>)encryptedSections.Single();
            var encryptedDocumentSlice = encryptedType0Section.Document.Slice;
            var encryptedDocumentBytes = new byte[encryptedDocumentSlice.Length];
            encryptedDocumentSlice.GetBytes(0, encryptedDocumentBytes, 0, encryptedDocumentBytes.Length);
            return encryptedDocumentBytes;
        }
    }
}
