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

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedDropIndexesOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly IClientSessionHandle _session;
        private readonly DropIndexOptions _options;

        public UnifiedDropIndexesOperation(
            IClientSessionHandle session,
            IMongoCollection<BsonDocument> collection,
            DropIndexOptions options)
        {
            _session = session;
            _collection = collection;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    _collection.Indexes.DropAll(_options, cancellationToken);
                }
                else
                {
                    _collection.Indexes.DropAll(_session, _options, cancellationToken);
                }

                return OperationResult.Empty();
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
                if (_session == null)
                {
                    await _collection.Indexes.DropAllAsync(_options, cancellationToken);
                }
                else
                {
                    await _collection.Indexes.DropAllAsync(_session, _options, cancellationToken);
                }

                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedDropIndexesOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedDropIndexesOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedDropIndexesOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];
            IClientSessionHandle session = null;
            DropIndexOptions options = null;

            foreach (var argument in arguments ?? new BsonDocument())
            {
                switch (argument.Name)
                {
                    case "maxTimeMS":
                        options ??= new DropIndexOptions();
                        options.MaxTime = TimeSpan.FromMilliseconds(argument.Value.AsInt32);
                        break;
                    case "session":
                        var sessionId = argument.Value.AsString;
                        session = _entityMap.Sessions[sessionId];
                        break;
                    case "timeoutMS":
                        options ??= new DropIndexOptions();
                        options.Timeout = UnifiedEntityMap.ParseTimeout(argument.Value);
                        break;
                    default:
                        throw new FormatException($"Invalid DropIndexesOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedDropIndexesOperation(session, collection, options);
        }
    }
}
