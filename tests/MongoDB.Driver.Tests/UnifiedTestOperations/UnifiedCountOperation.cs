﻿/* Copyright 2021-present MongoDB Inc.
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
#pragma warning disable CS0618 // Type or member is obsolete

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedCountOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly CountOptions _options;
        private readonly IClientSessionHandle _session;

        public UnifiedCountOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            CountOptions options,
            IClientSessionHandle session)
        {
            _collection = collection;
            _filter = filter;
            _options = options;
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _session == null
                    ? _collection.Count(_filter, _options, cancellationToken)
                    : _collection.Count(_session, _filter, _options, cancellationToken);

                return OperationResult.FromResult(result);
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
                var result = _session == null
                    ? await _collection.CountAsync(_filter, _options, cancellationToken)
                    : await _collection.CountAsync(_session, _filter, _options, cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCountOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCountOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCountOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            FilterDefinition<BsonDocument> filter = null;
            CountOptions options = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "comment":
                        options ??= new CountOptions();
                        options.Comment = argument.Value;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    default:
                        throw new FormatException($"Invalid CountOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedCountOperation(collection, filter, options, session);
        }
    }
}
