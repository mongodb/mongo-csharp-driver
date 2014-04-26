/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Driver.Internal
{
    internal class MongoQueryMessage : MongoRequestMessage
    {
        // private fields
        private readonly string _collectionFullName;
        private readonly QueryFlags _flags;
        private readonly int _maxDocumentSize;
        private readonly int _numberToSkip;
        private readonly int _numberToReturn;
        private readonly IMongoQuery _query;
        private readonly IMongoFields _fields;

        // constructors
        internal MongoQueryMessage(
            BsonBinaryWriterSettings writerSettings,
            string collectionFullName,
            QueryFlags flags,
            int maxDocumentSize,
            int numberToSkip,
            int numberToReturn,
            IMongoQuery query,
            IMongoFields fields)
            : base(MessageOpcode.Query, writerSettings)
        {
            _collectionFullName = collectionFullName;
            _flags = flags;
            _maxDocumentSize = maxDocumentSize;
            _numberToSkip = numberToSkip;
            _numberToReturn = numberToReturn;
            _query = query;
            _fields = fields;
        }

        // internal methods
        internal override void WriteBodyTo(BsonStreamWriter streamWriter)
        {
            using (var bsonWriter = new BsonBinaryWriter(streamWriter.BaseStream, WriterSettings))
            {
                bsonWriter.PushMaxDocumentSize(_maxDocumentSize);
                if (_query == null)
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteEndDocument();
                }
                else
                {
                    BsonSerializer.Serialize(bsonWriter, _query.GetType(), _query, b => b.SerializeIdFirst = true);
                }
                if (_fields != null)
                {
                    BsonSerializer.Serialize(bsonWriter, _fields);
                }
                bsonWriter.PopMaxDocumentSize();
            }
        }

        internal override void WriteHeaderTo(BsonStreamWriter buffer)
        {
            if ((_flags & QueryFlags.Exhaust) != 0)
            {
                throw new NotSupportedException("The Exhaust QueryFlag is not yet supported.");
            }

            base.WriteHeaderTo(buffer);
            buffer.WriteInt32((int)_flags);
            buffer.WriteCString(_collectionFullName);
            buffer.WriteInt32(_numberToSkip);
            buffer.WriteInt32(_numberToReturn);
        }
    }
}
