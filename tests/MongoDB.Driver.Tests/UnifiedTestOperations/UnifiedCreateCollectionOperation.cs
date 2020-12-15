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
    public class UnifiedCreateCollectionOperation : IUnifiedEntityTestOperation
    {
        private readonly string _collectionName;
        private readonly IMongoDatabase _database;
        private readonly IClientSessionHandle _session;

        public UnifiedCreateCollectionOperation(
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
                    _database.CreateCollection(_collectionName, cancellationToken: cancellationToken);
                }
                else
                {
                    _database.CreateCollection(_session, _collectionName, cancellationToken: cancellationToken);
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
                    await _database.CreateCollectionAsync(_collectionName, cancellationToken: cancellationToken);
                }
                else
                {
                    await _database.CreateCollectionAsync(_session, _collectionName, cancellationToken: cancellationToken);
                }

                return OperationResult.FromResult(null);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCreateCollectionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateCollectionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateCollectionOperation Build(string targetDatabaseId, BsonDocument arguments)
        {
            var database = _entityMap.GetDatabase(targetDatabaseId);

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
                        var sessionId = argument.Value.AsString;
                        session = _entityMap.GetSession(sessionId);
                        break;
                    default:
                        throw new FormatException($"Invalid CreateCollectionOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedCreateCollectionOperation(session, database, collectionName);
        }
    }
}
