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
    public class UnifiedListDatabasesOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoClient _client;
        private readonly IClientSessionHandle _session = null;

        public UnifiedListDatabasesOperation(
            IMongoClient client,
            IClientSessionHandle session)
        {
            _client = client;
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                using var cursor = _session == null
                    ? _client.ListDatabases(cancellationToken)
                    : _client.ListDatabases(_session, cancellationToken);

                var result = cursor.ToList(cancellationToken);

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
                using var cursor = _session == null
                    ? await _client.ListDatabasesAsync(cancellationToken)
                    : await _client.ListDatabasesAsync(_session, cancellationToken);

                var result = await cursor.ToListAsync(cancellationToken);

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedListDatabasesOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedListDatabasesOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedListDatabasesOperation Build(string targetClientId, BsonDocument arguments)
        {
            var client = _entityMap.GetClient(targetClientId);
            IClientSessionHandle session = null;

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    switch (argument.Name)
                    {
                        case "session":
                            session = _entityMap.GetSession(argument.Value.AsString);
                            break;
                        default:
                            throw new FormatException($"Invalid ListDatabasesOperation argument name: '{argument.Name}'.");
                    }
                }
            }

            return new UnifiedListDatabasesOperation(client, session);
        }
    }
}
