/* Copyright 2010-present MongoDB Inc.
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

using System.IO;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    internal abstract class MessageBinaryEncoderBase
    {
        // fields
        private readonly MessageEncoderSettings _encoderSettings;
        private readonly Stream _stream;

        // constructor
        protected MessageBinaryEncoderBase(Stream stream, MessageEncoderSettings encoderSettings)
        {
            _stream = Ensure.IsNotNull(stream, nameof(stream));
            _encoderSettings = encoderSettings;
        }

        // properties
        protected UTF8Encoding Encoding
        {
            get
            {
                if (_encoderSettings == null)
                {
                    return Utf8Encodings.Strict;
                }
                else
                {
                    return _encoderSettings.GetOrDefault(MessageEncoderSettingsName.ReadEncoding, Utf8Encodings.Strict);
                }
            }
        }

        protected bool IsEncryptionConfigured
        {
            get
            {
                return _encoderSettings?.GetOrDefault<IBinaryCommandFieldEncryptor>(MessageEncoderSettingsName.BinaryDocumentFieldEncryptor, null) != null;
            }
        }

        protected int? MaxDocumentSize
        {
            get
            {
                return _encoderSettings?.GetOrDefault<int?>(MessageEncoderSettingsName.MaxDocumentSize, null);
            }
        }

        protected int? MaxMessageSize
        {
            get
            {
                return _encoderSettings?.GetOrDefault<int?>(MessageEncoderSettingsName.MaxMessageSize, null);
            }
        }

        protected int? MaxWireDocumentSize
        {
            get
            {
                return _encoderSettings?.GetOrDefault<int?>(MessageEncoderSettingsName.MaxWireDocumentSize, null);
            }
        }

        // methods
        public BsonBinaryReader CreateBinaryReader()
        {
            var readerSettings = new BsonBinaryReaderSettings();
            if (_encoderSettings != null)
            {
                readerSettings.Encoding = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.ReadEncoding, readerSettings.Encoding);
                readerSettings.MaxDocumentSize = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.MaxDocumentSize, readerSettings.MaxDocumentSize);
            }
            return new BsonBinaryReader(_stream, readerSettings);
        }

        public BsonBinaryWriter CreateBinaryWriter()
        {
            var writerSettings = new BsonBinaryWriterSettings();
            if (_encoderSettings != null)
            {
                writerSettings.Encoding = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.WriteEncoding, writerSettings.Encoding);
                writerSettings.MaxDocumentSize = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.MaxDocumentSize, writerSettings.MaxDocumentSize);
                writerSettings.MaxSerializationDepth = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.MaxSerializationDepth, writerSettings.MaxSerializationDepth);
                writerSettings.SerializationDomain =
                    _encoderSettings.GetOrDefault<IBsonSerializationDomain>(MessageEncoderSettingsName.SerializationDomain, null); //TODO Using null here to find issues faster
            }
            return new BsonBinaryWriter(_stream, writerSettings);
        }
    }
}
