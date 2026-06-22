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
using MongoDB.Driver.Core.Configuration;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedAppendMetadataOperation : IUnifiedEntityTestOperation
    {
        private readonly MongoClient _client;
        private readonly LibraryInfo _libraryInfo;

        public UnifiedAppendMetadataOperation(MongoClient client, LibraryInfo libraryInfo)
        {
            _client = client;
            _libraryInfo = libraryInfo;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _client.AppendMetadata(_libraryInfo);
                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Execute(cancellationToken));
    }

    public class UnifiedAppendMetadataOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAppendMetadataOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedAppendMetadataOperation Build(string targetClientId, BsonDocument arguments)
        {
            var client = (MongoClient)_entityMap.Clients[targetClientId];

            string name = null;
            string version = null;
            string platform = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "driverInfoOptions":
                        foreach (var option in argument.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "name":
                                    name = option.Value.AsString;
                                    break;
                                case "version":
                                    version = option.Value.AsString;
                                    break;
                                case "platform":
                                    platform = option.Value.AsString;
                                    break;
                                default:
                                    throw new FormatException($"Invalid {nameof(UnifiedAppendMetadataOperation)} driverInfoOptions name: '{option.Name}'.");
                            }
                        }
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedAppendMetadataOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new(client, new LibraryInfo(name, version, platform));
        }
    }
}
