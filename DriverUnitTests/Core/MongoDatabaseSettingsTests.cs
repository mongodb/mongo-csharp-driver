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

using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.DriverUnitTests {
    [TestFixture]
    public class MongoDatabaseSettingsTests {
        [Test]
        public void TestAll() {
            var settings = new MongoDatabaseSettings(
                "database",
                MongoCredentials.Create("username", "password"),
                GuidRepresentation.CSharpLegacy,
                SafeMode.Create(5, TimeSpan.FromSeconds(5)),
                true
            );

            Assert.AreEqual("database", settings.DatabaseName);
            Assert.AreEqual(MongoCredentials.Create("username", "password"), settings.Credentials);
            Assert.AreEqual(GuidRepresentation.CSharpLegacy, settings.GuidRepresentation);
            Assert.AreEqual(SafeMode.Create(5, TimeSpan.FromSeconds(5)), settings.SafeMode);
            Assert.AreEqual(true, settings.SlaveOk);

            Assert.IsFalse(settings.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();
            Assert.AreEqual(settings, settings);

            settings.Freeze();
            Assert.IsTrue(settings.IsFrozen);
            Assert.AreEqual(hashCode, settings.GetHashCode());
            Assert.AreEqual(stringRepresentation, settings.ToString());
        }
    }
}
