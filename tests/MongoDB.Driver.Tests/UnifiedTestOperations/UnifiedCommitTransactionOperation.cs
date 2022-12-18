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
    public class UnifiedCommitTransactionOperation : IUnifiedEntityTestOperation
    {
        private readonly IClientSessionHandle _session;

        public UnifiedCommitTransactionOperation(IClientSessionHandle session)
        {
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _session.CommitTransaction(cancellationToken);
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
                await _session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                return OperationResult.Empty();
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }
    }

    public class UnifiedCommitTransactionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCommitTransactionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCommitTransactionOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var session = _entityMap.Sessions[targetSessionId];

            if (arguments != null)
            {
                throw new FormatException("CommitTransactionOperation is not expected to contain arguments.");
            }

            return new UnifiedCommitTransactionOperation(session);
        }
    }
}
