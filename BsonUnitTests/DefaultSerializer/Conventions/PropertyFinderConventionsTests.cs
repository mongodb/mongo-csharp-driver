/* Copyright 2010-2012 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class PropertyFinderConventionsTests
    {
        private class TestClass
        {
            public string Public { get; set; }
            public string PrivateWrite { get; private set; }
            public string PrivateRead { private get; set; }
            private string NotFound { get; set; }
        }

        [Test]
        public void TestPublicPropertyFinderConvention()
        {
            var convention = new PublicMemberFinderConvention();

            var properties = convention.FindMembers(typeof(TestClass)).ToList();

            Assert.AreEqual(3, properties.Count);
            Assert.IsTrue(properties.Any(x => x.Name == "Public"));
            Assert.IsTrue(properties.Any(x => x.Name == "PrivateWrite"));
            Assert.IsTrue(properties.Any(x => x.Name == "PrivateRead"));
        }
    }
}
