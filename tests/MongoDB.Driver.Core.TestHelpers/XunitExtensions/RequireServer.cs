/* Copyright 2016-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using Xunit;
using Xunit.Sdk;

namespace MongoDB.Driver.Core.TestHelpers.XunitExtensions
{
    public class RequireServer
    {
        #region static
        public static RequireServer Check()
        {
            if (Environment.GetEnvironmentVariable("SKIPTESTSTHATREQUIRESERVER") != null)
            {
                throw new SkipException("Test skipped because it requires a server.");
            }
            return new RequireServer();
        }
        #endregion

        private readonly int _maxWireVersion;
        private readonly SemanticVersion _serverVersion;

        public RequireServer()
        {
            _maxWireVersion = CoreTestConfiguration.MaxWireVersion;
            _serverVersion = CoreTestConfiguration.ServerVersion;
        }

        public RequireServer Authentication(bool authentication)
        {
            var actualAuthentication = IsAuthenticated();
            if (actualAuthentication == authentication)
            {
                return this;
            }
            throw new SkipException($"Test skipped because authentication is {(actualAuthentication ? "on" : "off")}.");
        }

        public RequireServer ClusterType(ClusterType clusterType)
        {
            var actualClusterType = CoreTestConfiguration.Cluster.Description.Type;
            if (actualClusterType == clusterType)
            {
                return this;
            }
            throw new SkipException($"Test skipped because cluster type is {actualClusterType} and not {clusterType}.");
        }

        public RequireServer ClusterTypes(params ClusterType[] clusterTypes)
        {
            var actualClusterType = CoreTestConfiguration.Cluster.Description.Type;
            if (clusterTypes.Contains(actualClusterType))
            {
                return this;
            }
            var clusterTypesString = string.Join(", ", clusterTypes.Select(t => t.ToString()));
            throw new SkipException($"Test skipped because cluster type is {actualClusterType} and not one of ({clusterTypesString}).");
        }

        public RequireServer DoesNotSupport(Feature feature)
        {
            if (!feature.IsSupported(_maxWireVersion))
            {
                return this;
            }
            throw new SkipException($"Test skipped because server version {_serverVersion} does support the {feature.Name} feature.");
        }

        public RequireServer DoesNotSupport(params Feature[] features)
        {
            foreach (var feature in features)
            {
                DoesNotSupport(feature);
            }
            return this;
        }

        /// <summary>
        /// Determine whether a load balancing mode is configured.
        /// </summary>
        /// <param name="enabled">The expected value.</param>
        /// <param name="ignorePreviousSetup">Force creating a new connection string and ignore the value assigned by scripts. Use this argument only for local development.</param>
        public RequireServer LoadBalancing(bool enabled, bool ignorePreviousSetup = false)
        {
            var isLoadBalancing = (ignorePreviousSetup ? CoreTestConfiguration.CreateConnectionString() : CoreTestConfiguration.ConnectionString).LoadBalanced;
            if (isLoadBalancing == enabled)
            {
                return this;
            }
            throw new SkipException($"Test skipped because load balancing mode is {(isLoadBalancing ? "on" : "off")}.");
        }

        public RequireServer RequireApiVersion(bool require = true)
        {
            if (CoreTestConfiguration.RequireApiVersion == require)
            {
                return this;
            }

            throw new SkipException("Test skipped because API version is " + (require ? "required" : "not required") + ".");
        }

        public RequireServer RunOn(BsonArray requirements)
        {
            if (requirements.Any(requirement => CanRunOn(requirement.AsBsonDocument)))
            {
                return this;
            }

            throw new SkipException($"Test skipped because cluster does not meet runOn requirements: {requirements}.");
        }

        public RequireServer Serverless(bool require = true)
        {
            var isServerless = CoreTestConfiguration.Serverless;

            if (isServerless == require)
            {
                return this;
            }

            throw new SkipException("Test skipped because serverless is " + (require ? "required" : "not required") + ".");
        }

        public RequireServer StableServer(bool stable = true)
        {
            var serverVersion = CoreTestConfiguration.ServerVersion;
            var isStableServer = serverVersion.PreRelease == null;

            if (isStableServer == stable)
            {
                return this;
            }

            throw new SkipException($"Test skipped because {(stable ? "GA" : "prerelease")} server version is expected, but found {serverVersion}.");
        }

        public RequireServer Supports(Feature feature)
        {
            if (feature.IsSupported(_maxWireVersion))
            {
                return this;
            }
            throw new SkipException($"Test skipped because server version {_serverVersion} does not support the {feature.Name} feature.");
        }

        public RequireServer Supports(params Feature[] features)
        {
            foreach (var feature in features)
            {
                Supports(feature);
            }
            return this;
        }

        public RequireServer SupportsCausalConsistency()
        {
            return ClusterTypes(Clusters.ClusterType.Sharded, Clusters.ClusterType.ReplicaSet).SupportsSessions();
        }

        public RequireServer SupportsSessions()
        {
            var clusterDescription = CoreTestConfiguration.Cluster.Description;
            if (clusterDescription.LogicalSessionTimeout != null || clusterDescription.Type == Clusters.ClusterType.LoadBalanced)
            {
                return this;
            }
            throw new SkipException($"Test skipped because the cluster does not support sessions.");
        }

        public RequireServer StorageEngine(string storageEngine)
        {
            var actualStorageEngine = CoreTestConfiguration.StorageEngine;
            if (actualStorageEngine.Equals(storageEngine, StringComparison.OrdinalIgnoreCase))
            {
                return this;
            }
            throw new SkipException($"Test skipped because storage engine is \"{actualStorageEngine}\" and not \"{storageEngine}\".");
        }

        public RequireServer StorageEngines(params string[] storageEngines)
        {
            var actualStorageEngine = CoreTestConfiguration.StorageEngine;
            if (storageEngines.Contains(actualStorageEngine, StringComparer.OrdinalIgnoreCase))
            {
                return this;
            }
            var storageEnginesString = string.Join(", ", storageEngines.Select(e => "\"" + e + "\""));
            throw new SkipException($"Test skipped because storage engine is \"{actualStorageEngine}\" and not one of ({storageEnginesString}).");
        }

        public RequireServer Tls(bool required = true)
        {
            var usingTls = CoreTestConfiguration.ConnectionString.Tls;
            if (usingTls == required)
            {
                return this;
            }
            throw new SkipException(
                $"Test skipped because the connection string specifies TLS={usingTls} " +
                $"and this test requires TLS={required}.");
        }

        public RequireServer MultipleMongosesIfSharded(bool required)
        {
            var clusterType = CoreTestConfiguration.Cluster.Description.Type;
            if (clusterType == Clusters.ClusterType.Sharded || clusterType == Clusters.ClusterType.LoadBalanced)
            {
                MultipleMongoses(required);
            }

            return this;
        }

        public RequireServer MultipleMongoses(bool required)
        {
            if (!required || CoreTestConfiguration.NumberOfMongoses >= 2)
            {
                return this;
            }

            throw new SkipException($"Test skipped because the cluster does not have multiple mongoses.");
        }

        public RequireServer VersionGreaterThanOrEqualTo(SemanticVersion version)
        {
            var actualVersion = CoreTestConfiguration.ServerVersion;
            if (actualVersion >= version)
            {
                return this;
            }
            throw new SkipException($"Test skipped because server version {actualVersion} is not greater than or equal to {version}.");
        }

        public RequireServer VersionGreaterThanOrEqualTo(string version)
        {
            return VersionGreaterThanOrEqualTo(SemanticVersion.Parse(version));
        }

        public RequireServer VersionLessThan(SemanticVersion version)
        {
            var actualVersion = _serverVersion;
            if (actualVersion < version)
            {
                return this;
            }
            throw new SkipException($"Test skipped because server version {actualVersion} is not less than {version}.");
        }

        public RequireServer VersionLessThan(string version)
        {
            return VersionLessThan(SemanticVersion.Parse(version));
        }

        public RequireServer VersionLessThanOrEqualTo(SemanticVersion version)
        {
            var actualVersion = CoreTestConfiguration.ServerVersion;
            if (actualVersion <= version)
            {
                return this;
            }
            throw new SkipException($"Test skipped because server version {actualVersion} is not less than or equal to {version}.");
        }

        public RequireServer VersionLessThanOrEqualTo(string version)
        {
            return VersionLessThanOrEqualTo(SemanticVersion.Parse(version));
        }

        // private methods
        private bool CanRunOn(BsonDocument requirements)
        {
            return requirements.All(IsRequirementSatisfied);
        }

        private bool IsAuthenticated() => CoreTestConfiguration.ConnectionString.Username != null;

        private bool IsRequirementSatisfied(BsonElement requirement)
        {
            switch (requirement.Name)
            {
                case "authEnabled":
                case "auth":
                    return IsAuthenticated() == requirement.Value.ToBoolean();
                case "minServerVersion":
                    {
                        var actualVersion = CoreTestConfiguration.ServerVersion;
                        var minServerVersion = SemanticVersion.Parse(requirement.Value.AsString);
                        return SemanticVersionCompareToAsReleased(actualVersion, minServerVersion) >= 0;
                    }
                case "maxServerVersion":
                    {
                        var actualVersion = CoreTestConfiguration.ServerVersion;
                        var maxServerVersion = SemanticVersion.Parse(requirement.Value.AsString);
                        return SemanticVersionCompareToAsReleased(actualVersion, maxServerVersion) <= 0;
                    }
                case "serverless":
                    var serverlessValue = requirement.Value.AsString;
                    switch (serverlessValue)
                    {
                        case "allow":
                            return true;
                        case "forbid":
                            return CoreTestConfiguration.Serverless == false;
                        case "require":
                            return CoreTestConfiguration.Serverless == true;
                        default:
                            throw new FormatException($"Invalid runOn requirement serverless field value: '{requirement.Value}'.");
                    }
                case "serverParameters":
                    var serverParameters = CoreTestConfiguration.GetServerParameters();
                    foreach (var parameter in requirement.Value.AsBsonDocument)
                    {
                        if (serverParameters[parameter.Name] != parameter.Value)
                        {
                            return false;
                        }
                    }
                    return true;
                case "topologies":
                case "topology":
                    var actualClusterType = CoreTestConfiguration.Cluster.Description.Type;
                    var runOnClusterTypes = requirement.Value.AsBsonArray.Select(topology => MapTopologyToClusterType(topology.AsString)).ToList();
                    return runOnClusterTypes.Contains(actualClusterType);
                case "csfle": return Feature.ClientSideEncryption.IsSupported(CoreTestConfiguration.MaxWireVersion);
                default:
                    throw new FormatException($"Unrecognized requirement field: '{requirement.Name}'.");
            }
        }

        private ClusterType MapTopologyToClusterType(string topology)
        {
            switch (topology)
            {
                case "single": return Clusters.ClusterType.Standalone;
                case "replicaset": return Clusters.ClusterType.ReplicaSet;
                case "sharded-replicaset":
                case "sharded": return Clusters.ClusterType.Sharded;
                case "load-balanced": return Clusters.ClusterType.LoadBalanced;
                default: throw new ArgumentException($"Invalid topology: \"{topology}\".", nameof(topology));
            }
        }

        private int SemanticVersionCompareToAsReleased(SemanticVersion a, SemanticVersion b)
        {
            var aReleased = new SemanticVersion(a.Major, a.Minor, a.Patch);
            var bReleased = new SemanticVersion(b.Major, b.Minor, b.Patch);
            return aReleased.CompareTo(bReleased);
        }
    }
}
