/*
 * Copyright 2019–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.IO;

namespace MongoDB.Driver.Encryption.Tests
{
    class BsonUtil
    {
        public static BsonDocument ToDocument(Binary bin)
        {
            MemoryStream stream = new MemoryStream(bin.ToArray());
            using (var jsonReader = new BsonBinaryReader(stream))
            {
                var context = BsonDeserializationContext.CreateRoot(jsonReader);
                return BsonDocumentSerializer.Instance.Deserialize(context);
            }
        }

        public static byte[] ToBytes(BsonDocument doc)
        {
            BsonBinaryWriterSettings settings = new BsonBinaryWriterSettings();
            return doc.ToBson(null, settings);
        }

        public static BsonDocument Concat(BsonDocument doc1, BsonDocument doc2)
        {
            BsonDocument dest = new BsonDocument();
            BsonDocumentWriter writer = new BsonDocumentWriter(dest);
            var context = BsonSerializationContext.CreateRoot(writer);

            writer.WriteStartDocument();

            foreach (var field in doc1)
            {
                writer.WriteName(field.Name);
                BsonValueSerializer.Instance.Serialize(context, field.Value);
            }

            foreach (var field in doc2)
            {
                writer.WriteName(field.Name);
                BsonValueSerializer.Instance.Serialize(context, field.Value);
            }

            writer.WriteEndDocument();
            return writer.Document;
        }


        public static BsonDocument FromJSON(string str)
        {
            var jsonReaderSettings = new JsonReaderSettings();
            using (var jsonReader = new JsonReader(str, jsonReaderSettings))
            {
                var context = BsonDeserializationContext.CreateRoot(jsonReader);
                return BsonDocumentSerializer.Instance.Deserialize(context);
            }
        }
    }
}
