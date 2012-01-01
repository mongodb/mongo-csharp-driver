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
using System.Reflection;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class BsonClassMapTests
    {
#pragma warning disable 169 // never used
#pragma warning disable 649 // never assigned to
        private class A
        {
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
#pragma warning restore

        [Test]
        public void TestMappingPicksUpAllMembersWithAttributes()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(A));
            Assert.AreEqual(6, classMap.MemberMaps.Count());
        }
    }

    [TestFixture]
    public class BsonClassMapMapByNameTests
    {
#pragma warning disable 169 // never used
        private class C
        {
            private string f;
            private string p { get; set; }
        }
#pragma warning restore

        [Test]
        public void TestMapField()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapField("f"));
            var memberMap = classMap.GetMemberMap("f");
            Assert.IsNotNull(memberMap);
            Assert.AreEqual("f", memberMap.ElementName);
            Assert.AreEqual("f", memberMap.MemberName);
        }

        [Test]
        public void TestMapIdField()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdField("f"));
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("f", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapIdMember()
        {
            var fieldInfo = typeof(C).GetField("f", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var classMap = new BsonClassMap<C>(cm => cm.MapIdMember(fieldInfo));
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("f", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapIdProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdProperty("p"));
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("p", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapMember()
        {
            var fieldInfo = typeof(C).GetField("f", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var classMap = new BsonClassMap<C>(cm => cm.MapMember(fieldInfo));
            var memberMap = classMap.GetMemberMap("f");
            Assert.IsNotNull(memberMap);
            Assert.AreEqual("f", memberMap.ElementName);
            Assert.AreEqual("f", memberMap.MemberName);
        }

        [Test]
        public void TestMapProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapProperty("p"));
            var memberMap = classMap.GetMemberMap("p");
            Assert.IsNotNull(memberMap);
            Assert.AreEqual("p", memberMap.ElementName);
            Assert.AreEqual("p", memberMap.MemberName);
        }
    }

    [TestFixture]
    public class BsonClassMapMapByLamdaTests
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public string F;
            public string P { get; set; }
        }
#pragma warning restore

        [Test]
        public void TestMapField()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapField(c => c.F));
            var memberMap = classMap.GetMemberMap("F");
            Assert.IsNotNull(memberMap);
            Assert.AreEqual("F", memberMap.ElementName);
            Assert.AreEqual("F", memberMap.MemberName);
        }

        [Test]
        public void TestMapIdField()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdField(c => c.F));
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("F", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapIdMember()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdMember(c => c.F));
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("F", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapIdProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdProperty(c => c.P));
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("P", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapMember()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapMember(c => c.F));
            var memberMap = classMap.GetMemberMap("F");
            Assert.IsNotNull(memberMap);
            Assert.AreEqual("F", memberMap.ElementName);
            Assert.AreEqual("F", memberMap.MemberName);
        }

        [Test]
        public void TestMapProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapProperty(c => c.P));
            var memberMap = classMap.GetMemberMap("P");
            Assert.IsNotNull(memberMap);
            Assert.AreEqual("P", memberMap.ElementName);
            Assert.AreEqual("P", memberMap.MemberName);
        }
    }

    [TestFixture]
    public class BsonClassMapUnmapTests
    {
        public class C
        {
            public ObjectId Id;
            public int X;
            public int FieldUnmappedByName;
            public int FieldUnmappedByLambda;
            public int PropertyUnmappedByName { get; set; }
            public int PropertyUnmappedByLambda { get; set; }
        }

        [Test]
        public void TestUnmap()
        {
            var classMap = new BsonClassMap<C>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap("Id"));
                cm.UnmapField("Id");
                cm.UnmapField("FieldUnmappedByName");
                cm.UnmapField(c => c.FieldUnmappedByLambda);
                cm.UnmapProperty("PropertyUnmappedByName");
                cm.UnmapProperty(c => c.PropertyUnmappedByLambda);
            });
            classMap.Freeze();
            Assert.IsNull(classMap.IdMemberMap);
            Assert.AreEqual(1, classMap.MemberMaps.Count());
            var memberMap = classMap.MemberMaps.Single();
            Assert.AreEqual("X", memberMap.MemberName);
        }
    }

    [TestFixture]
    public class BsonClassMapIsClassMapRegisteredTests
    {
        private static bool __testAlreadyRan;

        public class C
        {
            public ObjectId Id;
            public int X;
        }

        [Test]
        public void TestIsClassMapRegistered()
        {
            // test can only be run once
            if (__testAlreadyRan)
            {
                return;
            }
            else
            {
                __testAlreadyRan = true;
            }
            Assert.IsFalse(BsonClassMap.IsClassMapRegistered(typeof(C)));
            BsonClassMap.RegisterClassMap<C>(cm => { cm.AutoMap(); });
            Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(C)));
        }
    }

    [TestFixture]
    public class BsonShouldSerializeTests
    {
        public class ClassA
        {
            public string LocalName { get; set; }
            public DateTime AsOfUtc { get; set; }

            public bool ShouldSerializeAsOfUtc()
            {
                return this.AsOfUtc != DateTime.MinValue;
            }
        }

        [Test]
        public void TestShouldSerializeSuccess()
        {
            // test that the value is not serialized
            var c = new ClassA();
            c.AsOfUtc = DateTime.MinValue;
            c.LocalName = "Saleem";
            var json = c.ToJson();
            var expected = "{ 'LocalName' : 'Saleem' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            // test that the value is serialized
            var date = new DateTime(2011, 4, 24, 0, 0, 0, DateTimeKind.Utc);
            c.AsOfUtc = date;
            expected = "{ 'LocalName' : 'Saleem', 'AsOfUtc' : ISODate('2011-04-24T00:00:00Z') }".Replace("'", "\"");
            json = c.ToJson();
            Assert.AreEqual(expected, json);
        }

    }
}
