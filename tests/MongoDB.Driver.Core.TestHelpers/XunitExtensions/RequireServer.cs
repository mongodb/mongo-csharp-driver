/* Copyright 2016 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.TestHelpers.XunitExtensions
{
    public class RequireServer
    {
        #region static
        public static RequireServer Check()
        {
            if (Environment.GetEnvironmentVariable("SKIPTESTSTHATREQUIRESERVER") != null)
            {
                throw new SkipTestException("Test skipped because it requires a server.");
            }
            return new RequireServer();
        }
        #endregion

        private SemanticVersion _serverVersion;

        public RequireServer()
        {
            _serverVersion = CoreTestConfiguration.ServerVersion;
        }

        public RequireServer Authentication(bool authentication)
        {
            var actualAuthentication = CoreTestConfiguration.ConnectionString.Username != null;
            if (actualAuthentication == authentication)
            {
                return this;
            }
            throw new SkipTestException($"Test skipped because authentication is {(actualAuthentication ? "on" : "off")}.");
        }

        public RequireServer ClusterType(ClusterType clusterType)
        {
            var actualClusterType = CoreTestConfiguration.Cluster.Description.Type;
            if (actualClusterType == clusterType)
            {
                return this;
            }
            throw new SkipTestException($"Test skipped because cluster type is {actualClusterType} and not {clusterType}.");
        }

        public RequireServer ClusterTypes(params ClusterType[] clusterTypes)
        {
            var actualClusterType = CoreTestConfiguration.Cluster.Description.Type;
            if (clusterTypes.Contains(actualClusterType))
            {
                return this;
            }
            var clusterTypesString = string.Join(", ", clusterTypes.Select(t => t.ToString()));
            throw new SkipTestException($"Test skipped because cluster type is {actualClusterType} and not one of ({clusterTypesString}).");
        }

        public RequireServer Supports(Feature feature)
        {
            if (feature.IsSupported(_serverVersion))
            {
                return this;
            }
            throw new SkipTestException($"Test skipped because server version {_serverVersion} does not support the {feature.Name} feature.");
        }

        public RequireServer Supports(params Feature[] features)
        {
            foreach (var feature in features)
            {
                Supports(feature);
            }
            return this;
        }

        public RequireServer DoesNotSupport(Feature feature)
        {
            if (!feature.IsSupported(_serverVersion))
            {
                return this;
            }
            throw new SkipTestException($"Test skipped because server version {_serverVersion} does support the {feature.Name} feature.");
        }

        public RequireServer DoesNotSupport(params Feature[] features)
        {
            foreach (var feature in features)
            {
                DoesNotSupport(feature);
            }
            return this;
        }

        public RequireServer StorageEngine(string storageEngine)
        {
            var actualStorageEngine = CoreTestConfiguration.GetStorageEngine();
            if (actualStorageEngine.Equals(storageEngine, StringComparison.OrdinalIgnoreCase))
            {
                return this;
            }
            throw new SkipTestException($"Test skipped because storage engine is \"{actualStorageEngine}\" and not \"{storageEngine}\".");
        }

        public RequireServer StorageEngines(params string[] storageEngines)
        {
            var actualStorageEngine = CoreTestConfiguration.GetStorageEngine();
            if (storageEngines.Contains(actualStorageEngine, StringComparer.OrdinalIgnoreCase))
            {
                return this;
            }
            var storageEnginesString = string.Join(", ", storageEngines.Select(e => "\"" + e + "\""));
            throw new SkipTestException($"Test skipped because storage engine is \"{actualStorageEngine}\" and not one of ({storageEnginesString}).");
        }

        public RequireServer VersionGreaterThanOrEqualTo(SemanticVersion version)
        {
            var actualVersion = CoreTestConfiguration.ServerVersion;
            if (actualVersion >= version)
            {
                return this;
            }
            throw new SkipTestException($"Test skipped because server version {actualVersion} is not greater than or equal to {version}.");
        }

        public RequireServer VersionGreaterThanOrEqualTo(string version)
        {
            return VersionGreaterThanOrEqualTo(SemanticVersion.Parse(version));
        }

        public RequireServer VersionLessThan(SemanticVersion version)
        {
            var actualVersion = CoreTestConfiguration.ServerVersion;
            if (actualVersion < version)
            {
                return this;
            }
            throw new SkipTestException($"Test skipped because server version {actualVersion} is not less than {version}.");
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
            throw new SkipTestException($"Test skipped because server version {actualVersion} is not less than or equal to {version}.");
        }

        public RequireServer VersionLessThanOrEqualTo(string version)
        {
            return VersionLessThanOrEqualTo(SemanticVersion.Parse(version));
        }
    }
}
