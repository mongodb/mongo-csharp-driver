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

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedFindOneAndDeleteOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly FindOneAndDeleteOptions<BsonDocument> _options;

        public UnifiedFindOneAndDeleteOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            FindOneAndDeleteOptions<BsonDocument> options)
        {
            _collection = collection;
            _filter = filter;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _collection.FindOneAndDelete(_filter, _options, cancellationToken);

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
                var result = await _collection.FindOneAndDeleteAsync(_filter, _options, cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedFindOneAndDeleteOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedFindOneAndDeleteOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedFindOneAndDeleteOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            FilterDefinition<BsonDocument> filter = null;
            FindOneAndDeleteOptions<BsonDocument> options = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "comment":
                        options ??= new FindOneAndDeleteOptions<BsonDocument>();
                        options.Comment = argument.Value;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        options ??= new FindOneAndDeleteOptions<BsonDocument>();
                        options.Hint = argument.Value;
                        break;
                    case "let":
                        options ??= new FindOneAndDeleteOptions<BsonDocument>();
                        options.Let = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid FindOneAndDeleteOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedFindOneAndDeleteOperation(collection, filter, options);
        }
    }
}
