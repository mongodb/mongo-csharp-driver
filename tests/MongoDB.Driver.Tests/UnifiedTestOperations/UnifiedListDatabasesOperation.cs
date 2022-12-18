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
        private readonly ListDatabasesOptions _options = null;
        private readonly IClientSessionHandle _session = null;

        public UnifiedListDatabasesOperation(
            IMongoClient client,
            ListDatabasesOptions options,
            IClientSessionHandle session)
        {
            _client = client;
            _options = options;
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                using var cursor = _session == null
                    ? _client.ListDatabases(_options, cancellationToken)
                    : _client.ListDatabases(_session, _options, cancellationToken);

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
                    ? await _client.ListDatabasesAsync(_options, cancellationToken)
                    : await _client.ListDatabasesAsync(_session, _options, cancellationToken);

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
            var client = _entityMap.Clients[targetClientId];
            IClientSessionHandle session = null;
            ListDatabasesOptions options = null;

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    switch (argument.Name)
                    {
                        case "filter":
                            options ??= new ListDatabasesOptions();
                            options.Filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                            break;
                        case "session":
                            session = _entityMap.Sessions[argument.Value.AsString];
                            break;
                        default:
                            throw new FormatException($"Invalid ListDatabasesOperation argument name: '{argument.Name}'.");
                    }
                }
            }

            return new UnifiedListDatabasesOperation(client, options, session);
        }
    }
}
