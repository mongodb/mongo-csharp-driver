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
using MongoDB.Bson.Serialization.IdGenerators;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class StringObjectIdIdGeneratorConventionsTests
    {
        private StringObjectIdIdGeneratorConvention _subject;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _subject = new StringObjectIdIdGeneratorConvention();
        }

        [Test]
        public void TestDoesNotSetWhenNoIdExists()
        {
            var classMap = new BsonClassMap<TestClass>(cm =>
            { });

            Assert.DoesNotThrow(() => _subject.PostProcess(classMap));
        }

        [Test]
        public void TestDoesNotSetWhenTypeIsntString()
        {
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.MapIdMember(x => x.OId);
            });

            _subject.PostProcess(classMap);
            Assert.IsNull(classMap.IdMemberMap.IdGenerator);
        }

        [Test]
        public void TestDoesNotSetWhenStringIsNotRepresentedAsObjectId()
        {
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.MapIdMember(x => x.String);
            });

            _subject.PostProcess(classMap);
            Assert.IsNull(classMap.IdMemberMap.IdGenerator);
        }

        [Test]
        public void TestSetsWhenIdIsStringAndRepresentedAsAnObjectId()
        {
            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.MapIdMember(x => x.String).SetRepresentation(BsonType.ObjectId);
            });

            _subject.PostProcess(classMap);
            Assert.IsNotNull(classMap.IdMemberMap.IdGenerator);
            Assert.IsInstanceOf<StringObjectIdGenerator>(classMap.IdMemberMap.IdGenerator);
        }

        private class TestClass
        {
            public ObjectId OId { get; set; }

            public string String { get; set; }
        }
    }
}