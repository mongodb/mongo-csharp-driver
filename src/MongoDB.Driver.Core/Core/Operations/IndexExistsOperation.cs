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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class IndexExistsOperation : IReadOperation<bool>
    {
        // fields
        private CollectionNamespace _collectionNamespace;
        private string _indexName;
        private MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public IndexExistsOperation(
            CollectionNamespace collectionNamespace,
            BsonDocument keys,
            MessageEncoderSettings messageEncoderSettings)
            : this(collectionNamespace, CreateIndexOperation.GetDefaultIndexName(keys), messageEncoderSettings)
        {
        }

        public IndexExistsOperation(
            CollectionNamespace collectionNamespace,
            string indexName,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _indexName = Ensure.IsNotNullOrEmpty(indexName, "indexName");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
            set { _collectionNamespace = Ensure.IsNotNull(value, "value"); }
        }

        public string IndexName
        {
            get { return _indexName; }
            set { _indexName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        // methods
        private BsonDocument CreateFilter()
        {
            return new BsonDocument
            {
                { "name", _indexName },
                { "ns", _collectionNamespace.FullName }
            };
        }

        public async Task<bool> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var filter = CreateFilter();
            var operation = new CountOperation(_collectionNamespace.DatabaseNamespace.SystemIndexesCollection, _messageEncoderSettings)
            {
                Criteria = filter
            };
            var count = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            return count != 0;
        }
    }
}
