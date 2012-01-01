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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class IdGeneratorConventionsTests
    {
        private class TestClass
        {
            public Guid GuidId { get; set; }
            public ObjectId ObjectId { get; set; }
        }

        [Test]
        public void TestLookupIdGeneratorConvention()
        {
            var convention = new LookupIdGeneratorConvention();

            var guidProperty = typeof(TestClass).GetProperty("GuidId");
            var objectIdProperty = typeof(TestClass).GetProperty("ObjectId");

            Assert.IsInstanceOf<GuidGenerator>(convention.GetIdGenerator(guidProperty));
            Assert.IsInstanceOf<ObjectIdGenerator>(convention.GetIdGenerator(objectIdProperty));
        }
    }
}
