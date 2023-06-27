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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedCreateSearchIndexOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly CreateSearchIndexModel _createSearchIndexModel;

        public UnifiedCreateSearchIndexOperation(
            IMongoCollection<BsonDocument> collection,
            CreateSearchIndexModel createSearchIndexModel)
        {
            _collection = collection;
            _createSearchIndexModel = createSearchIndexModel;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _collection.SearchIndexes.CreateOne(_createSearchIndexModel, cancellationToken: cancellationToken);

                return OperationResult.FromResult(BsonString.Create(result));
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
                var result = await _collection.SearchIndexes.CreateOneAsync(_createSearchIndexModel, cancellationToken: cancellationToken);

                return OperationResult.FromResult(BsonString.Create(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public sealed class UnifiedCreateSearchIndexOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateSearchIndexOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateSearchIndexOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            if (arguments.ElementCount != 1 || arguments.First().Name != "model")
            {
                throw new FormatException($"Expected single CreateSearchIndexOperation argument 'model'.");
            }

            var collection = _entityMap.Collections[targetCollectionId];
            var model = arguments["model"].AsBsonDocument;

            BsonDocument definition = null;
            string name = null;

            foreach (var argument in model)
            {
                switch (argument.Name)
                {
                    case "name":
                        name = argument.Value.AsString;
                        break;
                    case "definition":
                        definition = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid CreateSearchIndexOperation model argument name: '{argument.Name}'.");
                }
            }

            var createSearchIndexModel = new CreateSearchIndexModel(name, definition);

            return new(collection, createSearchIndexModel);
        }
    }
}
