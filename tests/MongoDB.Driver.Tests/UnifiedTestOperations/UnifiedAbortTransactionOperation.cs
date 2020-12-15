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
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedAbortTransactionOperation : IUnifiedSpecialTestOperation
    {
        private readonly IClientSessionHandle _session;

        public UnifiedAbortTransactionOperation(IClientSessionHandle session)
        {
            _session = session;
        }

        public void Execute()
        {
            _session.AbortTransaction();
        }
    }

    public class UnifiedAbortTransactionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAbortTransactionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedAbortTransactionOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var session = _entityMap.GetSession(targetSessionId);

            if (arguments != null)
            {
                throw new FormatException("AbortTransactionOperation is not expected to contain arguments.");
            }

            return new UnifiedAbortTransactionOperation(session);
        }
    }
}
