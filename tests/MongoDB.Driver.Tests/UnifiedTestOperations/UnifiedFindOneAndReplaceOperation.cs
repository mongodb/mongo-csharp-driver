/* Copyright 2021-present MongoDB Inc.
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
    public class UnifiedFindOneAndReplaceOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly FindOneAndReplaceOptions<BsonDocument> _options;
        private readonly BsonDocument _replacement;
        private readonly IClientSessionHandle _session;

        public UnifiedFindOneAndReplaceOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            BsonDocument replacement,
            FindOneAndReplaceOptions<BsonDocument> options,
            IClientSessionHandle session)
        {
            _collection = collection;
            _filter = filter;
            _options = options;
            _session = session;
            _replacement = replacement;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _session == null
                    ? _collection.FindOneAndReplace(_filter, _replacement, _options, cancellationToken)
                    : _collection.FindOneAndReplace(_session, _filter, _replacement, _options, cancellationToken);

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
                    ? await _collection.FindOneAndReplaceAsync(_filter, _replacement, _options, cancellationToken)
                    : await _collection.FindOneAndReplaceAsync(_session, _filter, _replacement, _options, cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedFindOneAndReplaceOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedFindOneAndReplaceOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedFindOneAndReplaceOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            FilterDefinition<BsonDocument> filter = null;
            FindOneAndReplaceOptions<BsonDocument> options = null;
            BsonDocument replacement = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "comment":
                        options ??= new FindOneAndReplaceOptions<BsonDocument>();
                        options.Comment = argument.Value;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        options ??= new FindOneAndReplaceOptions<BsonDocument>();
                        options.Hint = argument.Value;
                        break;
                    case "let":
                        options ??= new FindOneAndReplaceOptions<BsonDocument>();
                        options.Let = argument.Value.AsBsonDocument;
                        break;
                    case "replacement":
                        replacement = argument.Value.AsBsonDocument;
                        break;
                    case "returnDocument":
                        options ??= new FindOneAndReplaceOptions<BsonDocument>();
                        options.ReturnDocument = (ReturnDocument)Enum.Parse(typeof(ReturnDocument), argument.Value.AsString, true);
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    case "upsert":
                        options ??= new FindOneAndReplaceOptions<BsonDocument>();
                        options.IsUpsert = argument.Value.AsBoolean;
                        break;
                    default:
                        throw new FormatException($"Invalid FindOneAndReplaceOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedFindOneAndReplaceOperation(collection, filter, replacement, options, session);
        }
    }
}
