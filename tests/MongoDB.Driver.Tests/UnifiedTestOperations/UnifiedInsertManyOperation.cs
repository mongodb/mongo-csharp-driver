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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedInsertManyOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly List<BsonDocument> _documents;
        private readonly InsertManyOptions _options;
        private readonly IClientSessionHandle _session;

        public UnifiedInsertManyOperation(
            IClientSessionHandle session,
            IMongoCollection<BsonDocument> collection,
            List<BsonDocument> documents,
            InsertManyOptions options)
        {
            _session = session;
            _collection = collection;
            _documents = documents;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    _collection.InsertMany(_documents, _options, cancellationToken);
                }
                else
                {
                    _collection.InsertMany(_session, _documents, _options, cancellationToken);
                }

                return OperationResult.FromResult(null); // In .NET InsertMany returns no result
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
                    await _collection.InsertManyAsync(_documents, _options, cancellationToken);
                }
                else
                {
                    await _collection.InsertManyAsync(_session, _documents, _options, cancellationToken);
                }

                return OperationResult.FromResult(null);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedInsertManyOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedInsertManyOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedInsertManyOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            List<BsonDocument> documents = null;
            InsertManyOptions options = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "comment":
                        options ??= new InsertManyOptions();
                        options.Comment = argument.Value;
                        break;
                    case "documents":
                        documents = argument.Value.AsBsonArray.Cast<BsonDocument>().ToList();
                        break;
                    case "ordered":
                        options = options ?? new InsertManyOptions();
                        options.IsOrdered = argument.Value.AsBoolean;
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    default:
                        throw new FormatException($"Invalid InsertManyOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedInsertManyOperation(session, collection, documents, options);
        }
    }
}
