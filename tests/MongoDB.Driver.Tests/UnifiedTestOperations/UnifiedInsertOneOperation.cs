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
    public class UnifiedInsertOneOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly BsonDocument _document;
        private readonly InsertOneOptions _options;
        private readonly IClientSessionHandle _session;

        public UnifiedInsertOneOperation(
            IClientSessionHandle session,
            IMongoCollection<BsonDocument> collection,
            BsonDocument document,
            InsertOneOptions options)
        {
            _session = session;
            _collection = collection;
            _document = document;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    _collection.InsertOne(_document, _options, cancellationToken);
                }
                else
                {
                    _collection.InsertOne(_session, _document, _options, cancellationToken);
                }

                return OperationResult.FromResult(new BsonDocument("insertedId", _document["_id"]));
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
                    await _collection.InsertOneAsync(_document, _options, cancellationToken);
                }
                else
                {
                    await _collection.InsertOneAsync(_session, _document, _options, cancellationToken);
                }

                return OperationResult.FromResult(new BsonDocument("insertedId", _document["_id"]));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedInsertOneOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedInsertOneOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedInsertOneOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            BsonDocument document = null;
            InsertOneOptions options = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "bypassDocumentValidation":
                        options ??= new InsertOneOptions();
                        options.BypassDocumentValidation = argument.Value.AsBoolean;
                        break;
                    case "comment":
                        options ??= new InsertOneOptions();
                        options.Comment = argument.Value;
                        break;
                    case "document":
                        document = argument.Value.AsBsonDocument;
                        break;
                    case "session":
                        var sessionId = argument.Value.AsString;
                        session = _entityMap.Sessions[sessionId];
                        break;
                    default:
                        throw new FormatException($"Invalid InsertOneOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedInsertOneOperation(session, collection, document, options);
        }
    }
}
