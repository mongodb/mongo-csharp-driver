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

using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
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
#pragma warning disable 618
            Assert.AreEqual("FirstName", convention.GetElementName(typeof(TestClass).GetProperty("FirstName")));
            Assert.AreEqual("Age", convention.GetElementName(typeof(TestClass).GetProperty("Age")));
            Assert.AreEqual("_DumbName", convention.GetElementName(typeof(TestClass).GetProperty("_DumbName")));
            Assert.AreEqual("lowerCase", convention.GetElementName(typeof(TestClass).GetProperty("lowerCase")));
#pragma warning restore 618
        }

        [Test]
        public void TestCamelCaseElementNameConvention()
        {
            var convention = new CamelCaseElementNameConvention();
#pragma warning disable 618
            Assert.AreEqual("firstName", convention.GetElementName(typeof(TestClass).GetProperty("FirstName")));
            Assert.AreEqual("age", convention.GetElementName(typeof(TestClass).GetProperty("Age")));
            Assert.AreEqual("_DumbName", convention.GetElementName(typeof(TestClass).GetProperty("_DumbName")));
            Assert.AreEqual("lowerCase", convention.GetElementName(typeof(TestClass).GetProperty("lowerCase")));
#pragma warning restore 618
        }
    }
}
