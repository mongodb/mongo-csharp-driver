/* Copyright 2021-present MongoDB Inc.
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
    public class UnifiedListIndexesOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ListIndexesOptions _listIndexesOptions;
        private readonly IClientSessionHandle _session;

        public UnifiedListIndexesOperation(
            IMongoCollection<BsonDocument> collection,
            ListIndexesOptions listIndexesOptions,
            IClientSessionHandle session)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _listIndexesOptions = listIndexesOptions; // can be null
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                using var cursor = _session == null
                    ? _collection.Indexes.List(_listIndexesOptions, cancellationToken)
                    : _collection.Indexes.List(_session, _listIndexesOptions, cancellationToken);

                var indexes = cursor.ToList(cancellationToken);

                return OperationResult.FromResult(new BsonArray(indexes));
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var cursor = _session == null
                    ? await _collection.Indexes.ListAsync(_listIndexesOptions, cancellationToken)
                    : await _collection.Indexes.ListAsync(_session, _listIndexesOptions, cancellationToken);

                var indexes = await cursor.ToListAsync(cancellationToken);

                return OperationResult.FromResult(new BsonArray(indexes));
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }
    }

    public class UnifiedListIndexesOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedListIndexesOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedListIndexesOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            var listIndexesOptions = new ListIndexesOptions();
            IClientSessionHandle session = null;

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    switch (argument.Name)
                    {
                        case "batchSize":
                            listIndexesOptions.BatchSize = argument.Value.ToInt32();
                            break;
                        case "session":
                            session = _entityMap.Sessions[argument.Value.AsString];
                            break;
                        default:
                            throw new FormatException($"Invalid {nameof(UnifiedListIndexesOperation)} argument name: '{argument.Name}'.");
                    }
                }
            }

            return new UnifiedListIndexesOperation(collection, listIndexesOptions, session);
        }
    }
}
