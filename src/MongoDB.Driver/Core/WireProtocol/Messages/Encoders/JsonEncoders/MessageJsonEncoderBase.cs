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

using System;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    internal abstract class MessageJsonEncoderBase
    {
        // fields
        private readonly MessageEncoderSettings _encoderSettings;
        private readonly TextReader _textReader;
        private readonly TextWriter _textWriter;

        // constructor
        protected MessageJsonEncoderBase(TextReader textReader, TextWriter textWriter, MessageEncoderSettings encoderSettings)
        {
            Ensure.That(textReader != null || textWriter != null, "textReader and textWriter cannot both be null.");
            _textReader = textReader;
            _textWriter = textWriter;
            _encoderSettings = encoderSettings;
        }

        // methods
        public JsonReader CreateJsonReader()
        {
            if (_textReader == null)
            {
                throw new InvalidOperationException("No TextReader was provided.");
            }

            var readerSettings = new JsonReaderSettings();
            return new JsonReader(_textReader, readerSettings);
        }

        public JsonWriter CreateJsonWriter()
        {
            if (_textWriter == null)
            {
                throw new InvalidOperationException("No TextWriter was provided.");
            }

            var writerSettings = new JsonWriterSettings();
            if (_encoderSettings != null)
            {
                writerSettings.Indent = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.Indent, false);
                writerSettings.IndentChars = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.IndentChars, "");
                writerSettings.MaxSerializationDepth = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.MaxSerializationDepth, BsonDefaults.MaxSerializationDepth);
                writerSettings.NewLineChars = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.NewLineChars, "");
                writerSettings.OutputMode = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.OutputMode, JsonOutputMode.Shell);
                writerSettings.ShellVersion = _encoderSettings.GetOrDefault(MessageEncoderSettingsName.ShellVersion, new Version(2, 6, 0));
                writerSettings.SerializationDomain =
                    _encoderSettings.GetOrDefault<IBsonSerializationDomain>(MessageEncoderSettingsName.SerializationDomain, null); //TODO Using null here to find issues faster
            }
            return new JsonWriter(_textWriter, writerSettings);
        }
    }
}
