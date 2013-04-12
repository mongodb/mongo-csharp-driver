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

using MongoDB.Bson.IO;

namespace MongoDB.Driver.Operations
{
    internal abstract class DatabaseOperation
    {
        private readonly string _databaseName;
        private readonly string _collectionName;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly BsonBinaryWriterSettings _writerSettings;

        protected DatabaseOperation(
            string databaseName,
            string collectionName,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings)
        {
            _databaseName = databaseName;
            _collectionName = collectionName;
            _readerSettings = (BsonBinaryReaderSettings)readerSettings.FrozenCopy();
            _writerSettings = (BsonBinaryWriterSettings)writerSettings.FrozenCopy();
        }

        protected string CollectionName
        {
            get { return _collectionName; }
        }

        protected string CollectionFullName
        {
            get { return _databaseName + "." + _collectionName; }
        }

        protected string DatabaseName
        {
            get { return _databaseName; }
        }

        protected BsonBinaryReaderSettings GetNodeAdjustedReaderSettings(MongoServerInstance node)
        {
            var readerSettings = _readerSettings.Clone();
            readerSettings.MaxDocumentSize = node.MaxDocumentSize;
            return readerSettings;
        }

        protected BsonBinaryWriterSettings GetNodeAdjustedWriterSettings(MongoServerInstance node)
        {
            var writerSettings = _writerSettings.Clone();
            writerSettings.MaxDocumentSize = node.MaxDocumentSize;
            return writerSettings;
        }
    }
}
