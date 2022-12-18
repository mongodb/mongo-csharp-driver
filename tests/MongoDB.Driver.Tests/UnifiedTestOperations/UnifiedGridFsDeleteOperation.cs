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
using MongoDB.Driver.GridFS;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedGridFsDeleteOperation : IUnifiedEntityTestOperation
    {
        private readonly IGridFSBucket _bucket;
        private readonly ObjectId _id;

        public UnifiedGridFsDeleteOperation(
            IGridFSBucket bucket,
            ObjectId id)
        {
            _bucket = bucket;
            _id = id;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _bucket.Delete(_id, cancellationToken);

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
                await _bucket.DeleteAsync(_id, cancellationToken);

                return OperationResult.FromResult(null);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedGridFsDeleteOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedGridFsDeleteOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedGridFsDeleteOperation Build(string targetBucketId, BsonDocument arguments)
        {
            var bucket = _entityMap.Buckets[targetBucketId];

            ObjectId? id = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "id":
                        id = argument.Value.AsObjectId;
                        break;
                    default:
                        throw new FormatException($"Invalid GridFsDeleteOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedGridFsDeleteOperation(bucket, id.Value);
        }
    }
}
