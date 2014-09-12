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

using System.Linq;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver
{
    public abstract class CollectionUsingSpecification : CollectionUsingTest
    {
        // methods
        [TestFixtureSetUp]
        public void CollectionUsingSpecificationSetUp()
        {
            Given();
            When();
        }

        protected virtual void Given()
        {
        }

        protected abstract void When();

        protected static class Require
        {
            public static void MinimumServerVersion(string version)
            {
                var minVersion = SemanticVersion.Parse(version);

                var firstServer = SuiteConfiguration.Cluster.Description.Servers.First();
                if (firstServer.Version < minVersion)
                {
                    var message = string.Format("Requires a minimum server version of {0}, but currently connected to {1}.", minVersion, firstServer.Version);
                    Assert.Ignore(message);
                }
            }
        }
    }
}
