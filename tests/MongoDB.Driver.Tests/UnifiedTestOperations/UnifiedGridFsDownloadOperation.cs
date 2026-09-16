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
    public class UnifiedGridFsDownloadOperation : IUnifiedEntityTestOperation
    {
        private readonly GridFSBucket _bucket;
        private readonly BsonValue _id;

        public UnifiedGridFsDownloadOperation(
            GridFSBucket bucket,
            BsonValue id)
        {
            _bucket = bucket;
            _id = id;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _bucket.DownloadAsBytes(_id, cancellationToken: cancellationToken);

                return OperationResult.FromResult(BsonUtils.ToHexString(result));
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
                var result = await _bucket.DownloadAsBytesAsync(_id, cancellationToken: cancellationToken);

                return OperationResult.FromResult(BsonUtils.ToHexString(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedGridFsDownloadOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedGridFsDownloadOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedGridFsDownloadOperation Build(string targetBucketId, BsonDocument arguments)
        {
            var bucket = _entityMap.Buckets[targetBucketId];

            BsonValue id = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "id":
                        id = argument.Value;
                        break;
                    default:
                        throw new FormatException($"Invalid GridFsDownloadOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedGridFsDownloadOperation(
                bucket,
                id ?? throw new FormatException("GridFsDownloadOperation argument 'id' is required."));
        }
    }
}
