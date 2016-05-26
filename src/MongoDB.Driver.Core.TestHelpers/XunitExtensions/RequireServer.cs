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
using System.Linq;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.TestHelpers.XunitExtensions
{
    public enum AuthenticationRequirement
    {
        Unspecified,
        On,
        Off
    }

    [Flags]
    public enum ClusterTypes
    {
        Standalone = 1,
        ReplicaSet = 2,
        Sharded = 4,
        StandaloneOrReplicaSet = Standalone | ReplicaSet,
        StandaloneOrSharded = Standalone | Sharded,
        ReplicaSetOrSharded = ReplicaSet | Sharded,
        Any = Standalone | ReplicaSet | Sharded
    }

    public static class RequireServer
    {
        public static void Any()
        {
            if (Environment.GetEnvironmentVariable("SKIPTESTSTHATREQUIRESERVER") != null)
            {
                throw new SkipTestException("Test skipped because it requires a server.");
            }
        }

        public static void Where(
            AuthenticationRequirement? authentication = null,
            ClusterTypes? clusterTypes = null,
            string minimumVersion = null,
            string modules = null,
            string storageEngines = null,
            string versionLessThan = null
            )
        {
            Any();
            if (minimumVersion != null)
            {
                CheckMinimumVersion(minimumVersion);
            }
            if (versionLessThan != null)
            {
                CheckVersionLessThan(versionLessThan);
            }
            if (clusterTypes != null)
            {
                CheckClusterTypes(clusterTypes.Value);
            }
            if (modules != null)
            {
                CheckModules(modules);
            }
            if (storageEngines != null)
            {
                CheckStorageEngines(storageEngines);
            }
            if (authentication != null)
            {
                CheckAuthentication(authentication.Value);
            }
        }

        private static void CheckAuthentication(AuthenticationRequirement authentication)
        {
            switch (authentication)
            {
                case AuthenticationRequirement.Off:
                    if (CoreTestConfiguration.ConnectionString.Username != null)
                    {
                        throw new SkipTestException("Test skipped because authentication is on.");
                    }
                    return;
                case AuthenticationRequirement.On:
                    if (CoreTestConfiguration.ConnectionString.Username == null)
                    {
                        throw new SkipTestException("Test skipped because authentication is off.");
                    }
                    return;
                case AuthenticationRequirement.Unspecified:
                    return;
                default:
                    throw new ArgumentException($"Invalid authentication requirement: {authentication}.", nameof(authentication));
            }
        }

        private static void CheckClusterTypes(ClusterTypes clusterTypes)
        {
            var clusterType = CoreTestConfiguration.Cluster.Description.Type;
            switch (clusterType)
            {
                case ClusterType.ReplicaSet:
                    if ((clusterTypes & ClusterTypes.ReplicaSet) != 0)
                    {
                        return;
                    }
                    break;
                case ClusterType.Sharded:
                    if ((clusterTypes & ClusterTypes.Sharded) != 0)
                    {
                        return;
                    }
                    break;
                case ClusterType.Standalone:
                    if ((clusterTypes & ClusterTypes.Standalone) != 0)
                    {
                        return;
                    }
                    break;
            }
            throw new SkipTestException($"Test skipped because cluster type is {clusterType} and not {clusterTypes}.");
        }

        private static void CheckMinimumVersion(string minimumVersion)
        {
            var minimumSemanticVersion = SemanticVersion.Parse(minimumVersion);
            var actualVersion = CoreTestConfiguration.ServerVersion;
            if (actualVersion < minimumSemanticVersion)
            {
                throw new SkipTestException($"Test skipped because server version {actualVersion} is less than {minimumVersion}.");
            }
        }

        private static void CheckModules(string modules)
        {
            var requiredModules = modules.Split(',');
            var actualModules = CoreTestConfiguration.GetModules().ToArray();

            if (requiredModules.Any(requiredModule => !actualModules.Contains(requiredModule)))
            {
                var requiredCsv = string.Join(", ", requiredModules);
                var actualCsv = string.Join(", ", actualModules);
                throw new SkipTestException($"Test skipped because one or more required modules is missing (required: [{requiredCsv}], actual: [{actualCsv}]).");
            }
        }

        private static void CheckStorageEngines(string storageEngines)
        {
            var requiredStorageEngines = storageEngines.Split(',');
            var actualStorageEngine = CoreTestConfiguration.GetStorageEngine();
            if (!requiredStorageEngines.Contains(actualStorageEngine))
            {
                var requiredCsv = string.Join(", ", requiredStorageEngines.Select(e => "\"" + e + "\"").ToArray());
                throw new SkipTestException($"Test skipped because \"{actualStorageEngine}\" is not one of the required storage engines: {requiredCsv}.");
            }
        }

        private static void CheckVersionLessThan(string versionLessThan)
        {
            var lessThanSemanticVersion = SemanticVersion.Parse(versionLessThan);
            var actualVersion = CoreTestConfiguration.ServerVersion;
            if (actualVersion >= lessThanSemanticVersion)
            {
                throw new SkipTestException($"Test skipped because server version {actualVersion} is not less than {versionLessThan}.");
            }
        }
    }
}
