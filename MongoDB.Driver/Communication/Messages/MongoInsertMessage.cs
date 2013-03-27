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
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Driver.Internal
{
    internal class MongoInsertMessage : MongoRequestMessage
    {
        // private fields
        private string _collectionFullName;
        private bool _checkElementNames;
        private InsertFlags _flags;
        private int _firstDocumentStartPosition;
        private int _lastDocumentStartPosition;

        // constructors
        internal MongoInsertMessage(
            BsonBinaryWriterSettings writerSettings,
            string collectionFullName,
            bool checkElementNames,
            InsertFlags flags)
            : base(MessageOpcode.Insert, writerSettings)
        {
            _collectionFullName = collectionFullName;
            _checkElementNames = checkElementNames;
            _flags = flags;
        }

        // internal methods
        internal void AddDocument(BsonBuffer buffer, Type nominalType, object document)
        {
            _lastDocumentStartPosition = buffer.Position;
            using (var bsonWriter = new BsonBinaryWriter(buffer, false, WriterSettings))
            {
                bsonWriter.CheckElementNames = _checkElementNames;
                BsonSerializer.Serialize(bsonWriter, nominalType, document, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
            BackpatchMessageLength(buffer);
        }

        internal byte[] RemoveLastDocument(BsonBuffer buffer)
        {
            var lastDocumentLength = buffer.Position - _lastDocumentStartPosition;
            buffer.Position = _lastDocumentStartPosition;
            var lastDocument = buffer.ReadBytes(lastDocumentLength);
            buffer.Position = _lastDocumentStartPosition;
            buffer.Length = _lastDocumentStartPosition;
            BackpatchMessageLength(buffer);
            return lastDocument;
        }

        internal void ResetBatch(BsonBuffer buffer, byte[] lastDocument)
        {
            buffer.Position = _firstDocumentStartPosition;
            buffer.Length = _firstDocumentStartPosition;
            buffer.WriteBytes(lastDocument);
            BackpatchMessageLength(buffer);
        }

        // protected methods
        protected override void WriteBody(BsonBuffer buffer)
        {
            buffer.WriteInt32((int)_flags);
            buffer.WriteCString(new UTF8Encoding(false, true), _collectionFullName);
            _firstDocumentStartPosition = buffer.Position;
            // documents to be added later by calling AddDocument
        }
    }
}
