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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders
{
    public static class MessageEncoderSettingsName
    {
        // encoder settings used by the binary encoders
        public const string FixOldBinarySubTypeOnInput = "FixOldBinarySubTypeOnInput";
        public const string FixOldBinarySubTypeOnOutput = "FixOldBinarySubTypeOnOutput";
        public const string FixOldDateTimeMaxValueOnInput = "FixOldDateTimeMaxValueOnInput";
        public const string GuidRepresentation = "GuidRepresentation";
        public const string MaxDocumentSize = "MaxDocumentSize";
        public const string MaxSerializationDepth = "MaxSerializationDepth";
        public const string ReadEncoding = "ReadEncoding";
        public const string WriteEncoding = "WriteEncoding";

        // additional encoder settings used by the JSON encoders
        public const string Indent = "Indent";
        public const string IndentChars = "IndentChars";
        public const string NewLineChars = "NewLineChars";
        public const string OutputMode = "OutputMode";
        public const string ShellVersion = "ShellVersion";

        // other encoders (if any) might use additional settings
    }

    public class MessageEncoderSettings : IEnumerable<KeyValuePair<string, object>>
    {
        // fields
        private readonly Dictionary<string, object> _settings = new Dictionary<string, object>();

        // methods
        public MessageEncoderSettings Add<T>(string name, T value)
        {
            _settings.Add(name, value);
            return this;
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
