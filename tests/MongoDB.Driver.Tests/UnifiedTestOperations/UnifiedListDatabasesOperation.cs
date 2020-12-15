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

        public UnifiedListDatabasesOperation(IMongoClient client)
        {
            _client = client;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _client.ListDatabases(cancellationToken);
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
                var cursor = await _client.ListDatabasesAsync(cancellationToken);
                var result = await cursor.ToListAsync();

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

            if (arguments != null)
            {
                throw new FormatException("ListDatabasesOperation is not expected to contain arguments.");
            }

            return new UnifiedListDatabasesOperation(client);
        }
    }
}
