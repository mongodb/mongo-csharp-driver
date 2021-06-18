/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedFindOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly FindOptions<BsonDocument> _options;
        private readonly IClientSessionHandle _session;

        public UnifiedFindOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            IClientSessionHandle session,
            FindOptions<BsonDocument> options)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _filter = Ensure.IsNotNull(filter, nameof(filter));
            _options = options; // can be null
            _session = session; // can be null
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _session == null
                    ? _collection.FindSync(_filter, _options, cancellationToken)
                    : _collection.FindSync(_session, _filter, _options, cancellationToken);
                var result = cursor.ToList();

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _session == null
                    ? await _collection.FindAsync(_filter, _options, cancellationToken).ConfigureAwait(false)
                    : await _collection.FindAsync(_session, _filter, _options, cancellationToken).ConfigureAwait(false);
                var result = await cursor.ToListAsync();

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedFindOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedFindOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedFindOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            FilterDefinition<BsonDocument> filter = null;
            FindOptions<BsonDocument> options = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "batchSize":
                        options = options ?? new FindOptions<BsonDocument>();
                        options.BatchSize = argument.Value.AsInt32;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "limit":
                        options = options ?? new FindOptions<BsonDocument>();
                        options.Limit = argument.Value.AsInt32;
                        break;
                    case "session":
                        session = _entityMap.GetSession(argument.Value.AsString);
                        break;
                    case "sort":
                        options = options ?? new FindOptions<BsonDocument>();
                        options.Sort = new BsonDocumentSortDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Invalid FindOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedFindOperation(collection, filter, session, options);
        }
    }
}
