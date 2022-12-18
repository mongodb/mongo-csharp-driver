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
    public class UnifiedCreateIndexOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly CreateIndexModel<BsonDocument> _createIndexModel;
        private readonly IClientSessionHandle _session;

        public UnifiedCreateIndexOperation(
            IClientSessionHandle session,
            IMongoCollection<BsonDocument> collection,
            CreateIndexModel<BsonDocument> createIndexModel)
        {
            _session = session;
            _collection = collection;
            _createIndexModel = createIndexModel;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                string result;

                if (_session == null)
                {
                    result = _collection.Indexes.CreateOne(_createIndexModel, cancellationToken: cancellationToken);
                }
                else
                {
                    result = _collection.Indexes.CreateOne(_session, _createIndexModel, cancellationToken: cancellationToken);
                }

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
                string result;

                if (_session == null)
                {
                    result = await _collection.Indexes.CreateOneAsync(_createIndexModel, cancellationToken: cancellationToken);
                }
                else
                {
                    result = await _collection.Indexes.CreateOneAsync(_session, _createIndexModel, cancellationToken: cancellationToken);
                }

                return OperationResult.FromResult(BsonString.Create(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCreateIndexOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateIndexOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateIndexOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            BsonDocument keys = null;
            CreateIndexOptions options = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "keys":
                        keys = argument.Value.AsBsonDocument;
                        break;
                    case "name":
                        options = options ?? new CreateIndexOptions();
                        options.Name = argument.Value.AsString;
                        break;
                    case "session":
                        var sessionId = argument.Value.AsString;
                        session = _entityMap.Sessions[sessionId];
                        break;
                    default:
                        throw new FormatException($"Invalid CreateIndexOperation argument name: '{argument.Name}'.");
                }
            }

            var createIndexModel = new CreateIndexModel<BsonDocument>(keys, options);

            return new UnifiedCreateIndexOperation(session, collection, createIndexModel);
        }
    }
}
