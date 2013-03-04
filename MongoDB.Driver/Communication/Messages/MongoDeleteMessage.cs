/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using System.Text;

namespace MongoDB.Driver.Internal
{
    internal class MongoDeleteMessage : MongoRequestMessage
    {
        // private fields
        private string _collectionFullName;
        private RemoveFlags _flags;
        private IMongoQuery _query;

        // constructors
        internal MongoDeleteMessage(
            BsonBinaryWriterSettings writerSettings,
            string collectionFullName,
            RemoveFlags flags,
            IMongoQuery query)
            : base(MessageOpcode.Delete, writerSettings)
        {
            _collectionFullName = collectionFullName;
            _flags = flags;
            _query = query;
        }

        // protected methods
        protected override void WriteBody(BsonBuffer buffer)
        {
            buffer.WriteInt32(0); // reserved
            buffer.WriteCString(new UTF8Encoding(false, true), _collectionFullName);
            buffer.WriteInt32((int)_flags);

            using (var bsonWriter = new BsonBinaryWriter(buffer, false, WriterSettings))
            {
                if (_query == null)
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteEndDocument();
                }
                else
                {
                    BsonSerializer.Serialize(bsonWriter, _query.GetType(), _query, DocumentSerializationOptions.SerializeIdFirstInstance);
                }
            }
        }
    }
}
