﻿/* Copyright 2010-2014 MongoDB Inc.
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

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    [TestFixture]
    public class ElementNameConventionsTests
    {
        private class TestClass
        {
            public string FirstName { get; set; }
            public int Age { get; set; }
            public string _DumbName { get; set; }
            public string lowerCase { get; set; }
        }

        [Test]
        public void TestMemberNameElementNameConvention()
        {
            var convention = new MemberNameElementNameConvention();
            var classMap = new BsonClassMap<TestClass>();
            convention.Apply(classMap.MapMember(x => x.FirstName));
            convention.Apply(classMap.MapMember(x => x.Age));
            convention.Apply(classMap.MapMember(x => x._DumbName));
            convention.Apply(classMap.MapMember(x => x.lowerCase));
            Assert.AreEqual("FirstName", classMap.GetMemberMap(x => x.FirstName).ElementName);
            Assert.AreEqual("Age", classMap.GetMemberMap(x => x.Age).ElementName);
            Assert.AreEqual("_DumbName", classMap.GetMemberMap(x => x._DumbName).ElementName);
            Assert.AreEqual("lowerCase", classMap.GetMemberMap(x => x.lowerCase).ElementName);
        }

        [Test]
        public void TestCamelCaseElementNameConvention()
        {
            var convention = new CamelCaseElementNameConvention();
            var classMap = new BsonClassMap<TestClass>();
            convention.Apply(classMap.MapMember(x => x.FirstName));
            convention.Apply(classMap.MapMember(x => x.Age));
            convention.Apply(classMap.MapMember(x => x._DumbName));
            convention.Apply(classMap.MapMember(x => x.lowerCase));
            Assert.AreEqual("firstName", classMap.GetMemberMap(x => x.FirstName).ElementName);
            Assert.AreEqual("age", classMap.GetMemberMap(x => x.Age).ElementName);
            Assert.AreEqual("_DumbName", classMap.GetMemberMap(x => x._DumbName).ElementName);
            Assert.AreEqual("lowerCase", classMap.GetMemberMap(x => x.lowerCase).ElementName);
        }
    }
}
