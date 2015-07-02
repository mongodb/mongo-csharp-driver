/* Copyright 2013-2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Core.Helpers
{
    public static class RawBsonDocumentHelper
    {
        public static RawBsonDocument FromBsonDocument(BsonDocument document)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var bsonWriter = new BsonBinaryWriter(memoryStream, BsonBinaryWriterSettings.Defaults))
                {
                    var context = BsonSerializationContext.CreateRoot(bsonWriter);
                    BsonDocumentSerializer.Instance.Serialize(context, document);
                }
                return new RawBsonDocument(memoryStream.ToArray());
            }
        }

        public static RawBsonDocument FromJson(string json)
        {
            return FromBsonDocument(BsonDocument.Parse(json));
        }
    }
}