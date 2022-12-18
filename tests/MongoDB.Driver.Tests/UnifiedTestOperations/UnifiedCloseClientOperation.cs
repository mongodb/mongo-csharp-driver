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
using MongoDB.Driver.TestHelpers;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedCloseClientOperation : IUnifiedEntityTestOperation
    {
        private readonly DisposableMongoClient _client;

        public UnifiedCloseClientOperation(DisposableMongoClient client)
        {
            _client = client;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _client.Dispose();
                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Execute(cancellationToken));
    }

    public class UnifiedCloseClientOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCloseClientOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCloseClientOperation Build(string targetClientId, BsonDocument arguments)
        {
            if (arguments?.ElementCount > 0)
            {
                throw new FormatException($"{nameof(UnifiedCloseClientOperationBuilder)} does not expected any arguments.");
            }

            var client = _entityMap.Clients[targetClientId];

            return new(client);
        }
    }
}
