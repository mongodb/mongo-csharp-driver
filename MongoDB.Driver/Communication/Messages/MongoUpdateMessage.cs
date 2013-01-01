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

namespace MongoDB.Driver.Internal
{
    internal class MongoUpdateMessage : MongoRequestMessage
    {
        // private fields
        private string _collectionFullName;
        private bool _checkUpdateDocument;
        private UpdateFlags _flags;
        private IMongoQuery _query;
        private IMongoUpdate _update;

        // constructors
        internal MongoUpdateMessage(
            BsonBinaryWriterSettings writerSettings,
            string collectionFullName,
            bool checkUpdateDocument,
            UpdateFlags flags,
            IMongoQuery query,
            IMongoUpdate update)
            : base(MessageOpcode.Update, null, writerSettings)
        {
            _collectionFullName = collectionFullName;
            _checkUpdateDocument = checkUpdateDocument;
            _flags = flags;
            _query = query;
            _update = update;
        }

        // protected methods
        protected override void WriteBody()
        {
            Buffer.WriteInt32(0); // reserved
            Buffer.WriteCString(_collectionFullName);
            Buffer.WriteInt32((int)_flags);

            using (var bsonWriter = BsonWriter.Create(Buffer, WriterSettings))
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
                bsonWriter.CheckUpdateDocument = _checkUpdateDocument;
                BsonSerializer.Serialize(bsonWriter, _update.GetType(), _update, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
        }
    }
}
