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
        private readonly TransactionOptions _transactionOptions;

        public UnifiedStartTransactionOperation(IClientSessionHandle session, TransactionOptions transactionOptions)
        {
            _session = session;
            _transactionOptions = transactionOptions;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _session.StartTransaction(_transactionOptions);

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
            TransactionOptions options = null;

            if (arguments != null)
            {
                WriteConcern writeConcern = null;
                ReadConcern readConcern = null;
                ReadPreference readPreference = null;
                TimeSpan? maxCommitTime = null;

                foreach (var argument in arguments)
                {
                    switch (argument.Name)
                    {
                        case "maxCommitTimeMS":
                            maxCommitTime = TimeSpan.FromMilliseconds(argument.Value.AsInt32);
                            break;
                        case "readConcern":
                            readConcern = ReadConcern.FromBsonDocument(argument.Value.AsBsonDocument);
                            break;
                        case "readPreference":
                            readPreference = ReadPreference.FromBsonDocument(argument.Value.AsBsonDocument);
                            break;
                        case "writeConcern":
                            writeConcern = UnifiedEntityMap.ParseWriteConcern(argument.Value.AsBsonDocument);
                            break;
                        default:
                            throw new FormatException($"Invalid StartTransactionOperation argument name: '{argument.Name}'.");
                    }
                }

                options = new TransactionOptions(readConcern, readPreference, writeConcern, maxCommitTime);
            }

            return new UnifiedStartTransactionOperation(session, options);
        }
    }
}
