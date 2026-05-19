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

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedDropCollectionOperation : IUnifiedEntityTestOperation
    {
        private readonly string _collectionName;
        private readonly IMongoDatabase _database;
        private readonly IClientSessionHandle _session;

        public UnifiedDropCollectionOperation(
            IClientSessionHandle session,
            IMongoDatabase database,
            string collectionName)
        {
            _session = session;
            _database = database;
            _collectionName = collectionName;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    _database.DropCollection(_collectionName);
                }
                else
                {
                    _database.DropCollection(_session, _collectionName);
                }

                return OperationResult.FromResult(null);
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
                    await _database.DropCollectionAsync(_collectionName, cancellationToken);
                }
                else
                {
                    await _database.DropCollectionAsync(_session, _collectionName, cancellationToken);
                }

                return OperationResult.FromResult(null);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedDropCollectionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedDropCollectionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedDropCollectionOperation Build(string targetDatabaseId, BsonDocument arguments)
        {
            var database = _entityMap.Databases[targetDatabaseId];

            string collectionName = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "collection":
                        collectionName = argument.Value.AsString;
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    default:
                        throw new FormatException($"Invalid DropCollectionOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedDropCollectionOperation(session, database, collectionName);
        }
    }
}
