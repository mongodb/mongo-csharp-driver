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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedRewrapManyDataKeyOperation : IUnifiedEntityTestOperation
    {
        private readonly ClientEncryption _clientEncryption;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly RewrapManyDataKeyOptions _options;

        public UnifiedRewrapManyDataKeyOperation(ClientEncryption clientEncryption, FilterDefinition<BsonDocument> filter, RewrapManyDataKeyOptions options)
        {
            _clientEncryption = Ensure.IsNotNull(clientEncryption, nameof(clientEncryption));
            _filter = filter;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _clientEncryption.RewrapManyDataKey(_filter, _options, cancellationToken);

                return OperationResult.FromResult(CreateResult(result.BulkWriteResult));
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
                var result = await _clientEncryption.RewrapManyDataKeyAsync(_filter, _options, cancellationToken);

                return OperationResult.FromResult(CreateResult(result.BulkWriteResult));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        private BsonDocument CreateResult(BulkWriteResult bulkWriteResult)
            => bulkWriteResult != null
            ? new BsonDocument
            {
                {
                    "bulkWriteResult",
                    new BsonDocument
                    {
                        { "insertedCount", bulkWriteResult.InsertedCount },
                        { "matchedCount", bulkWriteResult.MatchedCount },
                        { "modifiedCount", bulkWriteResult.ModifiedCount },
                        { "deletedCount", bulkWriteResult.DeletedCount },
                        { "upsertedCount", bulkWriteResult.Upserts.Count },
                        { "upsertedIds", new BsonDocument(bulkWriteResult.Upserts.Select(i => new BsonElement(i.Index.ToString(), i.Id))) }
                    }
                }
            }
            : new BsonDocument();
    }

    public class UnifiedRewrapManyDataKeyOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedRewrapManyDataKeyOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedRewrapManyDataKeyOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var clientEncryption = _entityMap.ClientEncryptions[targetSessionId];

            FilterDefinition<BsonDocument> filter = null;
            string provider = null;
            BsonDocument masterKey = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "filter":
                        filter = argument.Value.AsBsonDocument;
                        break;
                    case "opts":
                        foreach (var option in argument.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "provider": provider = option.Value.AsString; break;
                                case "masterKey": masterKey = option.Value.AsBsonDocument; break;
                                default: throw new FormatException($"Invalid {nameof(RewrapManyDataKeyOptions)} option argument name: '{option.Name}'.");
                        };
                        }
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedRewrapManyDataKeyOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedRewrapManyDataKeyOperation(clientEncryption, filter, provider != null || masterKey != null ? new RewrapManyDataKeyOptions(provider, masterKey) : null);
        }
    }
}
