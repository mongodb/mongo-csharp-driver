/* Copyright 2010-2011 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Driver.Internal
{
    internal class MongoQueryMessage : MongoRequestMessage
    {
        // private fields
        private string collectionFullName;
        private QueryFlags flags;
        private int numberToSkip;
        private int numberToReturn;
        private IMongoQuery query;
        private IMongoFields fields;

        // constructors
        internal MongoQueryMessage(BsonBinaryWriterSettings writerSettings, string collectionFullName, QueryFlags flags, int numberToSkip, int numberToReturn, IMongoQuery query, IMongoFields fields)
            : this(null, writerSettings, collectionFullName, flags, numberToSkip, numberToReturn, query, fields)
        {
        }

        internal MongoQueryMessage(BsonBuffer buffer, BsonBinaryWriterSettings writerSettings, string collectionFullName, QueryFlags flags, int numberToSkip, int numberToReturn, IMongoQuery query, IMongoFields fields)
            : base(MessageOpcode.Query, buffer, writerSettings)
        {
            this.collectionFullName = collectionFullName;
            this.flags = flags;
            this.numberToSkip = numberToSkip;
            this.numberToReturn = numberToReturn;
            this.query = query;
            this.fields = fields;
        }

        // protected methods
        protected override void WriteBody()
        {
            buffer.WriteInt32((int)flags);
            buffer.WriteCString(collectionFullName);
            buffer.WriteInt32(numberToSkip);
            buffer.WriteInt32(numberToReturn);

            using (var bsonWriter = BsonWriter.Create(buffer, writerSettings))
            {
                if (query == null)
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteEndDocument();
                }
                else
                {
                    BsonSerializer.Serialize(bsonWriter, query.GetType(), query, DocumentSerializationOptions.SerializeIdFirstInstance);
                }
                if (fields != null)
                {
                    BsonSerializer.Serialize(bsonWriter, fields);
                }
            }
        }
    }
}
