/* Copyright 2018-present MongoDB Inc.
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

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.IO;
using Xunit.Abstractions;

namespace MongoDB.Bson.TestHelpers.JsonDrivenTests
{
    public class JsonDrivenTestCase : IXunitSerializable
    {
        // private fields
        private string _name;
        private BsonDocument _shared;
        private BsonDocument _test;

        // public constructors
        public JsonDrivenTestCase()
        {
        }

        public JsonDrivenTestCase(string name, BsonDocument shared, BsonDocument test)
        {
            _name = name;
            _shared = shared;
            _test = test;
        }

        // public properties
        public string Name => _name;

        public BsonDocument Shared => _shared;

        public BsonDocument Test => _test;

        // public methods
        public void Deserialize(IXunitSerializationInfo info)
        {
            _name = info.GetValue<string>(nameof(_name));
            _shared = DeserializeBsonDocument(info.GetValue<string>(nameof(_shared)));
            _test = DeserializeBsonDocument(info.GetValue<string>(nameof(_test)));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(_name), _name);
            info.AddValue(nameof(_shared), SerializeBsonDocument(_shared));
            info.AddValue(nameof(_test), SerializeBsonDocument(_test));
        }

        public override string ToString()
        {
            return _name;
        }

        // private methods
        private BsonDocument DeserializeBsonDocument(string value)
        {
            var jsonReaderSettings = new JsonReaderSettings { GuidRepresentation = GuidRepresentation.Unspecified };
            using (var jsonReader = new JsonReader(value, jsonReaderSettings))
            {
                var context = BsonDeserializationContext.CreateRoot(jsonReader);
                return BsonDocumentSerializer.Instance.Deserialize(context);
            }
        }

        private string SerializeBsonDocument(BsonDocument value)
        {
            var jsonWriterSettings = new JsonWriterSettings { GuidRepresentation = GuidRepresentation.Unspecified };
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter, jsonWriterSettings))
            {
                var context = BsonSerializationContext.CreateRoot(jsonWriter);
                BsonDocumentSerializer.Instance.Serialize(context, value);
                return stringWriter.ToString();
            }
        }
    }
}
