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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp637
{
    [TestFixture]
    public class CSharp637Tests
    {
        public interface IMyInterface
        {
            string SomeField1 { get; set; }

            string SomeField2 { get; set; }
        }

        public class MyClass : IMyInterface
        {
            [BsonElement("foo")]
            public string SomeField1 { get; set; }

            [BsonElement("bar")]
            string IMyInterface.SomeField2 { get; set; }
        }

        public static BsonMemberMap MapMyInterface1<T>()
            where T : IMyInterface
        {
            var classMap = new BsonClassMap<T>();
            classMap.AutoMap();
            return classMap.MapMember(t => t.SomeField1);
        }

        public static BsonMemberMap MapMyInterface2<T>()
            where T : IMyInterface
        {
            var classMap = new BsonClassMap<T>();
            classMap.AutoMap();
            return classMap.MapMember(t => t.SomeField2);
        }

        [Test]
        public void TestMapGenericMember()
        {
            var memberMap = MapMyInterface1<MyClass>();
            Assert.IsNotNull(memberMap);
            Assert.AreEqual(typeof(MyClass), memberMap.ClassMap.ClassType);
            Assert.AreEqual("foo", memberMap.ElementName);
            Assert.AreEqual("SomeField1", memberMap.MemberName);
        }

        [Test]
        public void TestMapExplicitGenericMember()
        {
            var memberMap = MapMyInterface2<MyClass>();
            Assert.IsNotNull(memberMap);
            Assert.AreEqual(typeof(MyClass), memberMap.ClassMap.ClassType);
            Assert.AreEqual("bar", memberMap.ElementName);
            Assert.AreEqual("MongoDB.BsonUnitTests.Jira.CSharp637.CSharp637Tests.IMyInterface.SomeField2", memberMap.MemberName);
        }
    }
}