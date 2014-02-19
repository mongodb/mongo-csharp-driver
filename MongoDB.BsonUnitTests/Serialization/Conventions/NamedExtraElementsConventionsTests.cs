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
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class NamedExtraElementsConventionsTests
    {
        private NamedExtraElementsMemberConvention _subject;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _subject = new NamedExtraElementsMemberConvention(new[] { "One", "Two" });
        }

        [Test]
        public void TestDoesNotMapExtraElementsWhenOneIsNotFound()
        {
            var classMap = new BsonClassMap<TestClass1>();

            _subject.Apply(classMap);

            Assert.IsNull(classMap.ExtraElementsMemberMap);
        }

        [Test]
        public void TestMapsExtraElementsWhenFirstNameExists()
        {
            var classMap = new BsonClassMap<TestClass2>();

            _subject.Apply(classMap);

            Assert.IsNotNull(classMap.ExtraElementsMemberMap);
        }

        [Test]
        public void TestMapsExtraElementsWhenSecondNameExists()
        {
            var classMap = new BsonClassMap<TestClass3>();

            _subject.Apply(classMap);

            Assert.IsNotNull(classMap.ExtraElementsMemberMap);
        }

        [Test]
        public void TestMapsExtraElementsWhenBothExist()
        {
            var classMap = new BsonClassMap<TestClass4>();

            _subject.Apply(classMap);

            Assert.IsNotNull(classMap.ExtraElementsMemberMap);
            Assert.AreEqual("One", classMap.ExtraElementsMemberMap.MemberName);
        }

        [Test]
        public void TestDoesNotMapExtraElementsWhenIsNotValidType()
        {
            var classMap = new BsonClassMap<TestClass5>();

            _subject.Apply(classMap);

            Assert.IsNull(classMap.ExtraElementsMemberMap);
        }

        private class TestClass1
        {
            public BsonDocument None { get; set; }
        }

        private class TestClass2
        {
            public BsonDocument One { get; set; }
        }

        private class TestClass3
        {
            public BsonDocument Two { get; set; }
        }

        private class TestClass4
        {
            public BsonDocument One { get; set; }

            public BsonDocument Two { get; set; }
        }

        private class TestClass5
        {
            public int One { get; set; }
        }
    }
}