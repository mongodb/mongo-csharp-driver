/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

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
#pragma warning disable 414 // fieldMappedByAttribute2 is assigned but its value is never used
            [BsonElement]
            private readonly int fieldMappedByAttribute2;
#pragma warning restore

            public int PropertyMapped { get; set; }
            public int PropertyMapped2 { get; private set; }
            public int PropertyMapped3 { private get; set; }

            private int PropertyNotMapped { get; set; }

            [BsonElement("PropertyMappedByAttribute")]
            private int PropertyMappedByAttribute { get; set; }

            [BsonElement]
            public int PropertyMappedByAttribute2
            {
                get { return PropertyMapped + 1; }
            }

            public A()
            {
                fieldMappedByAttribute2 = 10;
            }
        }

        private class B
        {
            public int A { get; set; }

            public B(int a)
            {
                A = a;
            }
        }
#pragma warning restore

        [Test]
        public void TestMappingPicksUpAllMembersWithAttributes()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(A));
            Assert.AreEqual(8, classMap.AllMemberMaps.Count());
        }

        [Test]
        public void TestSetCreator()
        {
            var classMap = new BsonClassMap<B>(cm =>
            {
                cm.AutoMap();
                cm.SetCreator(() => new B(10));
            });

            classMap.Freeze();

            var instance = (B)classMap.CreateInstance();
            Assert.AreEqual(10, instance.A);
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
            var classMap = new BsonClassMap<C>(cm => cm.MapIdField("f")).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("f", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapIdMember()
        {
            var fieldInfo = typeof(C).GetField("f", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var classMap = new BsonClassMap<C>(cm => cm.MapIdMember(fieldInfo)).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("f", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapIdProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdProperty("p")).Freeze();
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
            var classMap = new BsonClassMap<C>(cm => cm.MapIdField(c => c.F)).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("F", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapIdMember()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdMember(c => c.F)).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.IsNotNull(idMemberMap);
            Assert.AreEqual("_id", idMemberMap.ElementName);
            Assert.AreEqual("F", idMemberMap.MemberName);
        }

        [Test]
        public void TestMapIdProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdProperty(c => c.P)).Freeze();
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
            Assert.AreEqual(1, classMap.AllMemberMaps.Count());
            var memberMap = classMap.AllMemberMaps.Single();
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
    public class BsonClassMapGetRegisteredClassMapTests
    {
        private static bool __firstTime = true;

        public class C
        {
            public ObjectId Id;
            public int X;
        }

        public class D
        {
            public ObjectId Id;
            public string Y;
        }

        [Test]
        public void TestGetRegisteredClassMaps()
        {
            // this unit test can only be run once (per process)
            if (__firstTime)
            {
                Assert.IsFalse(BsonClassMap.IsClassMapRegistered(typeof(C)));
                Assert.IsFalse(BsonClassMap.IsClassMapRegistered(typeof(D)));
                var classMaps = BsonClassMap.GetRegisteredClassMaps();
                var classMapTypes = classMaps.Select(x => x.ClassType).ToList();
                Assert.IsFalse(classMapTypes.Contains(typeof(C)));
                Assert.IsFalse(classMapTypes.Contains(typeof(D)));

                BsonClassMap.RegisterClassMap<C>(cm => cm.AutoMap());
                BsonClassMap.RegisterClassMap<D>(cm => cm.AutoMap());

                Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(C)));
                Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(D)));
                classMaps = BsonClassMap.GetRegisteredClassMaps();
                classMapTypes = classMaps.Select(x => x.ClassType).ToList();
                Assert.IsTrue(classMapTypes.Contains(typeof(C)));
                Assert.IsTrue(classMapTypes.Contains(typeof(D)));

                __firstTime = false;
            }
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

    [TestFixture]
    public class NonStandardIdTests
    {
        public class TestClass
        {
            [BsonId]
            public int MyId;
            public int Id;
        }

        [Test]
        public void TestConventionsMapTheCorrectId()
        {
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());

            Assert.AreEqual("MyId", classMap.IdMemberMap.MemberName);
        }
    }

    [TestFixture]
    public class BsonClassMapResetTests
    {
        [Test]
        public void TestAllValuesGoBackToTheirDefaults()
        {
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.SetCreator(() => { throw new Exception("will get reset."); });
                cm.MapMember(x => x.String);
                cm.SetDiscriminator("blah");
                cm.SetDiscriminatorIsRequired(true);
                cm.MapExtraElementsMember(x => x.ExtraElements);
                cm.MapIdMember(x => x.OId);
                cm.SetIgnoreExtraElements(false);
                cm.SetIgnoreExtraElementsIsInherited(true);
                cm.SetIsRootClass(true);
                cm.AddKnownType(typeof(InheritedTestClass));
            });

            classMap.Reset();

            classMap.Freeze();

            Assert.DoesNotThrow(() => classMap.CreateInstance());
            Assert.AreEqual(0, classMap.DeclaredMemberMaps.Count());
            Assert.AreEqual("TestClass", classMap.Discriminator);
            Assert.IsFalse(classMap.DiscriminatorIsRequired);
            Assert.IsNull(classMap.ExtraElementsMemberMap);
            Assert.IsNull(classMap.IdMemberMap);
            Assert.IsTrue(classMap.IgnoreExtraElements);
            Assert.IsFalse(classMap.IgnoreExtraElementsIsInherited);
            Assert.IsFalse(classMap.IsRootClass);
            Assert.AreEqual(0, classMap.KnownTypes.Count());
        }

        private class TestClass
        {
            public ObjectId OId { get; set; }

            public string String { get; set; }

            public Dictionary<string, object> ExtraElements { get; set; }
        }

        private class InheritedTestClass : TestClass
        { }
    }
}
