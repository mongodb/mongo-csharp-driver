/* Copyright 2013-2015 MongoDB Inc.
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
using System.Reflection;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Core
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

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequiresServerAttribute : CategoryAttribute, ITestAction
    {
        // fields
        private IList<string> _modules;
        private IList<string> _storageEngines;

        // constructors
        public RequiresServerAttribute()
            : base("RequiresServer")
        {
            ClusterTypes = ClusterTypes.Any;
        }

        public RequiresServerAttribute(string beforeTestMethodName)
            : this()
        {
            BeforeTestMethodName = beforeTestMethodName;
        }

        public RequiresServerAttribute(string beforeTestMethodName, string afterTestMethodName)
            : this(beforeTestMethodName)
        {
            AfterTestMethodName = afterTestMethodName;
        }

        // properties

        public AuthenticationRequirement Authentication { get; set; }

        public string AfterTestMethodName { get; set; }

        public string BeforeTestMethodName { get; set; }

        public ClusterTypes ClusterTypes { get; set; }

        public string MinimumVersion { get; set; }

        public string Modules
        {
            get
            {
                return _modules == null ? null : string.Join(",", _modules);
            }
            set
            {
                _modules = value == null ? null : value.Split(',');
            }
        }

        public string StorageEngines
        {
            get
            {
                return _storageEngines == null ? null : string.Join(",", _storageEngines);
            }
            set
            {
                _storageEngines = value == null ? null : value.Split(',');
            }
        }

        public ActionTargets Targets
        {
            get { return ActionTargets.Test; }
        }

        public string VersionLessThan { get; set; }

        // methods
        public void AfterTest(TestDetails details)
        {
            InvokeMethod(details.Fixture, AfterTestMethodName);
        }

        public void BeforeTest(TestDetails details)
        {
            EnsureAuthentication();
            EnsureVersion();
            EnsureClusterTypes();
            EnsureModules();
            EnsureStorageEngine();

            InvokeMethod(details.Fixture, BeforeTestMethodName);
        }

        private void InvokeMethod(object fixture, string methodName)
        {
            if (methodName != null)
            {
                var fixtureType = fixture.GetType();
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                var methodInfo = fixtureType.GetMethod(methodName, bindingFlags);
                if (methodInfo == null)
                {
                    var message = string.Format("Type '{0}' does not contain a method named '{1}'.", fixtureType.Name, methodName);
                    Assert.Fail(message);
                }
                methodInfo.Invoke(methodInfo.IsStatic ? null : fixture, new object[0]);
            }
        }

        private void EnsureAuthentication()
        {
            if (Authentication == AuthenticationRequirement.Unspecified)
            {
                return;
            }

            if (Authentication == AuthenticationRequirement.On)
            {
                if (CoreTestConfiguration.ConnectionString.Username == null)
                {
                    Assert.Ignore("Requires authentication but no credentials were provided.");
                }
            }
            else
            {
                if (CoreTestConfiguration.ConnectionString.Username != null)
                {
                    Assert.Ignore("Requires no authentication but credentials were provided.");
                }
            }
        }

        private void EnsureClusterTypes()
        {
            if (ClusterTypes == ClusterTypes.Any)
            {
                return;
            }

            var clusterType = CoreTestConfiguration.Cluster.Description.Type;
            if ((ClusterTypes & ClusterTypes.Standalone) == ClusterTypes.Standalone && clusterType == ClusterType.Standalone)
            {
                return;
            }

            if ((ClusterTypes & ClusterTypes.ReplicaSet) == ClusterTypes.ReplicaSet && clusterType == ClusterType.ReplicaSet)
            {
                return;
            }

            if ((ClusterTypes & ClusterTypes.Sharded) == ClusterTypes.Sharded && clusterType == ClusterType.Sharded)
            {
                return;
            }

            var message = string.Format("Requires server type of {0}, but is connected to {1}.", ClusterTypes, clusterType);
            Assert.Ignore(message);
        }

        private void EnsureModules()
        {
            if (_modules != null)
            {
                var modules = CoreTestConfiguration.GetModules();

                if (!_modules.All(x => modules.Contains(x)))
                {
                    var message = string.Format("Requires modules [{0}], but currently connected to a server with [{1}] modules.",
                        string.Join(", ", _modules),
                        string.Join(", ", modules));
                    Assert.Ignore(message);
                }
            }
        }

        private void EnsureStorageEngine()
        {
            if (_storageEngines != null)
            {
                var storageEngine = CoreTestConfiguration.GetStorageEngine();
                if (!_storageEngines.Contains(storageEngine))
                {
                    var message = string.Format("Requires storage engine \"{0}\", but currently connected to a server using storage engine \"{1}\".", StorageEngines, storageEngine);
                    Assert.Ignore(message);
                }
            }
        }

        private void EnsureVersion()
        {
            if (MinimumVersion != null)
            {
                var minSemanticVersion = SemanticVersion.Parse(MinimumVersion);

                if (CoreTestConfiguration.ServerVersion < minSemanticVersion)
                {
                    var message = string.Format("Requires a minimum server version of {0}, but currently connected to version {1}.", minSemanticVersion, CoreTestConfiguration.ServerVersion);
                    Assert.Ignore(message);
                }
            }

            if (VersionLessThan != null)
            {
                var version = SemanticVersion.Parse(VersionLessThan);

                if (CoreTestConfiguration.ServerVersion >= version)
                {
                    var message = string.Format("Requires a server version less than {0}, but currently connected to version {1}.", version, CoreTestConfiguration.ServerVersion);
                    Assert.Ignore(message);
                }
            }
        }
    }
}
