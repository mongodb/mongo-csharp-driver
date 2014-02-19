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

using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class ReadWriteMemberFinderConventionsTests
    {
        private ReadWriteMemberFinderConvention _subject;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _subject = new ReadWriteMemberFinderConvention();
        }

        [Test]
        public void TestMapsAllTheReadAndWriteFieldsAndProperties()
        {
            var classMap = new BsonClassMap<TestClass>();

            _subject.Apply(classMap);

            Assert.AreEqual(3, classMap.DeclaredMemberMaps.Count());

            Assert.IsNotNull(classMap.GetMemberMap(x => x.Mapped1));
            Assert.IsNotNull(classMap.GetMemberMap(x => x.Mapped2));
            Assert.IsNotNull(classMap.GetMemberMap(x => x.Mapped3));

            Assert.IsNull(classMap.GetMemberMap(x => x.NotMapped1));
            Assert.IsNull(classMap.GetMemberMap(x => x.NotMapped2));
        }

        private class TestClass
        {
            public string Mapped1 { get; set; }

            public string Mapped2 = "blah";

            // yes, we'll map this because we know how to set it and part of it is public...
            public string Mapped3 { get; private set; }

            public readonly string NotMapped1 = "blah";

            public string NotMapped2
            {
                get { return "blah"; }
            }
        }
    }
}