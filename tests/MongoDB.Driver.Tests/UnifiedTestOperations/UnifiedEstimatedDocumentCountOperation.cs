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
    public class UnifiedEstimatedDocumentCountOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly EstimatedDocumentCountOptions _options;

        public UnifiedEstimatedDocumentCountOperation(
            IMongoCollection<BsonDocument> collection,
            EstimatedDocumentCountOptions options)
        {
            _collection = collection;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _collection.EstimatedDocumentCount(_options, cancellationToken: cancellationToken);

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
                var result = await _collection.EstimatedDocumentCountAsync(_options, cancellationToken: cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedEstimatedDocumentCountOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedEstimatedDocumentCountOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedEstimatedDocumentCountOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            EstimatedDocumentCountOptions options = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    default:
                        throw new FormatException($"Invalid EstimatedDocumentCountOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedEstimatedDocumentCountOperation(collection, options);
        }
    }
}
