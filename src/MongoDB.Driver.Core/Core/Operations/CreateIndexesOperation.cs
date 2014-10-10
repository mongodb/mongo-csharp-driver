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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateIndexesOperation : IWriteOperation<BsonDocument>
    {
        #region static
        // static fields
        private static readonly SemanticVersion __serverVersionSupportingCreateIndexesCommand = new SemanticVersion(2, 7, 6);
        #endregion

        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IEnumerable<CreateIndexRequest> _requests;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        public CreateIndexesOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<CreateIndexRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _requests = Ensure.IsNotNull(requests, "requests").ToList();
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public IEnumerable<CreateIndexRequest> Requests
        {
            get { return _requests; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        internal BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray(_requests.Select(request => request.CreateIndexDocument())) }
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(slidingTimeout, cancellationToken))
            {
                if (connectionSource.ServerDescription.Version >= __serverVersionSupportingCreateIndexesCommand)
                {
                    return await ExecuteUsingCommandAsync(connectionSource, slidingTimeout, cancellationToken);
                }
                else
                {
                    return await ExecuteUsingInsertAsync(connectionSource, slidingTimeout, cancellationToken);
                }
            }
        }

        private Task<BsonDocument> ExecuteUsingCommandAsync(IConnectionSourceHandle connectionSource, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var databaseNamespace = _collectionNamespace.DatabaseNamespace;
            var command = CreateCommand();
            var resultSerializer = BsonDocumentSerializer.Instance;
            var operation = new WriteCommandOperation<BsonDocument>(databaseNamespace, command, resultSerializer, _messageEncoderSettings);
            return operation.ExecuteAsync(connectionSource, timeout, cancellationToken);
        }

        private async Task<BsonDocument> ExecuteUsingInsertAsync(IConnectionSourceHandle connectionSource, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            var systemIndexesCollection = _collectionNamespace.DatabaseNamespace.SystemIndexesCollection;

            foreach (var createIndexRequest in _requests)
            {
                var document = createIndexRequest.CreateIndexDocument();
                document.InsertAt(0, new BsonElement("ns", _collectionNamespace.FullName));
                var documentSource = new BatchableSource<BsonDocument>(new[] { document });
                var operation = new InsertOpcodeOperation(systemIndexesCollection, documentSource, _messageEncoderSettings);
                var result = await operation.ExecuteAsync(connectionSource, slidingTimeout, cancellationToken);
            }

            return new BsonDocument("ok", 1);
        }
    }
}
