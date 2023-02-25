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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedRecordTopologyDescriptionOperation : IUnifiedSpecialTestOperation
    {
        private readonly IMongoClient _client;
        private readonly UnifiedEntityMap _entityMap;
        private readonly string _id;

        public UnifiedRecordTopologyDescriptionOperation(IMongoClient client, UnifiedEntityMap entityMap, string id)
        {
            _client = Ensure.IsNotNull(client, nameof(client));
            _entityMap = Ensure.IsNotNull(entityMap, nameof(entityMap));
            _id = Ensure.IsNotNull(id, nameof(id));
        }

        public void Execute()
        {
            _entityMap.TopologyDescription.Add(_id, _client.Cluster.Description);
        }
    }

    public sealed class UnifiedRecordTopologyDescriptionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedRecordTopologyDescriptionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedRecordTopologyDescriptionOperation Build(BsonDocument arguments)
        {
            IMongoClient client = null;
            string id = null;
            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    switch (argument.Name)
                    {
                        case "client":
                            client = _entityMap.Clients[argument.Value.AsString];
                            break;
                        case "id":
                            id = argument.Value.AsString;
                            break;
                        default:
                            throw new FormatException($"Invalid {nameof(UnifiedRecordTopologyDescriptionOperation)} argument name: '{argument.Name}'.");
                    }
                }
            }

            return new UnifiedRecordTopologyDescriptionOperation(client, _entityMap, id);
        }
    }
}
