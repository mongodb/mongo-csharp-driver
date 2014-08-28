/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class FindAndModifyOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private CollectionNamespace _collectionNamespace;
        private FindAndModifyDocumentVersion? _documentVersionReturned;
        private BsonDocument _fields;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private BsonDocument _query;
        private BsonDocument _sort;
        private BsonDocument _update;
        private bool? _upsert;

        // constructors
        public FindAndModifyOperation(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument update,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _query = query;
            _update = Ensure.IsNotNull(update, "update");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
            set { _collectionNamespace = Ensure.IsNotNull(value, "value"); }
        }

        public FindAndModifyDocumentVersion? DocumentVersionReturned
        {
            get { return _documentVersionReturned; }
            set { _documentVersionReturned = value; }
        }

        public BsonDocument Fields
        {
            get { return _fields; }
            set { _fields = value; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public BsonDocument Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        public BsonDocument Update
        {
            get { return _update; }
            set { _update = Ensure.IsNotNull(value, "value"); }
        }

        public bool? Upsert
        {
            get { return _upsert; }
            set { _upsert = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "findAndModify", _collectionNamespace.CollectionName },
                { "query", _query, _query != null },
                { "sort", _sort, _sort != null },
                { "update", _update },
                { "new", () => _documentVersionReturned.Value == FindAndModifyDocumentVersion.Modified, _documentVersionReturned.HasValue },
                { "field", _fields, _fields != null },
                { "upsert", () => _upsert.Value, _upsert.HasValue },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var result = await ExecuteCommandAsync(binding, timeout, cancellationToken);
            return result["value"].AsBsonDocument;
        }

        public async Task<BsonDocument> ExecuteCommandAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation(_collectionNamespace.DatabaseNamespace, command, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}
