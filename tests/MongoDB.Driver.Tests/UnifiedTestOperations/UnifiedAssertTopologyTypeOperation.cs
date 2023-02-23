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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedAssertTopologyTypeOperation : IUnifiedSpecialTestOperation
    {
        private readonly string _expectedTopologyType;
        private readonly ClusterDescription _topologyDescription;

        public UnifiedAssertTopologyTypeOperation(ClusterDescription topologyDescription, string expectedTypologyType)
        {
            _expectedTopologyType = Ensure.IsNotNull(expectedTypologyType, nameof(expectedTypologyType));
            _topologyDescription = Ensure.IsNotNull(topologyDescription, nameof(topologyDescription));
        }

        public void Execute()
        {
            IsExpectedTopology(_expectedTopologyType, _topologyDescription).Should().BeTrue();
        }

        private bool IsExpectedTopology(string topologyType, ClusterDescription clusterDescription) =>
            topologyType switch
            {
                "Single" => clusterDescription.Type == ClusterType.Standalone,
                "ReplicaSetNoPrimary" => clusterDescription.Type == ClusterType.ReplicaSet && clusterDescription.Servers.All(s => s.Type != ServerType.ReplicaSetPrimary),
                "ReplicaSetWithPrimary" => clusterDescription.Type == ClusterType.ReplicaSet && clusterDescription.Servers.Any(s => s.Type == ServerType.ReplicaSetPrimary),
                "Sharded" => clusterDescription.Type == ClusterType.Sharded,
                "LoadBalanced" => clusterDescription.Type == ClusterType.LoadBalanced,
                "Unknown" => clusterDescription.Type == ClusterType.Unknown,
                _ => throw new Exception($"Unexpected topology type: {topologyType}."),
            };
    }

    public sealed class UnifiedAssertTopologyTypeOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAssertTopologyTypeOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedAssertTopologyTypeOperation Build(BsonDocument arguments)
        {
            ClusterDescription topologyDescription = null;
            string expectedTopologyType = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "topologyDescription":
                        topologyDescription = _entityMap.TopologyDescription[argument.Value.AsString];
                        break;
                    case "topologyType":
                        expectedTopologyType = argument.Value.AsString;
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedAssertTopologyTypeOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedAssertTopologyTypeOperation(topologyDescription, expectedTopologyType);
        }
    }
}
