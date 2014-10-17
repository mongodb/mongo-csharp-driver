/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    public enum Authentication
    {
        Unspecified,
        On,
        Off
    }

    [Flags]
    public enum ServerTypes
    {
        Standalone = 1,
        ReplicaSetMember = 2,
        ShardRouter = 4,
        StandaloneOrReplicaSetMember = Standalone | ReplicaSetMember,
        StandaloneOrShardRouter = Standalone | ShardRouter,
        ReplicaSetMemberOrShardRouter = ReplicaSetMember | ShardRouter,
        Any = Standalone | ReplicaSetMember | ShardRouter
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequiresServerAttribute : CategoryAttribute, ITestAction
    {
        // fields
        private IList<string> _storageEngines;

        // constructors
        public RequiresServerAttribute()
            : base("RequiresServer")
        {
            ServerTypes = ServerTypes.Any;
        }

        // properties

        public Authentication Authentication { get; set; }

        public string AfterTestMethodName { get; set; }

        public string BeforeTestMethodName { get; set; }

        public string MinimumVersion { get; set; }

        public ServerTypes ServerTypes { get; set; }

        public string StorageEngines
        {
            get
            {
                return _storageEngines == null ? null : string.Join(",", _storageEngines.ToArray());
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
            EnsureServerTypes();
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
            if (Authentication == Authentication.Unspecified)
            {
                return;
            }

            if (Authentication == Authentication.On)
            {
                if (!Configuration.TestClient.Settings.Credentials.Any())
                {
                    Assert.Ignore("Requires authentication, but no credentials were provided.");
                }
            }
            else
            {
                if (Configuration.TestClient.Settings.Credentials.Any())
                {
                    Assert.Ignore("Requires no authentication, but credentials were provided.");
                }
            }
        }

        private void EnsureServerTypes()
        {
            if (ServerTypes == ServerTypes.Any)
            {
                return;
            }

            var instanceType = Configuration.TestServer.Primary.InstanceType;
            if ((ServerTypes & ServerTypes.Standalone) == ServerTypes.Standalone && instanceType == MongoServerInstanceType.StandAlone)
            {
                return;
            }

            if ((ServerTypes & ServerTypes.ReplicaSetMember) == ServerTypes.ReplicaSetMember && instanceType == MongoServerInstanceType.ReplicaSetMember)
            {
                return;
            }

            if ((ServerTypes & ServerTypes.ShardRouter) == ServerTypes.ShardRouter && instanceType == MongoServerInstanceType.ShardRouter)
            {
                return;
            }

            var message = string.Format("Requires server type of {0}, but is connected to {1}.", ServerTypes, instanceType);
            Assert.Ignore(message);
        }

        private void EnsureStorageEngine()
        {
            if (_storageEngines != null)
            {
                var storageEngine = Configuration.GetStorageEngine();
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
                var minVersion = new Version(MinimumVersion);

                if (Configuration.TestServer.BuildInfo.Version < minVersion)
                {
                    var message = string.Format("Requires a minimum server version of {0}, but currently connected to version {1}.", minVersion, Configuration.TestServer.BuildInfo.Version);
                    Assert.Ignore(message);
                }
            }

            if (VersionLessThan != null)
            {
                var version = new Version(MinimumVersion);

                if (Configuration.TestServer.BuildInfo.Version >= version)
                {
                    var message = string.Format("Requires a server version less than {0}, but currently connected to version {1}.", version, Configuration.TestServer.BuildInfo.Version);
                    Assert.Ignore(message);
                }
            }
        }
    }
}
