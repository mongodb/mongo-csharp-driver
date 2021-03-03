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
    public class UnifiedUpdateManyOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly UpdateOptions _options;
        private readonly IClientSessionHandle _session;
        private readonly UpdateDefinition<BsonDocument> _update;

        public UnifiedUpdateManyOperation(
            IClientSessionHandle session,
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            UpdateDefinition<BsonDocument> update,
            UpdateOptions options)
        {
            _session = session;
            _collection = collection;
            _filter = filter;
            _update = update;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                UpdateResult result;

                if (_session == null)
                {
                    result = _collection.UpdateMany(_filter, _update, _options, cancellationToken);
                }
                else
                {
                    result = _collection.UpdateMany(_session, _filter, _update, _options, cancellationToken);
                }

                return OperationResult.FromResult(null);
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
                UpdateResult result;

                if (_session == null)
                {
                    result = await _collection.UpdateManyAsync(_filter, _update, _options, cancellationToken);
                }
                else
                {
                    result = await _collection.UpdateManyAsync(_session, _filter, _update, _options, cancellationToken);
                }

                return OperationResult.FromResult(null);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedUpdateManyOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedUpdateManyOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedUpdateManyOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            FilterDefinition<BsonDocument> filter = null;
            UpdateOptions options = null;
            IClientSessionHandle session = null;
            UpdateDefinition<BsonDocument> update = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "filter":
                        filter = argument.Value.AsBsonDocument;
                        break;
                    case "session":
                        session = _entityMap.GetSession(argument.Value.AsString);
                        break;
                    case "update":
                        update = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid UpdateManyOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedUpdateManyOperation(session, collection, filter, update, options);
        }
    }

    public class UnifiedUpdateManyOperationResultConverter
    {
        public OperationResult Convert(UpdateResult result)
        {
            throw new NotImplementedException("Specification requirements are not clear on result format.");
        }
    }
}
