/* Copyright 2010-present MongoDB Inc.
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
    public class UnifiedGridFsDownloadByNameOperation : IUnifiedEntityTestOperation
    {
        private readonly IGridFSBucket _bucket;
        private readonly string _fileName;
        private readonly GridFSDownloadByNameOptions _options;

        public UnifiedGridFsDownloadByNameOperation(
            IGridFSBucket bucket,
            string fileName,
            GridFSDownloadByNameOptions options)
        {
            _bucket = bucket;
            _fileName = fileName;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _bucket.DownloadAsBytesByName(_fileName, _options, cancellationToken: cancellationToken);

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
                var result = await _bucket.DownloadAsBytesByNameAsync(_fileName, _options, cancellationToken: cancellationToken);

                return OperationResult.FromResult(BsonUtils.ToHexString(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedGridFsDownloadByNameOperationBuilder(UnifiedEntityMap entityMap)
    {
        public UnifiedGridFsDownloadByNameOperation Build(string targetBucketId, BsonDocument arguments)
        {
            var bucket = entityMap.Buckets[targetBucketId];

            string filename = null;
            GridFSDownloadByNameOptions options = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "filename":
                        filename = argument.Value.AsString;
                        break;
                    case "revision":
                        options = options ?? new GridFSDownloadByNameOptions();
                        options.Revision = argument.Value.AsInt32;
                        break;
                    default:
                        throw new FormatException($"Invalid UnifiedGridFsDownloadByNameOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedGridFsDownloadByNameOperation(bucket, filename, options);
        }
    }
}
