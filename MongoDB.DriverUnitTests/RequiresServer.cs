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
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequiresServerAttribute : CategoryAttribute, ITestAction
    {
        // fields
        private readonly string _afterTestMethodName;
        private readonly string _beforeTestMethodName;
        private IList<string> _storageEngines;

        // constructors
        public RequiresServerAttribute(string beforeTestMethodName = null, string afterTestMethodName = null)
            : base("RequiresServer")
        {
            _beforeTestMethodName = beforeTestMethodName;
            _afterTestMethodName = afterTestMethodName;
        }

        // properties

        public string MinimumVersion { get; set; }

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
            InvokeMethod(details.Fixture, _afterTestMethodName);
        }

        public void BeforeTest(TestDetails details)
        {
            EnsureVersion();
            EnsureStorageEngine();

            InvokeMethod(details.Fixture, _beforeTestMethodName);
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
