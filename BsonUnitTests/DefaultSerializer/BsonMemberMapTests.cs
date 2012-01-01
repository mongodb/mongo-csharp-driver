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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class BsonMemberMapTests
    {
        private class TestClass
        {
            public int Field;

            public int Property { get; set; }

            public int ReadOnlyProperty { get; private set; }

            public TestClass()
            {
                ReadOnlyProperty = 10;
            }
        }

        [Test]
        public void TestGettingAField()
        {
            var instance = new TestClass { Field = 42 };
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Field");

            int value = (int)memberMap.Getter(instance);

            Assert.AreEqual(42, value);
        }

        [Test]
        public void TestSettingAField()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Field");

            memberMap.Setter(instance, 42);

            Assert.AreEqual(42, instance.Field);
        }

        [Test]
        public void TestGettingAProperty()
        {
            var instance = new TestClass { Property = 42 };
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Property");

            int value = (int)memberMap.Getter(instance);

            Assert.AreEqual(42, value);
        }

        [Test]
        public void TestSettingAProperty()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("Property");

            memberMap.Setter(instance, 42);

            Assert.AreEqual(42, instance.Property);
        }

        [Test]
        public void TestGettingAReadOnlyProperty()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("ReadOnlyProperty");

            int value = (int)memberMap.Getter(instance);

            Assert.AreEqual(10, value);
        }

        [Test]
        public void TestSettingAReadOnlyProperty()
        {
            var instance = new TestClass();
            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("ReadOnlyProperty");

            memberMap.Setter(instance, 42);

            Assert.AreEqual(42, instance.ReadOnlyProperty);
        }
    }
}
