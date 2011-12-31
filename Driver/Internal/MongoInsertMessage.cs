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
    internal class MongoInsertMessage : MongoRequestMessage
    {
        // private fields
        private string _collectionFullName;
        private bool _checkElementNames;
        private InsertFlags _flags;
        private int _firstDocumentStartPosition;
        private int _lastDocumentStartPosition;

        // constructors
        internal MongoInsertMessage(BsonBinaryWriterSettings writerSettings, string collectionFullName, bool checkElementNames, InsertFlags flags)
            : base(MessageOpcode.Insert, null, writerSettings)
        {
            _collectionFullName = collectionFullName;
            _checkElementNames = checkElementNames;
            _flags = flags;
        }

        // internal methods
        internal void AddDocument(Type nominalType, object document)
        {
            _lastDocumentStartPosition = _buffer.Position;
            using (var bsonWriter = BsonWriter.Create(_buffer, _writerSettings))
            {
                bsonWriter.CheckElementNames = _checkElementNames;
                BsonSerializer.Serialize(bsonWriter, nominalType, document, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
            BackpatchMessageLength();
        }

        internal byte[] RemoveLastDocument()
        {
            var lastDocumentLength = (int)(_buffer.Position - _lastDocumentStartPosition);
            var lastDocument = new byte[lastDocumentLength];
            _buffer.CopyTo(_lastDocumentStartPosition, lastDocument, 0, lastDocumentLength);
            _buffer.Position = _lastDocumentStartPosition;
            BackpatchMessageLength();

            return lastDocument;
        }

        internal void ResetBatch(byte[] lastDocument)
        {
            _buffer.Position = _firstDocumentStartPosition;
            _buffer.WriteBytes(lastDocument);
            BackpatchMessageLength();
        }

        // protected methods
        protected override void WriteBody()
        {
            _buffer.WriteInt32((int)_flags);
            _buffer.WriteCString(_collectionFullName);
            _firstDocumentStartPosition = _buffer.Position;
            // documents to be added later by calling AddDocument
        }
    }
}
