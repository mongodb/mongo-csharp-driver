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
    public class ListCollectionsOperation : IReadOperation<IReadOnlyList<BsonDocument>>
    {
        #region static
        // static fields
        private static readonly SemanticVersion __versionSupportingListCollectionsCommand = new SemanticVersion(2, 7, 6);
        #endregion

        // fields
        private BsonDocument _criteria;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public ListCollectionsOperation(
            DatabaseNamespace databaseNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, "databaseNamespace");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
        }

        // properties
        public BsonDocument Criteria
        {
            get { return _criteria; }
            set { _criteria = value; }
        }

        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        // methods
        public async Task<IReadOnlyList<BsonDocument>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");

            using (var connectionSource = await binding.GetReadConnectionSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                if (connectionSource.ServerDescription.Version >= __versionSupportingListCollectionsCommand)
                {
                    return await ExecuteUsingCommandAsync(connectionSource, binding.ReadPreference, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await ExecuteUsingQueryAsync(connectionSource, binding.ReadPreference, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<IReadOnlyList<BsonDocument>> ExecuteUsingCommandAsync(IConnectionSourceHandle connectionSource, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            var command = new BsonDocument
            {
                { "listCollections", 1 },
                { "filter", _criteria, _criteria != null }
            };
            var operation = new ReadCommandOperation<BsonDocument>(_databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var response = await operation.ExecuteAsync(connectionSource, readPreference, cancellationToken).ConfigureAwait(false);
            return response["collections"].AsBsonArray.Select(value => (BsonDocument)value).ToList();
        }

        private async Task<IReadOnlyList<BsonDocument>> ExecuteUsingQueryAsync(IConnectionSourceHandle connectionSource, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            // if the criteria includes a comparison to the "name" we must convert the value to a full namespace
            var criteria = _criteria;
            if (criteria != null && criteria.Contains("name"))
            {
                var value = criteria["name"];
                if (!value.IsString)
                {
                    throw new NotSupportedException("Name criteria must be a plain string when connected to a server version less than 2.8.");
                }
                criteria = (BsonDocument)criteria.Clone(); // shallow clone
                criteria["name"] = _databaseNamespace.DatabaseName + "." + value;
            }

            var operation = new FindOperation<BsonDocument>(_databaseNamespace.SystemNamespacesCollection, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Criteria = criteria
            };
            var cursor = await operation.ExecuteAsync(connectionSource, readPreference, cancellationToken).ConfigureAwait(false);

            var collections = new List<BsonDocument>();
            var prefix = _databaseNamespace + ".";

            while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            {
                var batch = cursor.Current;
                foreach (var collection in batch)
                {
                    var name = (string)collection["name"];
                    if (name.StartsWith(prefix))
                    {
                        var collectionName = name.Substring(prefix.Length);
                        if (!collectionName.Contains('$'))
                        {
                            collection["name"] = collectionName; // replace the full namespace with just the collection name
                            collections.Add(collection);
                        }
                    }
                }
            }

            return collections;
        }
    }
}
