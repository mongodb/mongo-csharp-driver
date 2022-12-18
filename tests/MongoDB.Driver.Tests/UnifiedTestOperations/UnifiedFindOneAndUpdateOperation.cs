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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedFindOneAndUpdateOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly FindOneAndUpdateOptions<BsonDocument> _options;
        private readonly UpdateDefinition<BsonDocument> _update;
        private readonly IClientSessionHandle _session;

        public UnifiedFindOneAndUpdateOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            UpdateDefinition<BsonDocument> update,
            IClientSessionHandle session,
            FindOneAndUpdateOptions<BsonDocument> options)
        {
            _collection = collection;
            _filter = filter;
            _update = update;
            _session = session;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _session == null
                    ? _collection.FindOneAndUpdate(_filter, _update, _options, cancellationToken)
                    : _collection.FindOneAndUpdate(_session, _filter, _update, _options, cancellationToken);

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
                    ? await _collection.FindOneAndUpdateAsync(_filter, _update, _options, cancellationToken)
                    : await _collection.FindOneAndUpdateAsync(_session, _filter, _update, _options, cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedFindOneAndUpdateOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedFindOneAndUpdateOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedFindOneAndUpdateOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            FilterDefinition<BsonDocument> filter = null;
            FindOneAndUpdateOptions<BsonDocument> options = null;
            UpdateDefinition<BsonDocument> update = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "comment":
                        options ??= new FindOneAndUpdateOptions<BsonDocument>();
                        options.Comment = argument.Value;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        options ??= new FindOneAndUpdateOptions<BsonDocument>();
                        options.Hint = argument.Value;
                        break;
                    case "let":
                        options ??= new FindOneAndUpdateOptions<BsonDocument>();
                        options.Let = argument.Value.AsBsonDocument;
                        break;
                    case "returnDocument":
                        options ??= new FindOneAndUpdateOptions<BsonDocument>();
                        options.ReturnDocument = (ReturnDocument)Enum.Parse(typeof(ReturnDocument), argument.Value.AsString);
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    case "sort":
                        options ??= new FindOneAndUpdateOptions<BsonDocument>();
                        options.Sort = new BsonDocumentSortDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "update":
                        switch (argument.Value)
                        {
                            case BsonDocument:
                                update = argument.Value.AsBsonDocument;
                                break;
                            case BsonArray:
                                update = PipelineDefinition<BsonDocument, BsonDocument>.Create(argument.Value.AsBsonArray.Cast<BsonDocument>());
                                break;
                            default:
                                throw new FormatException($"Invalid FindOneAndUpdateOperation update argument: '{argument.Value}'.");
                        }
                        break;
                    default:
                        throw new FormatException($"Invalid FindOneAndUpdateOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedFindOneAndUpdateOperation(collection, filter, update, session, options);
        }
    }
}
