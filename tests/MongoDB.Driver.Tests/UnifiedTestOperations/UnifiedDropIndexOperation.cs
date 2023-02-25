/* Copyright 2010-present MongoDB Inc.
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

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedDropIndexOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly string _indexName;
        private readonly IClientSessionHandle _session;

        public UnifiedDropIndexOperation(
            IClientSessionHandle session,
            IMongoCollection<BsonDocument> collection,
            string indexName)
        {
            _session = session;
            _collection = collection;
            _indexName = indexName;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    _collection.Indexes.DropOne(_indexName, cancellationToken);
                }
                else
                {
                    _collection.Indexes.DropOne(_session, _indexName, cancellationToken);
                }

                return OperationResult.Empty();
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
                if (_session == null)
                {
                    await _collection.Indexes.DropOneAsync(_indexName, cancellationToken);
                }
                else
                {
                    await _collection.Indexes.DropOneAsync(_session, _indexName, cancellationToken);
                }

                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedDropIndexOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedDropIndexOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedDropIndexOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];
            string indexName = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "name":
                        indexName = argument.Value.AsString;
                        break;
                    case "session":
                        var sessionId = argument.Value.AsString;
                        session = _entityMap.Sessions[sessionId];
                        break;
                    default:
                        throw new FormatException($"Invalid DropIndexOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedDropIndexOperation(session, collection, indexName);
        }
    }
}
