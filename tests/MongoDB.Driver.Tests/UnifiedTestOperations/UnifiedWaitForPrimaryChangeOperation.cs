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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedWaitForPrimaryChangeOperation : IUnifiedSpecialTestOperation
    {
        private readonly IMongoClient _client;
        private readonly ClusterDescription _priorClusterDescription;
        private readonly TimeSpan _timeout;

        public UnifiedWaitForPrimaryChangeOperation(IMongoClient client, ClusterDescription priorClusterDescription, TimeSpan timeout)
        {
            _client = Ensure.IsNotNull(client, nameof(client));
            _priorClusterDescription = Ensure.IsNotNull(priorClusterDescription, nameof(priorClusterDescription));
            _timeout = timeout;
        }

        public void Execute()
        {
            SpinWait.SpinUntil(IsTopologyChanged, _timeout).Should().BeTrue($"because cluster topology should be changed during {_timeout}, but it wasn't");
        }

        private bool IsTopologyChanged()
        {
            var currentTopology = _client.Cluster.Description;
            return currentTopology.Servers.Any(s => s.Type == ServerType.ReplicaSetPrimary) && !currentTopology.Equals(_priorClusterDescription);
        }
    }

    public sealed class UnifiedWaitForPrimaryChangeOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedWaitForPrimaryChangeOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedWaitForPrimaryChangeOperation Build(BsonDocument arguments)
        {
            IMongoClient client = null;
            ClusterDescription priorClusterDescription = null;
            var timeout = TimeSpan.FromSeconds(10); // default
            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "client":
                        client = _entityMap.Clients[argument.Value.AsString];
                        break;
                    case "priorTopologyDescription":
                        priorClusterDescription = _entityMap.TopologyDescription[argument.Value.AsString];
                        break;
                    case "timeoutMS":
                        timeout = TimeSpan.FromSeconds(argument.Value.AsInt32);
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedWaitForPrimaryChangeOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedWaitForPrimaryChangeOperation(client, priorClusterDescription, timeout);
        }
    }
}
