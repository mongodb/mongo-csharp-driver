/* Copyright 2010-present MongoDB Inc.
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
using System.Runtime.Serialization;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
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

        [Fact]
        public void TestMappingPicksUpAllMembersWithAttributes()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(A));
            Assert.Equal(8, classMap.AllMemberMaps.Count());
        }

        [Fact]
        public void TestSetCreator()
        {
            var classMap = new BsonClassMap<B>(cm =>
            {
                cm.AutoMap();
                cm.SetCreator(() => new B(10));
            });

            classMap.Freeze();

            var instance = (B)classMap.CreateInstance();
            Assert.Equal(10, instance.A);
        }
    }

    public class BsonClassMapMapByNameTests
    {
#pragma warning disable 169 // never used
        private class C
        {
            private string f;
            private string p { get; set; }
        }
#pragma warning restore

        [Fact]
        public void TestMapField()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapField("f"));
            var memberMap = classMap.GetMemberMap("f");
            Assert.NotNull(memberMap);
            Assert.Equal("f", memberMap.ElementName);
            Assert.Equal("f", memberMap.MemberName);
        }

        [Fact]
        public void TestMapIdField()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdField("f")).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.NotNull(idMemberMap);
            Assert.Equal("_id", idMemberMap.ElementName);
            Assert.Equal("f", idMemberMap.MemberName);
        }

        [Fact]
        public void TestMapIdMember()
        {
            var fieldInfo = typeof(C).GetField("f", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var classMap = new BsonClassMap<C>(cm => cm.MapIdMember(fieldInfo)).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.NotNull(idMemberMap);
            Assert.Equal("_id", idMemberMap.ElementName);
            Assert.Equal("f", idMemberMap.MemberName);
        }

        [Fact]
        public void TestMapIdProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdProperty("p")).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.NotNull(idMemberMap);
            Assert.Equal("_id", idMemberMap.ElementName);
            Assert.Equal("p", idMemberMap.MemberName);
        }

        [Fact]
        public void TestMapMember()
        {
            var fieldInfo = typeof(C).GetField("f", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var classMap = new BsonClassMap<C>(cm => cm.MapMember(fieldInfo));
            var memberMap = classMap.GetMemberMap("f");
            Assert.NotNull(memberMap);
            Assert.Equal("f", memberMap.ElementName);
            Assert.Equal("f", memberMap.MemberName);
        }

        [Fact]
        public void TestMapProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapProperty("p"));
            var memberMap = classMap.GetMemberMap("p");
            Assert.NotNull(memberMap);
            Assert.Equal("p", memberMap.ElementName);
            Assert.Equal("p", memberMap.MemberName);
        }
    }

    public class BsonClassMapMapByLamdaTests
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public string F;
            public string P { get; set; }
        }
#pragma warning restore

        [Fact]
        public void TestMapField()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapField(c => c.F));
            var memberMap = classMap.GetMemberMap("F");
            Assert.NotNull(memberMap);
            Assert.Equal("F", memberMap.ElementName);
            Assert.Equal("F", memberMap.MemberName);
        }

        [Fact]
        public void TestMapIdField()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdField(c => c.F)).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.NotNull(idMemberMap);
            Assert.Equal("_id", idMemberMap.ElementName);
            Assert.Equal("F", idMemberMap.MemberName);
        }

        [Fact]
        public void TestMapIdMember()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdMember(c => c.F)).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.NotNull(idMemberMap);
            Assert.Equal("_id", idMemberMap.ElementName);
            Assert.Equal("F", idMemberMap.MemberName);
        }

        [Fact]
        public void TestMapIdProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapIdProperty(c => c.P)).Freeze();
            var idMemberMap = classMap.IdMemberMap;
            Assert.NotNull(idMemberMap);
            Assert.Equal("_id", idMemberMap.ElementName);
            Assert.Equal("P", idMemberMap.MemberName);
        }

        [Fact]
        public void TestMapMember()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapMember(c => c.F));
            var memberMap = classMap.GetMemberMap("F");
            Assert.NotNull(memberMap);
            Assert.Equal("F", memberMap.ElementName);
            Assert.Equal("F", memberMap.MemberName);
        }

        [Fact]
        public void TestMapProperty()
        {
            var classMap = new BsonClassMap<C>(cm => cm.MapProperty(c => c.P));
            var memberMap = classMap.GetMemberMap("P");
            Assert.NotNull(memberMap);
            Assert.Equal("P", memberMap.ElementName);
            Assert.Equal("P", memberMap.MemberName);
        }
    }

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

        [Fact]
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
            Assert.Null(classMap.IdMemberMap);
            Assert.Equal(1, classMap.AllMemberMaps.Count());
            var memberMap = classMap.AllMemberMaps.Single();
            Assert.Equal("X", memberMap.MemberName);
        }
    }

    public class BsonClassMapIsClassMapRegisteredTests
    {
        private static bool __testAlreadyRan;

        public class C
        {
            public ObjectId Id;
            public int X;
        }

        [Fact]
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
            Assert.False(BsonClassMap.IsClassMapRegistered(typeof(C)));
            BsonClassMap.RegisterClassMap<C>(cm => { cm.AutoMap(); });
            Assert.True(BsonClassMap.IsClassMapRegistered(typeof(C)));
        }
    }

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

        [Fact]
        public void TestGetRegisteredClassMaps()
        {
            // this unit test can only be run once (per process)
            if (__firstTime)
            {
                Assert.False(BsonClassMap.IsClassMapRegistered(typeof(C)));
                Assert.False(BsonClassMap.IsClassMapRegistered(typeof(D)));
                var classMaps = BsonClassMap.GetRegisteredClassMaps();
                var classMapTypes = classMaps.Select(x => x.ClassType).ToList();
                Assert.False(classMapTypes.Contains(typeof(C)));
                Assert.False(classMapTypes.Contains(typeof(D)));

                BsonClassMap.RegisterClassMap<C>(cm => cm.AutoMap());
                BsonClassMap.RegisterClassMap<D>(cm => cm.AutoMap());

                Assert.True(BsonClassMap.IsClassMapRegistered(typeof(C)));
                Assert.True(BsonClassMap.IsClassMapRegistered(typeof(D)));
                classMaps = BsonClassMap.GetRegisteredClassMaps();
                classMapTypes = classMaps.Select(x => x.ClassType).ToList();
                Assert.True(classMapTypes.Contains(typeof(C)));
                Assert.True(classMapTypes.Contains(typeof(D)));

                __firstTime = false;
            }
        }
    }

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

        [Fact]
        public void TestShouldSerializeSuccess()
        {
            // test that the value is not serialized
            var c = new ClassA();
            c.AsOfUtc = DateTime.MinValue;
            c.LocalName = "Saleem";
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'LocalName' : 'Saleem' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            // test that the value is serialized
            var date = new DateTime(2011, 4, 24, 0, 0, 0, DateTimeKind.Utc);
            c.AsOfUtc = date;
            expected = "{ 'LocalName' : 'Saleem', 'AsOfUtc' : ISODate('2011-04-24T00:00:00Z') }".Replace("'", "\"");
            json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            Assert.Equal(expected, json);
        }

    }

    public class NonStandardIdTests
    {
        public class TestClass
        {
            [BsonId]
            public int MyId;
            public int Id;
        }

        [Fact]
        public void TestConventionsMapTheCorrectId()
        {
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());

            Assert.Equal("MyId", classMap.IdMemberMap.MemberName);
        }
    }

    public class BsonClassMapResetTests
    {
        [Fact]
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

            classMap.CreateInstance();
            Assert.Equal(0, classMap.DeclaredMemberMaps.Count());
            Assert.Equal("TestClass", classMap.Discriminator);
            Assert.False(classMap.DiscriminatorIsRequired);
            Assert.Null(classMap.ExtraElementsMemberMap);
            Assert.Null(classMap.IdMemberMap);
            Assert.True(classMap.IgnoreExtraElements);
            Assert.False(classMap.IgnoreExtraElementsIsInherited);
            Assert.False(classMap.IsRootClass);
            Assert.Equal(0, classMap.KnownTypes.Count());
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

    public class BsonClassMapEqualsTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = CreateBsonClassMap(typeof(C));
            var y = CreateDerivedFromBsonClassMap(typeof(C));

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = CreateBsonClassMap(typeof(C));

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = CreateBsonClassMap(typeof(C));
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = CreateBsonClassMap(typeof(C));

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = CreateBsonClassMap(typeof(C));
            var y = Clone(x);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("baseClassMap")]
        [InlineData("classType")]
        [InlineData("creator")]
        [InlineData("creatorMaps")]
        [InlineData("declaredMemberMaps")]
        [InlineData("discriminator")]
        [InlineData("discriminatorIsRequired")]
        [InlineData("extraElementsMemberIndex")]
        [InlineData("extraElementsMemberMap")]
        [InlineData("frozen")]
        [InlineData("hasRootClass")]
        [InlineData("idMemberMap")]
        [InlineData("ignoreExtraElements")]
        [InlineData("ignoreExtraElementsIsInherited")]
        [InlineData("isRootClass")]
        [InlineData("knownTypes")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var x = CreateBsonClassMap(typeof(C));
            var y = notEqualFieldName switch
            {
                "baseClassMap" => WithBaseClassMap(x, null),
                "classType" => WithClassType(x, typeof(D)),
                "creator" => WithCreator(x, () => null),
                "creatorMaps" => WithCreatorMaps(x, null),
                "declaredMemberMaps" => WithDeclaredMemberMaps(x, null),
                "discriminator" => WithDiscriminator(x, null),
                "discriminatorIsRequired" => WithDiscriminatorIsRequired(x, false),
                "extraElementsMemberIndex" => WithExtraElementsMemberIndex(x, 1),
                "extraElementsMemberMap" => WithExtraElementsMemberMap(x, null),
                "frozen" => WithFrozen(x, false),
                "hasRootClass" => WithHasRootClass(x, false),
                "idMemberMap" => WithIdMemberMap(x, null),
                "ignoreExtraElements" => WithIgnoreExtraElements(x, false),
                "ignoreExtraElementsIsInherited" => WithIgnoreExtraElementsIsInherited(x, false),
                "isRootClass" => WithIsRootClass(x, false),
                "knownTypes" => WithKnownTypes(x, null),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = CreateBsonClassMap(typeof(C));

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        private BsonClassMap CreateBsonClassMap(Type classType)
        {
            var classMap = new BsonClassMap(classType);
            classMap.AutoMap();
            classMap.SetCreator(() => new BsonClassMap(classType));
            classMap.Freeze();
            return classMap;
        }

        private BsonClassMap WithBaseClassMap(BsonClassMap classMap, BsonClassMap value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_baseClassMap", value);
            return clone;
        }

        private BsonClassMap WithClassType(BsonClassMap classMap, Type value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_classType", value);
            return clone;
        }

        private BsonClassMap WithCreator(BsonClassMap classMap, Func<object> value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_creator", value);
            return clone;
        }

        private BsonClassMap WithCreatorMaps(BsonClassMap classMap, List<BsonCreatorMap> value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_creatorMaps", value);
            return clone;
        }

        private BsonClassMap WithDeclaredMemberMaps(BsonClassMap classMap, List<BsonMemberMap> value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_declaredMemberMaps", value);
            return clone;
        }

        private BsonClassMap WithDiscriminator(BsonClassMap classMap, string value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_discriminator", value);
            return clone;
        }

        private BsonClassMap WithDiscriminatorIsRequired(BsonClassMap classMap, bool value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_discriminatorIsRequired", value);
            return clone;
        }

        private BsonClassMap WithExtraElementsMemberIndex(BsonClassMap classMap, int value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_extraElementsMemberIndex", value);
            return clone;
        }

        private BsonClassMap WithExtraElementsMemberMap(BsonClassMap classMap, BsonMemberMap value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_extraElementsMemberMap", value);
            return clone;
        }

        private BsonClassMap WithFrozen(BsonClassMap classMap, bool value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_frozen", value);
            return clone;
        }

        private BsonClassMap WithHasRootClass(BsonClassMap classMap, bool value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_hasRootClass", value);
            return clone;
        }

        private BsonClassMap WithIdMemberMap(BsonClassMap classMap, BsonMemberMap value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_idMemberMap", value);
            return clone;
        }

        private BsonClassMap WithIgnoreExtraElements(BsonClassMap classMap, bool value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_ignoreExtraElements", value);
            return clone;
        }

        private BsonClassMap WithIgnoreExtraElementsIsInherited(BsonClassMap classMap, bool value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_ignoreExtraElementsIsInherited", value);
            return clone;
        }

        private BsonClassMap WithIsRootClass(BsonClassMap classMap, bool value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_isRootClass", value);
            return clone;
        }

        private BsonClassMap WithKnownTypes(BsonClassMap classMap, List<Type> value)
        {
            var clone = Clone(classMap);
            Reflector.SetFieldValue(classMap, "_knownTypes", value);
            return clone;
        }

        private BsonClassMap Clone(BsonClassMap classMap)
        {
            var clone = (BsonClassMap)FormatterServices.GetUninitializedObject(classMap.GetType());
            Reflector.SetFieldValue(clone, "_baseClassMap", Reflector.GetFieldValue(classMap, "_baseClassMap"));
            Reflector.SetFieldValue(clone, "_classType", Reflector.GetFieldValue(classMap, "_classType"));
            Reflector.SetFieldValue(clone, "_creator", Reflector.GetFieldValue(classMap, "_creator"));
            Reflector.SetFieldValue(clone, "_creatorMaps", Reflector.GetFieldValue(classMap, "_creatorMaps"));
            Reflector.SetFieldValue(clone, "_declaredMemberMaps", Reflector.GetFieldValue(classMap, "_declaredMemberMaps"));
            Reflector.SetFieldValue(clone, "_discriminator", Reflector.GetFieldValue(classMap, "_discriminator"));
            Reflector.SetFieldValue(clone, "_discriminatorIsRequired", Reflector.GetFieldValue(classMap, "_discriminatorIsRequired"));
            Reflector.SetFieldValue(clone, "_extraElementsMemberIndex", Reflector.GetFieldValue(classMap, "_extraElementsMemberIndex"));
            Reflector.SetFieldValue(clone, "_extraElementsMemberMap", Reflector.GetFieldValue(classMap, "_extraElementsMemberMap"));
            Reflector.SetFieldValue(clone, "_frozen", Reflector.GetFieldValue(classMap, "_frozen"));
            Reflector.SetFieldValue(clone, "_hasRootClass", Reflector.GetFieldValue(classMap, "_hasRootClass"));
            Reflector.SetFieldValue(clone, "_idMemberMap", Reflector.GetFieldValue(classMap, "_idMemberMap"));
            Reflector.SetFieldValue(clone, "_ignoreExtraElements", Reflector.GetFieldValue(classMap, "_ignoreExtraElements"));
            Reflector.SetFieldValue(clone, "_ignoreExtraElementsIsInherited", Reflector.GetFieldValue(classMap, "_ignoreExtraElementsIsInherited"));
            Reflector.SetFieldValue(clone, "_isRootClass", Reflector.GetFieldValue(classMap, "_isRootClass"));
            Reflector.SetFieldValue(clone, "_knownTypes", Reflector.GetFieldValue(classMap, "_knownTypes"));
            return clone;
        }

        private BsonClassMap CreateDerivedFromBsonClassMap(Type classType)
        {
            var classMap = new DerivedFromBsonClassMap(classType);
            classMap.AutoMap();
            classMap.Freeze();
            return classMap;
        }

        private class DerivedFromBsonClassMap : BsonClassMap
        {
            public DerivedFromBsonClassMap(Type classType) : base(classType)
            {
            }
        }

        [BsonDiscriminator(Required = true, RootClass = true)]
        [BsonIgnoreExtraElements(Inherited = true)]
        private class C
        {
            [BsonConstructor]
            public C() { }
            public int Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            [BsonExtraElements]
            BsonDocument ExtraElements { get; set; }
        }

        private class D : C
        {
        }
    }
}
