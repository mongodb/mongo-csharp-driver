﻿/* Copyright 2010-2013 10gen Inc.
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
    public class MemberNameElementNameConventionsTests
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
            var firstName = classMap.MapMember(x => x.FirstName);
            var age = classMap.MapMember(x => x.Age);
            var _dumbName = classMap.MapMember(x => x._DumbName);
            var lowerCase = classMap.MapMember(x => x.lowerCase);

            convention.Apply(firstName);
            convention.Apply(age);
            convention.Apply(_dumbName);
            convention.Apply(lowerCase);

            Assert.AreEqual("FirstName", firstName.ElementName);
            Assert.AreEqual("Age", age.ElementName);
            Assert.AreEqual("_DumbName", _dumbName.ElementName);
            Assert.AreEqual("lowerCase", lowerCase.ElementName);
        }
    }
}
