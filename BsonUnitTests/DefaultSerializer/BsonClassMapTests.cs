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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.DefaultSerializer;

namespace MongoDB.BsonUnitTests.DefaultSerializer {
    [TestFixture]
    public class BsonClassMapTests {
        private class C {
            public short SD { get; set; }
            [BsonUseCompactRepresentation(false)]
            public short SF { get; set; }
            [BsonUseCompactRepresentation(true)]
            public short SC { get; set; }
        }

        [Test]
        public void TestInt16UseCompactRepresentation() {
            var classMap = BsonClassMap.RegisterClassMap<C>();
            var sdMemberMap = classMap.GetMemberMap("SD");
            var sfMemberMap = classMap.GetMemberMap("SF");
            var scMemberMap = classMap.GetMemberMap("SC");
            Assert.AreEqual(true, sdMemberMap.UseCompactRepresentation);
            Assert.AreEqual(false, sfMemberMap.UseCompactRepresentation);
            Assert.AreEqual(true, scMemberMap.UseCompactRepresentation);
        }

        private class A {
            private int fieldNotMapped;
            public readonly int FieldNotMapped2;
            public int FieldMapped;
            [BsonElement("FieldMappedByAttribute")]
            private int fieldMappedByAttribute;
            
            public int PropertyMapped { get; set; }
            public int PropertyMapped2 { get; private set; }
            public int PropertyMapped3 { private get; set; }

            private int PropertyNotMapped { get; set; }

            [BsonElement("PropertyMappedByAttribute")]
            private int PropertyMappedByAttribute { get; set; }
        }

        [Test]
        public void TestMappingPicksUpAllMembersWithAttributes() {
            var classMap = new BsonClassMap<A>(c => c.AutoMap());

            Assert.AreEqual(6, classMap.MemberMaps.Count());
        }
    }
}