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
            : base(MessageOpcode.Insert, null, writerSettings)
        {
            _collectionFullName = collectionFullName;
            _checkElementNames = checkElementNames;
            _flags = flags;
        }

        // internal methods
        internal void AddDocument(Type nominalType, object document)
        {
            _lastDocumentStartPosition = Buffer.Position;
            using (var bsonWriter = BsonWriter.Create(Buffer, WriterSettings))
            {
                bsonWriter.CheckElementNames = _checkElementNames;
                BsonSerializer.Serialize(bsonWriter, nominalType, document, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
            BackpatchMessageLength();
        }

        internal byte[] RemoveLastDocument()
        {
            var lastDocumentLength = (int)(Buffer.Position - _lastDocumentStartPosition);
            var lastDocument = new byte[lastDocumentLength];
            Buffer.CopyTo(_lastDocumentStartPosition, lastDocument, 0, lastDocumentLength);
            Buffer.Position = _lastDocumentStartPosition;
            BackpatchMessageLength();

            return lastDocument;
        }

        internal void ResetBatch(byte[] lastDocument)
        {
            Buffer.Position = _firstDocumentStartPosition;
            Buffer.WriteBytes(lastDocument);
            BackpatchMessageLength();
        }

        // protected methods
        protected override void WriteBody()
        {
            Buffer.WriteInt32((int)_flags);
            Buffer.WriteCString(_collectionFullName);
            _firstDocumentStartPosition = Buffer.Position;
            // documents to be added later by calling AddDocument
        }
    }
}
