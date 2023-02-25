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
    public class UnifiedWithTransactionOperation : IUnifiedOperationWithCreateAndRunOperationCallback
    {
        private readonly BsonArray _operations;
        private readonly TransactionOptions _options;
        private readonly IClientSessionHandle _session;

        public UnifiedWithTransactionOperation(
            IClientSessionHandle session,
            BsonArray operations,
            TransactionOptions options)
        {
            _session = session;
            _operations = operations;
            _options = options;
        }

        public void Execute(Action<BsonDocument, bool, CancellationToken> assertOperationCallback, CancellationToken cancellationToken)
        {
            _session.WithTransaction(
                callback: (session, token) =>
                {
                    foreach (var operationItem in _operations)
                    {
                        assertOperationCallback(operationItem.AsBsonDocument, false, token);
                    }

                    return (object)null;
                },
                transactionOptions: _options,
                cancellationToken: cancellationToken);
        }

        public async Task ExecuteAsync(Action<BsonDocument, bool, CancellationToken> assertOperationCallback, CancellationToken cancellationToken)
        {
            await _session.WithTransactionAsync(
                callbackAsync: (session, token) =>
                {
                    foreach (var operationItem in _operations)
                    {
                        assertOperationCallback(operationItem.AsBsonDocument, true, token);
                    }

                    return Task.FromResult<object>(null);
                },
                transactionOptions: _options,
                cancellationToken: cancellationToken);
        }
    }

    public class UnifiedWithTransactionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedWithTransactionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedWithTransactionOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var session = _entityMap.Sessions[targetSessionId];

            BsonArray operations = null;
            TransactionOptions options = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "callback":
                        operations = argument.Value.AsBsonArray;
                        break;
                    case "readConcern":
                        options = options ?? new TransactionOptions();
                        options = options.With(readConcern: ReadConcern.FromBsonDocument(argument.Value.AsBsonDocument));
                        break;
                    case "readPreference":
                        options = options ?? new TransactionOptions();
                        options = options.With(readPreference: ReadPreference.FromBsonDocument(argument.Value.AsBsonDocument));
                        break;
                    case "writeConcern":
                        options = options ?? new TransactionOptions();
                        options = options.With(writeConcern: WriteConcern.FromBsonDocument(argument.Value.AsBsonDocument));
                        break;
                    default:
                        throw new FormatException($"Invalid WithTransactionOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedWithTransactionOperation(session, operations, options);
        }
    }
}
