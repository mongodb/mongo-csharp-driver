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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class ListIndexesOperation : IReadOperation<IEnumerable<BsonDocument>>
    {
        #region static
        // static fields
        private static readonly SemanticVersion __serverVersionSupportingListIndexesCommand = new SemanticVersion(2, 7, 6);
        #endregion

        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public ListIndexesOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _messageEncoderSettings = messageEncoderSettings;
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

        // methods
        public async Task<IEnumerable<BsonDocument>> ExecuteAsync(IReadBinding binding, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");

            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetReadConnectionSourceAsync(slidingTimeout, cancellationToken).ConfigureAwait(false))
            {
                if (connectionSource.ServerDescription.Version >= __serverVersionSupportingListIndexesCommand)
                {
                    return await ExecuteUsingCommandAsync(connectionSource, binding.ReadPreference, slidingTimeout, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await ExecuteUsingQueryAsync(connectionSource, binding.ReadPreference, slidingTimeout, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<IEnumerable<BsonDocument>> ExecuteUsingCommandAsync(IConnectionSourceHandle connectionSource, ReadPreference readPreference, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var databaseNamespace = _collectionNamespace.DatabaseNamespace;
            var command = new BsonDocument("listIndexes", _collectionNamespace.CollectionName);
            var operation = new ReadCommandOperation<BsonDocument>(databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            BsonDocument result;
            try
            {
                result = await operation.ExecuteAsync(connectionSource, readPreference, timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (MongoCommandException ex)
            {
                if (ex.Code == 26)
                {
                    return Enumerable.Empty<BsonDocument>();
                }
                throw;
            }

            return result["indexes"].AsBsonArray.Cast<BsonDocument>();
        }

        private async Task<IEnumerable<BsonDocument>> ExecuteUsingQueryAsync(IConnectionSourceHandle connectionSource, ReadPreference readPreference, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var indexes = new List<BsonDocument>();

            var systemIndexesCollection = _collectionNamespace.DatabaseNamespace.SystemIndexesCollection;
            var criteria = new BsonDocument("ns", _collectionNamespace.FullName);
            var operation = new FindOperation<BsonDocument>(systemIndexesCollection, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Criteria = criteria
            };

            var cursor = await operation.ExecuteAsync(connectionSource, readPreference, timeout, cancellationToken).ConfigureAwait(false);
            while (await cursor.MoveNextAsync().ConfigureAwait(false))
            {
                var batch = cursor.Current;
                foreach (var index in batch)
                {
                    indexes.Add(index);
                }
            }

            return indexes;
        }
    }
}
