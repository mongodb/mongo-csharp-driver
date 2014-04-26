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

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Driver.Internal
{
    internal class MongoUpdateMessage : MongoRequestMessage
    {
        // private fields
        private readonly string _collectionFullName;
        private readonly bool _checkUpdateDocument;
        private readonly UpdateFlags _flags;
        private readonly int _maxDocumentSize;
        private readonly IMongoQuery _query;
        private readonly IMongoUpdate _update;

        // constructors
        internal MongoUpdateMessage(
            BsonBinaryWriterSettings writerSettings,
            string collectionFullName,
            bool checkUpdateDocument,
            UpdateFlags flags,
            int maxDocumentSize,
            IMongoQuery query,
            IMongoUpdate update)
            : base(MessageOpcode.Update, writerSettings)
        {
            _collectionFullName = collectionFullName;
            _checkUpdateDocument = checkUpdateDocument;
            _flags = flags;
            _maxDocumentSize = maxDocumentSize;
            _query = query;
            _update = update;
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
                    BsonSerializer.Serialize(bsonWriter, _query.GetType(), _query, c => c.SerializeIdFirst = true);
                }
                bsonWriter.CheckUpdateDocument = _checkUpdateDocument;
                BsonSerializer.Serialize(bsonWriter, _update.GetType(), _update, c => c.SerializeIdFirst = true);
                bsonWriter.PopMaxDocumentSize();
            }
        }

        internal override void WriteHeaderTo(BsonStreamWriter streamWriter)
        {
            base.WriteHeaderTo(streamWriter);
            streamWriter.WriteInt32(0); // reserved
            streamWriter.WriteCString(_collectionFullName);
            streamWriter.WriteInt32((int)_flags);
        }
    }
}
