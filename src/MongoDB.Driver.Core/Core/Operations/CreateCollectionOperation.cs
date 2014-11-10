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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateCollectionOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private bool? _autoIndexId;
        private bool? _capped;
        private readonly CollectionNamespace _collectionNamespace;
        private long? _maxDocuments;
        private long? _maxSize;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private bool? _usePowerOf2Sizes;

        // constructors
        public CreateCollectionOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public bool? AutoIndexId
        {
            get { return _autoIndexId; }
            set { _autoIndexId = value; }
        }

        public bool? Capped
        {
            get { return _capped; }
            set { _capped = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public long? MaxDocuments
        {
            get { return _maxDocuments; }
            set { _maxDocuments = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public long? MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public bool? UsePowerOf2Sizes
        {
            get { return _usePowerOf2Sizes; }
            set { _usePowerOf2Sizes = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "capped", () => _capped.Value, _capped.HasValue },
                { "autoIndexId", () => _autoIndexId.Value, _autoIndexId.HasValue },
                { "size", () => _maxSize.Value, _maxSize.HasValue },
                { "max", () => _maxDocuments.Value, _maxDocuments.HasValue },
                { "flags", () => _usePowerOf2Sizes.Value ? 1 : 0, _usePowerOf2Sizes.HasValue}
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
        }
    }
}
