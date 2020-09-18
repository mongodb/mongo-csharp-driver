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

using System.Collections;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq3.Misc
{
    public static class SerializationHelper
    {
        public static BsonValue SerializeValue(IBsonSerializer serializer, object value)
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteName("_v");
                var context = BsonSerializationContext.CreateRoot(writer);
                serializer.Serialize(context, value);
                writer.WriteEndDocument();
            }
            return document["_v"];
        }

        public static BsonArray SerializeValues(IBsonSerializer itemSerializer, IEnumerable values)
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteName("_v");
                writer.WriteStartArray();
                var context = BsonSerializationContext.CreateRoot(writer);
                foreach(var value in values)
                {
                    itemSerializer.Serialize(context, value);
                }
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            return document["_v"].AsBsonArray;
        }
    }
}
