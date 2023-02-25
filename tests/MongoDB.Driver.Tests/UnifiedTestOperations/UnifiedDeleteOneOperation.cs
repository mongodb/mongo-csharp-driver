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
    public class UnifiedDeleteOneOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly DeleteOptions _options;
        private readonly IClientSessionHandle _session = null;

        public UnifiedDeleteOneOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            DeleteOptions options,
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
                    ? _collection.DeleteOne(_filter, _options, cancellationToken)
                    : _collection.DeleteOne(_session, _filter, _options, cancellationToken);

                return new UnifiedDeleteOneOperationResultConverter().Convert(result);
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
                    ? await _collection.DeleteOneAsync(_filter, _options, cancellationToken)
                    : await _collection.DeleteOneAsync(_session, _filter, _options, cancellationToken);

                return new UnifiedDeleteOneOperationResultConverter().Convert(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedDeleteOneOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedDeleteOneOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedDeleteOneOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            FilterDefinition<BsonDocument> filter = null;
            DeleteOptions options = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "comment":
                        options ??= new DeleteOptions();
                        options.Comment = argument.Value;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        options ??= new DeleteOptions();
                        options.Hint = argument.Value;
                        break;
                    case "let":
                        options ??= new DeleteOptions();
                        options.Let = argument.Value.AsBsonDocument;
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    default:
                        throw new FormatException($"Invalid DeleteOneOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedDeleteOneOperation(collection, filter, options, session);
        }
    }

    public class UnifiedDeleteOneOperationResultConverter
    {
        public OperationResult Convert(DeleteResult result)
        {
            var document = new BsonDocument
            {
                { "deletedCount", result.DeletedCount }
            };

            return OperationResult.FromResult(document);
        }
    }
}
