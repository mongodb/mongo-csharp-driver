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
    internal class UnifiedAbortTransactionOperation : IUnifiedEntityTestOperation
    {
        private readonly IClientSessionHandle _session;
        private readonly AbortTransactionOptions _options;

        public UnifiedAbortTransactionOperation(IClientSessionHandle session, AbortTransactionOptions options)
        {
            _session = session;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _session.AbortTransaction(_options, cancellationToken);
                return OperationResult.Empty();
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
                await _session.AbortTransactionAsync(_options, cancellationToken).ConfigureAwait(false);
                return OperationResult.Empty();
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }
    }

    internal class UnifiedAbortTransactionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAbortTransactionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedAbortTransactionOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var session = _entityMap.Sessions[targetSessionId];
            TimeSpan? timeout = null;

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    switch (argument.Name)
                    {
                        case "timeoutMS":
                            timeout = UnifiedEntityMap.ParseTimeout(argument.Value);
                            break;
                        default:
                            throw new FormatException($"Invalid AbortTransactionOperation argument name: '{argument.Name}'.");
                    }
                }
            }

            AbortTransactionOptions options = null;
            if (timeout.HasValue)
            {
                options = new AbortTransactionOptions(timeout);
            }

            return new UnifiedAbortTransactionOperation(session, options);
        }
    }
}
