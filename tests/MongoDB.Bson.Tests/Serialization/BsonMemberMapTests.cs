/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
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
}
