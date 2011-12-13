/* Copyright 2010-2011 10gen Inc.
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
using NUnit.Framework;

using MongoDB.Driver;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoServerTests
    {
        [Test]
        public void TestCreateNoArgs()
        {
            var server = MongoServer.Create(); // no args!
            var expectedSeedList = new[] { new MongoServerAddress("localhost") };
            Assert.IsNull(server.Settings.DefaultCredentials);
            Assert.AreEqual(1, server.Instances.Count()); // Instance is created immediately but not connected
            Assert.AreEqual(MongoDefaults.GuidRepresentation, server.Settings.GuidRepresentation);
            Assert.AreEqual(SafeMode.False, server.Settings.SafeMode);
            Assert.AreEqual(false, server.Settings.SlaveOk);
            Assert.AreEqual(MongoServerState.Disconnected, server.State);
            Assert.IsTrue(expectedSeedList.SequenceEqual(server.Settings.Servers));
        }
    }
}
