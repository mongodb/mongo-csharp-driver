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
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.GridFS;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedGridFsUploadOperation : IUnifiedEntityTestOperation
    {
        private readonly IGridFSBucket _bucket;
        private readonly string _filename;
        private readonly GridFSUploadOptions _options;
        private readonly byte[] _source;

        public UnifiedGridFsUploadOperation(
            IGridFSBucket bucket,
            string filename,
            byte[] source,
            GridFSUploadOptions options)
        {
            _bucket = bucket;
            _filename = filename;
            _source = source;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _bucket.UploadFromBytes(_filename, _source, _options, cancellationToken);

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
                var result = await _bucket.UploadFromBytesAsync(_filename, _source, _options, cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedGridFsUploadOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedGridFsUploadOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedGridFsUploadOperation Build(string targetBucketId, BsonDocument arguments)
        {
            var bucket = _entityMap.Buckets[targetBucketId];

            string filename = null;
            GridFSUploadOptions options = null;
            byte[] source = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "chunkSizeBytes":
                        options = options ?? new GridFSUploadOptions();
                        options.ChunkSizeBytes = argument.Value.AsInt32;
                        break;
                    case "filename":
                        filename = argument.Value.AsString;
                        break;
                    case "source":
                        var sourceDocument = argument.Value.AsBsonDocument;
                        JsonDrivenHelper.EnsureAllFieldsAreValid(sourceDocument, "$$hexBytes");
                        var sourceString = sourceDocument["$$hexBytes"].AsString;
                        if (sourceString.Length % 2 != 0)
                        {
                            throw new FormatException("$$hexBytes must have an even number of bytes.");
                        }
                        source = BsonUtils.ParseHexString(sourceString);
                        break;
                    default:
                        throw new FormatException($"Invalid GridFsUploadOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedGridFsUploadOperation(bucket, filename, source, options);
        }
    }
}
