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
    public class UnifiedCreateFindCursorOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly BsonDocument _filter;
        private readonly FindOptions<BsonDocument> _findOptions;
        private readonly IClientSessionHandle _session;

        public UnifiedCreateFindCursorOperation(IClientSessionHandle session, IMongoCollection<BsonDocument> collection, BsonDocument filter, FindOptions<BsonDocument> findOptions)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _filter = Ensure.IsNotNull(filter, nameof(filter));
            _findOptions = findOptions; // can be null
            _session = session; // can be null
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _session != null
                    ? _collection.FindSync(_session, _filter, _findOptions)
                    : _collection.FindSync(_filter, _findOptions);
                var enumerator = cursor.ToEnumerable().GetEnumerator();

                return OperationResult.FromCursor(enumerator);
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
                var cursor = _session != null
                    ? await _collection.FindAsync(_session, _filter, _findOptions).ConfigureAwait(false)
                    : await _collection.FindAsync(_filter, _findOptions).ConfigureAwait(false);
                var enumerator = cursor.ToEnumerable().GetEnumerator();

                return OperationResult.FromCursor(enumerator);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCreateFindCursorOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateFindCursorOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateFindCursorOperation Build(string targetDatabaseId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetDatabaseId);

            IClientSessionHandle session = null;
            BsonDocument filter = null;
            var findOptions = new FindOptions<BsonDocument>();

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "filter":
                        filter = argument.Value.AsBsonDocument;
                        break;
                    case "batchSize":
                        findOptions.BatchSize = argument.Value.AsInt32;
                        break;
                    case "session":
                        session = _entityMap.GetSession(argument.Value.ToString());
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedCreateFindCursorOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedCreateFindCursorOperation(session, collection, filter, findOptions);
        }
    }
}
