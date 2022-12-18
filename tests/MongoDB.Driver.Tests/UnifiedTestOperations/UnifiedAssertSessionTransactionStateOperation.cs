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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedAssertSessionTransactionStateOperation : IUnifiedSpecialTestOperation
    {
        private readonly CoreTransactionState _coreTransactionState;
        private readonly IClientSessionHandle _session;

        public UnifiedAssertSessionTransactionStateOperation(
            IClientSessionHandle session,
            CoreTransactionState coreTransactionState)
        {
            _session = session;
            _coreTransactionState = coreTransactionState;
        }

        public void Execute()
        {
            _session.WrappedCoreSession.CurrentTransaction.State.Should().Be(_coreTransactionState);
        }
    }

    public class UnifiedAssertSessionTransactionStateOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAssertSessionTransactionStateOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedAssertSessionTransactionStateOperation Build(BsonDocument arguments)
        {
            CoreTransactionState? coreTransactionState = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    case "state":
                        coreTransactionState = (CoreTransactionState)Enum.Parse(typeof(CoreTransactionState), argument.Value.AsString, ignoreCase: true);
                        break;
                    default:
                        throw new FormatException($"Invalid AssertSessionTransactionStateOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedAssertSessionTransactionStateOperation(session, coreTransactionState.Value);
        }
    }
}
