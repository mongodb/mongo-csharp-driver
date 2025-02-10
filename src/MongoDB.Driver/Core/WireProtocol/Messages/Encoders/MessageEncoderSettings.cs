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
using System.Collections;
using System.Collections.Generic;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders
{
    internal static class MessageEncoderSettingsName
    {
        // encoder settings used by the binary encoders
        public const string BinaryDocumentFieldDecryptor = nameof(BinaryDocumentFieldDecryptor);
        public const string BinaryDocumentFieldEncryptor = nameof(BinaryDocumentFieldEncryptor);
        [Obsolete("Configure serializers instead.")]
        public const string GuidRepresentation = nameof(GuidRepresentation);
        public const string MaxDocumentSize = nameof(MaxDocumentSize);
        public const string MaxMessageSize = nameof(MaxMessageSize);
        public const string MaxSerializationDepth = nameof(MaxSerializationDepth);
        public const string MaxWireDocumentSize = nameof(MaxWireDocumentSize);
        public const string ReadEncoding = nameof(ReadEncoding);
        public const string WriteEncoding = nameof(WriteEncoding);

        // additional encoder settings used by the JSON encoders
        public const string Indent = nameof(Indent);
        public const string IndentChars = nameof(IndentChars);
        public const string NewLineChars = nameof(NewLineChars);
        public const string OutputMode = nameof(OutputMode);
        public const string ShellVersion = nameof(ShellVersion);

        // other encoders (if any) might use additional settings
        public const string SerializationDomain = nameof(SerializationDomain);
    }

    internal sealed class MessageEncoderSettings : IEnumerable<KeyValuePair<string, object>>
    {
        // fields
        private readonly Dictionary<string, object> _settings = new();

        // methods
        public MessageEncoderSettings Add<T>(string name, T value)
        {
            _settings.Add(name, value);
            return this;
        }

        public MessageEncoderSettings Clone()
        {
            var clone = new MessageEncoderSettings();
            foreach (var key in _settings.Keys)
            {
                clone.Add(key, _settings[key]);
            }
            return clone;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _settings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T GetOrDefault<T>(string name, T defaultValue)
        {
            object value;
            if (_settings.TryGetValue(name, out value))
            {
                return (T)value;
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
