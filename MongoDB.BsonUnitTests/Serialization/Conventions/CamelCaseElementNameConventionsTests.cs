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

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class CamelCaseElementNameConventionsTests
    {
        private class TestClass
        {
            public string FirstName { get; set; }
            public int Age { get; set; }
            public string _dumbName { get; set; }
            public string camelCasedName { get; set; }
            public string ALLCAPS { get; set; }
        }

        [Test]
        public void TestShouldDowncaseOnlyFirstCharByDefault()
        {
            var convention = new CamelCaseElementNameConvention();
            var classMap = new BsonClassMap<TestClass>();
            var firstName = classMap.MapMember(x => x.FirstName);
            var age = classMap.MapMember(x => x.Age);
            var allCaps = classMap.MapMember(x => x.ALLCAPS);

            convention.Apply(firstName);
            convention.Apply(age);
            convention.Apply(allCaps);

            Assert.AreEqual("firstName", firstName.ElementName);
            Assert.AreEqual("age", age.ElementName);
            Assert.AreEqual("aLLCAPS", allCaps.ElementName);
        }

        [Test]
        public void TestShouldHandleNameStartingWithNonLetter()
        {
            var convention = new CamelCaseElementNameConvention();
            var classMap = new BsonClassMap<TestClass>();
            var _dumbName = classMap.MapMember(x => x._dumbName);

            convention.Apply(_dumbName);

            Assert.AreEqual("_dumbName", _dumbName.ElementName);
        }

        [Test]
        public void TestShouldNotChangeIfAlreadyCamelCased()
        {
            var convention = new CamelCaseElementNameConvention();
            var classMap = new BsonClassMap<TestClass>();
            var lowerCase = classMap.MapMember(x => x.camelCasedName);

            convention.Apply(lowerCase);

            Assert.AreEqual("camelCasedName", lowerCase.ElementName);
        }

        [Test]
        [TestCase(2,"alLCAPS")]
        [TestCase(3,"allCAPS")]
        [TestCase(4,"allcAPS")]
        public void TestShouldDowncaseCertainNumberOfChars(int charsToDowncase, string expected)
        {
            var convention = new CamelCaseElementNameConvention(charsToDowncase);
            var classMap = new BsonClassMap<TestClass>();
            var ioStream = classMap.MapMember(x => x.ALLCAPS);

            convention.Apply(ioStream);

            Assert.AreEqual(expected, ioStream.ElementName);
        }
    }
}
