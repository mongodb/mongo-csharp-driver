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
using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonMemberMapTests
    {
        private class TestClass
        {
            public readonly int ReadOnlyField;

            public int Field;

            public int Property { get; set; }

            public int PrivateSettableProperty { get; private set; }

            public int ReadOnlyProperty
            {
                get { return Property + 1; }
            }

            public TestClass()
            {
                ReadOnlyField = 13;
                PrivateSettableProperty = 10;
            }
        }

        [Fact]
        public void TestGettingAField()
        {
            var instance = new TestClass { Field = 42 };
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Field");

            int value = (int)memberMap.Getter(instance);

            Assert.Equal(42, value);
        }

        [Fact]
        public void TestIsReadOnlyPropertyOfAField()
        {
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Field");

            Assert.False(memberMap.IsReadOnly);
        }

        [Fact]
        public void TestSetElementNameThrowsWhenElementNameContainsNulls()
        {
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Property");
            Assert.Throws<ArgumentException>(() => { memberMap.SetElementName("a\0b"); });
        }

        [Fact]
        public void TestSetElementNameThrowsWhenElementNameIsNull()
        {
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Property");
            Assert.Throws<ArgumentNullException>(() => { memberMap.SetElementName(null); });
        }

        [Fact]
        public void TestSettingAField()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Field");

            memberMap.Setter(instance, 42);

            Assert.Equal(42, instance.Field);
        }

        [Fact]
        public void TestGettingAReadOnlyField()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.ReadOnlyField);
            });
            var memberMap = classMap.GetMemberMap("ReadOnlyField");

            int value = (int)memberMap.Getter(instance);

            Assert.Equal(13, value);
        }

        [Fact]
        public void TestIsReadOnlyPropertyOfAReadOnlyField()
        {
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.ReadOnlyField);
            });
            var memberMap = classMap.GetMemberMap("ReadOnlyField");

            Assert.True(memberMap.IsReadOnly);
        }

        [Fact]
        public void TestSettingAReadOnlyField()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.ReadOnlyField);
            });
            var memberMap = classMap.GetMemberMap("ReadOnlyField");

            var ex = Record.Exception(() => memberMap.Setter(instance, 12));

            var expectedMessage = "The field 'System.Int32 ReadOnlyField' of class 'MongoDB.Bson.Tests.Serialization.BsonMemberMapTests+TestClass' is readonly. To avoid this exception, call IsReadOnly to ensure that setting a value is allowed.";
            Assert.IsType<BsonSerializationException>(ex);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void TestGettingAProperty()
        {
            var instance = new TestClass { Property = 42 };
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Property");

            int value = (int)memberMap.Getter(instance);

            Assert.Equal(42, value);
        }

        [Fact]
        public void TestIsReadOnlyPropertyOfAProperty()
        {
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Property");

            Assert.False(memberMap.IsReadOnly);
        }

        [Fact]
        public void TestSettingAProperty()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Property");

            memberMap.Setter(instance, 42);

            Assert.Equal(42, instance.Property);
        }

        [Fact]
        public void TestGettingAPrivateSettableProperty()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("PrivateSettableProperty");

            int value = (int)memberMap.Getter(instance);

            Assert.Equal(10, value);
        }

        [Fact]
        public void TestIsReadOnlyPropertyOfAPrivateSettableProperty()
        {
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("PrivateSettableProperty");

            Assert.False(memberMap.IsReadOnly);
        }

        [Fact]
        public void TestSettingAPrivateSettableProperty()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("PrivateSettableProperty");

            memberMap.Setter(instance, 42);

            Assert.Equal(42, instance.PrivateSettableProperty);
        }

        [Fact]
        public void TestGettingAReadOnlyProperty()
        {
            var instance = new TestClass { Property = 10 };
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.ReadOnlyProperty);
            });

            var memberMap = classMap.GetMemberMap("ReadOnlyProperty");

            int value = (int)memberMap.Getter(instance);

            Assert.Equal(11, value);
        }

        [Fact]
        public void TestIsReadOnlyPropertyOfAReadOnlyProperty()
        {
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.ReadOnlyProperty);
            });
            var memberMap = classMap.GetMemberMap("ReadOnlyProperty");

            Assert.True(memberMap.IsReadOnly);
        }

        [Fact]
        public void TestSettingAReadOnlyProperty()
        {
            var instance = new TestClass { Property = 10 };
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.ReadOnlyProperty);
            });
            var memberMap = classMap.GetMemberMap("ReadOnlyProperty");

            var ex = Record.Exception(() => memberMap.Setter(instance, 12));

            var expectedMessage = "The property 'System.Int32 ReadOnlyProperty' of class 'MongoDB.Bson.Tests.Serialization.BsonMemberMapTests+TestClass' has no 'set' accessor. To avoid this exception, call IsReadOnly to ensure that setting a value is allowed.";
            Assert.IsType<BsonSerializationException>(ex);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void TestReset()
        {
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.MapMember(c => c.Property);
            });

            var originalSerializer = new Int32Serializer();

            var memberMap = classMap.GetMemberMap(x => x.Property);
            memberMap.SetDefaultValue(42);
            memberMap.SetElementName("oops");
            memberMap.SetIdGenerator(new GuidGenerator());
            memberMap.SetIgnoreIfDefault(true);
            memberMap.SetIsRequired(true);
            memberMap.SetOrder(21);
            memberMap.SetSerializer(originalSerializer);
            memberMap.SetShouldSerializeMethod(o => false);

            memberMap.Reset();

            Assert.Equal(0, (int)memberMap.DefaultValue);
            Assert.Equal("Property", memberMap.ElementName);
            Assert.Null(memberMap.IdGenerator);
            Assert.False(memberMap.IgnoreIfDefault);
            Assert.False(memberMap.IgnoreIfNull);
            Assert.False(memberMap.IsRequired);
            Assert.Equal(int.MaxValue, memberMap.Order);
            Assert.NotSame(originalSerializer, memberMap.GetSerializer());
            Assert.Null(memberMap.ShouldSerializeMethod);
        }
    }

    public class BsonMemberMapEqualsTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = CreateBsonMemberMap();
            var y = new DerivedFromBsonMemberMap(x.ClassMap, x.MemberInfo);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = CreateBsonMemberMap();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = CreateBsonMemberMap();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = CreateBsonMemberMap();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = CreateBsonMemberMap();
            var y = Clone(x);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("defaultValue")]
        [InlineData("defaultValueCreator")]
        [InlineData("defaultValueSpecified")]
        [InlineData("elementName")]
        [InlineData("frozen")]
        [InlineData("idGenerator")]
        [InlineData("ignoreIfDefault")]
        [InlineData("ignoreIfNull")]
        [InlineData("isRequired")]
        [InlineData("memberInfo")]
        [InlineData("order")]
        [InlineData("serializer")]
        [InlineData("shouldSerializeMethod")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var x = CreateBsonMemberMap();
            var y = notEqualFieldName switch
            {
                "defaultValue" => WithDefaultValue(x, 1),
                "defaultValueCreator" => WithDefaultValueCreator(x, () => 1),
                "defaultValueSpecified" => WithDefaultValueSpecified(x, true),
                "elementName" => WithElementName(x, null),
                "frozen" => WithFrozen(x, false),
                "idGenerator" => WithIdGenerator(x, Mock.Of<IIdGenerator>()),
                "ignoreIfDefault" => WithIgnoreIfDefault(x, true),
                "ignoreIfNull" => WithIgnoreIfNull(x, true),
                "isRequired" => WithIsRequired(x, true),
                "memberInfo" => WithMemberInfo(x, null),
                "order" => WithOrder(x, 1),
                "serializer" => WithSerializerMethod(x, new Int32Serializer(BsonType.String)),
                "shouldSerializeMethod" => WithShouldSerializeMethod(x, x => true),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(notEqualFieldName == null ? true : false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = CreateBsonMemberMap();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        private BsonMemberMap CreateBsonMemberMap()
        {
            var classMap = new BsonClassMap(typeof(C));
            classMap.AutoMap();
            var memberMap = classMap.GetMemberMap("X");
            memberMap.SetSerializer(Int32Serializer.Instance);
            classMap.Freeze(); // also freezes the member
            return memberMap;
        }

        private BsonMemberMap Clone(BsonMemberMap memberMap)
        {
            var clone = (BsonMemberMap)FormatterServices.GetUninitializedObject(memberMap.GetType());
            Reflector.SetFieldValue(clone, "_classMap", Reflector.GetFieldValue(memberMap, "_classMap"));
            Reflector.SetFieldValue(clone, "_defaultValue", Reflector.GetFieldValue(memberMap, "_defaultValue"));
            Reflector.SetFieldValue(clone, "_defaultValueCreator", Reflector.GetFieldValue(memberMap, "_defaultValueCreator"));
            Reflector.SetFieldValue(clone, "_defaultValueSpecified", Reflector.GetFieldValue(memberMap, "_defaultValueSpecified"));
            Reflector.SetFieldValue(clone, "_elementName", Reflector.GetFieldValue(memberMap, "_elementName"));
            Reflector.SetFieldValue(clone, "_frozen", Reflector.GetFieldValue(memberMap, "_frozen"));
            Reflector.SetFieldValue(clone, "_idGenerator", Reflector.GetFieldValue(memberMap, "_idGenerator"));
            Reflector.SetFieldValue(clone, "_ignoreIfDefault", Reflector.GetFieldValue(memberMap, "_ignoreIfDefault"));
            Reflector.SetFieldValue(clone, "_ignoreIfNull", Reflector.GetFieldValue(memberMap, "_ignoreIfNull"));
            Reflector.SetFieldValue(clone, "_isRequired", Reflector.GetFieldValue(memberMap, "_isRequired"));
            Reflector.SetFieldValue(clone, "_memberInfo", Reflector.GetFieldValue(memberMap, "_memberInfo"));
            Reflector.SetFieldValue(clone, "_order", Reflector.GetFieldValue(memberMap, "_order"));
            Reflector.SetFieldValue(clone, "_serializer", Reflector.GetFieldValue(memberMap, "_serializer"));
            Reflector.SetFieldValue(clone, "_shouldSerializeMethod", Reflector.GetFieldValue(memberMap, "_shouldSerializeMethod"));
            return clone;
        }

        private BsonMemberMap WithDefaultValue(BsonMemberMap memberMap, object value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_defaultValue", value);
            return clone;
        }

        private BsonMemberMap WithDefaultValueCreator(BsonMemberMap memberMap, Func<object> value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_defaultValueCreator", value);
            return clone;
        }

        private BsonMemberMap WithDefaultValueSpecified(BsonMemberMap memberMap, bool value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_defaultValueSpecified", value);
            return clone;
        }

        private BsonMemberMap WithElementName(BsonMemberMap memberMap, string value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_elementName", value);
            return clone;
        }

        private BsonMemberMap WithFrozen(BsonMemberMap memberMap, bool value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_frozen", value);
            return clone;
        }

        private BsonMemberMap WithIdGenerator(BsonMemberMap memberMap, IIdGenerator value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_idGenerator", value);
            return clone;
        }

        private BsonMemberMap WithIgnoreIfDefault(BsonMemberMap memberMap, bool value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_ignoreIfDefault", value);
            return clone;
        }

        private BsonMemberMap WithIgnoreIfNull(BsonMemberMap memberMap, bool value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_ignoreIfNull", value);
            return clone;
        }

        private BsonMemberMap WithIsRequired(BsonMemberMap memberMap, bool value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_isRequired", value);
            return clone;
        }

        private BsonMemberMap WithMemberInfo(BsonMemberMap memberMap, MemberInfo value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_memberInfo", value);
            return clone;
        }

        private BsonMemberMap WithOrder(BsonMemberMap memberMap, int value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_order", value);
            return clone;
        }

        private BsonMemberMap WithSerializerMethod(BsonMemberMap memberMap, IBsonSerializer value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_serializer", value);
            return clone;
        }

        private BsonMemberMap WithShouldSerializeMethod(BsonMemberMap memberMap, Func<object, bool> value)
        {
            var clone = Clone(memberMap);
            Reflector.SetFieldValue(memberMap, "_shouldSerializeMethod", value);
            return clone;
        }

        private class DerivedFromBsonMemberMap : BsonMemberMap
        {
            public DerivedFromBsonMemberMap(BsonClassMap classMap, MemberInfo memberInfo) : base(classMap, memberInfo)
            {
            }
        }

        private class C
        {
            public int X { get; set; }
        }
    }
}
