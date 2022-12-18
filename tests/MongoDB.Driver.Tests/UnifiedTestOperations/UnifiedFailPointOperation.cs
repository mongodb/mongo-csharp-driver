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
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedFailPointOperation : IUnifiedFailPointOperation
    {
        private readonly bool _async;
        private readonly IMongoClient _client;
        private readonly BsonDocument _failPointCommand;

        public UnifiedFailPointOperation(
            IMongoClient client,
            BsonDocument failPointCommand,
            bool async)
        {
            _async = async;
            _client = Ensure.IsNotNull(client, nameof(client));
            _failPointCommand = Ensure.IsNotNull(failPointCommand, nameof(failPointCommand));
        }

        public void Execute(out FailPoint failPoint)
        {
            var cluster = _client.Cluster;
            var session = NoCoreSession.NewHandle();

            failPoint = FailPoint.Configure(cluster, session, _failPointCommand, _async);
        }
    }

    public class UnifiedFailPointOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedFailPointOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedFailPointOperation Build(BsonDocument arguments)
        {
            IMongoClient client = null;
            BsonDocument failPointCommand = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "client":
                        client = _entityMap.Clients[argument.Value.AsString];
                        break;
                    case "failPoint":
                        failPointCommand = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid FailPointOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedFailPointOperation(client, failPointCommand, _entityMap.Async);
        }
    }
}
