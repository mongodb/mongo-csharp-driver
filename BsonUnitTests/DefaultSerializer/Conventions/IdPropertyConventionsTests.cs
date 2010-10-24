/* Copyright 2010 10gen Inc.
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
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Bson.DefaultSerializer.Conventions;

namespace MongoDB.BsonUnitTests.DefaultSerializer.Conventions {
    [TestFixture]
    public class IdPropertyConventionsTests {
        private class TestClassA {
            public Guid Id { get; set; }
            public ObjectId OtherId { get; set; }
        }

        private class TestClassB {
            public ObjectId OtherId { get; set; }
        }

        [Test]
        public void TestIdPropertyConvention() {
            var convention = new NamedIdPropertyConvention("Id");

            var classAMap = BsonClassMap.RegisterClassMap<TestClassA>();
            var idMap = convention.FindIdPropertyMap(classAMap.PropertyMaps);
            Assert.IsNotNull(idMap);
            Assert.AreEqual("Id", idMap.PropertyName);

            var classBMap = BsonClassMap.RegisterClassMap<TestClassB>();
            idMap = convention.FindIdPropertyMap(classBMap.PropertyMaps);
            Assert.IsNull(idMap);
        }
    }
}
