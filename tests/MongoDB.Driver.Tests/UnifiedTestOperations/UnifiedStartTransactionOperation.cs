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
    public class UnifiedStartTransactionOperation : IUnifiedEntityTestOperation
    {
        private readonly IClientSessionHandle _session;

        public UnifiedStartTransactionOperation(IClientSessionHandle session)
        {
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _session.StartTransaction();

                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(cancellationToken));
        }
    }

    public class UnifiedStartTransactionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedStartTransactionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedStartTransactionOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var session = _entityMap.Sessions[targetSessionId];

            if (arguments != null)
            {
                throw new FormatException("StartTransactionOperation is not expected to contain arguments.");
            }

            return new UnifiedStartTransactionOperation(session);
        }
    }
}
