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
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.TestHelpers;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    internal sealed class UnifiedTargetedFailPointOperation : IUnifiedSpecialTestOperation
    {
        private readonly UnifiedEntityMap _entityMap;
        private readonly bool _async;
        private readonly BsonDocument _failPointCommand;
        private readonly IClientSessionHandle _session;

        public UnifiedTargetedFailPointOperation(
            UnifiedEntityMap entityMap,
            IClientSessionHandle session,
            BsonDocument failPointCommand,
            bool async)
        {
            _async = async;
            _entityMap = entityMap;
            _session = session;
            _failPointCommand = failPointCommand;
        }

        public void Execute()
        {
            var pinnedServer = _session?.WrappedCoreSession?.CurrentTransaction?.PinnedServer.EndPoint;
            if (pinnedServer == null)
            {
                throw new InvalidOperationException("UnifiedTargetedFailPointOperation requires a pinned server.");
            }

            var client = DriverTestConfiguration.CreateMongoClient(useMultipleShardRouters: true);
            _entityMap.RegisterForDispose(client);

            var cluster = client.GetClusterInternal();
            var (server, roundTripTime) = cluster.SelectServer(OperationContext.NoTimeout, new EndPointServerSelector(pinnedServer));

            var session = NoCoreSession.NewHandle();

            var failPoint = FailPoint.Configure(server, roundTripTime, session, _failPointCommand, withAsync: _async);
            _entityMap.RegisterForDispose(failPoint);
        }
    }

    internal sealed class UnifiedTargetedFailPointOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedTargetedFailPointOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedTargetedFailPointOperation Build(BsonDocument arguments)
        {
            BsonDocument failPointCommand = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "failPoint":
                        failPointCommand = argument.Value.AsBsonDocument;
                        break;
                    case "session":
                        var sessionId = argument.Value.AsString;
                        session = _entityMap.Sessions[sessionId];
                        break;
                    default:
                        throw new FormatException($"Invalid TargetedFailPointOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedTargetedFailPointOperation(_entityMap, session, failPointCommand, _entityMap.Async);
        }
    }
}
