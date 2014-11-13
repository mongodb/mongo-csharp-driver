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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public abstract class MessageBinaryEncoderBase
    {
        // fields
        private readonly MessageEncoderSettings _encoderSettings;
        private readonly Stream _stream;

        // constructor
        protected MessageBinaryEncoderBase(Stream stream, MessageEncoderSettings encoderSettings)
        {
            _stream = Ensure.IsNotNull(stream, "stream");
            _encoderSettings = encoderSettings;
        }

        // methods
        public BsonBinaryReader CreateBinaryReader()
        {
            var readerSettings = new BsonBinaryReaderSettings();
            if (_encoderSettings != null)
            {
                readerSettings.Encoding = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.ReadEncoding, readerSettings.Encoding);
                readerSettings.FixOldBinarySubTypeOnInput = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.FixOldBinarySubTypeOnInput, readerSettings.FixOldBinarySubTypeOnInput);
                readerSettings.FixOldDateTimeMaxValueOnInput = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.FixOldBinarySubTypeOnOutput, readerSettings.FixOldDateTimeMaxValueOnInput);
                readerSettings.GuidRepresentation = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.GuidRepresentation, readerSettings.GuidRepresentation);
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
                writerSettings.FixOldBinarySubTypeOnOutput = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.FixOldBinarySubTypeOnOutput, writerSettings.FixOldBinarySubTypeOnOutput);
                writerSettings.GuidRepresentation = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.GuidRepresentation, writerSettings.GuidRepresentation);
                writerSettings.MaxDocumentSize = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.MaxDocumentSize, writerSettings.MaxDocumentSize);
                writerSettings.MaxSerializationDepth = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.MaxSerializationDepth, writerSettings.MaxSerializationDepth);
            }
            return new BsonBinaryWriter(_stream, writerSettings);
        }
    }
}
