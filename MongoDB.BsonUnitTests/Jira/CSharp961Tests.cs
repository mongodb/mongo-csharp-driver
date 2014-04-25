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

using System.Collections.Generic;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp961Tests
    {
        static CSharp961Tests()
        {
            BsonClassMap.RegisterClassMap<Parent>();
            BsonClassMap.RegisterClassMap<Child>();
        }

        public interface IParent
        {
            [BsonId]
            BsonObjectId Id { get; set; }

            [BsonElement("name")]
            string Name { get; set; }

            [BsonElement("children")]
            Dictionary<string, IChild> StringToChildren { get; set; }
        }

        public interface IChild
        {
            [BsonElement("name")]
            string Name { get; set; }
        }

        public class Parent : IParent
        {
            [BsonId]
            public BsonObjectId Id { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("children")]
            public Dictionary<string, IChild> StringToChildren { get; set; }
        }

        public class Child : IChild
        {
            [BsonElement("name")]
            public string Name { get; set; }
        }

        private Parent _sampleParent = new Parent
        {
            Id = new ObjectId("535a524abfcae300b04cea7b"),
            Name = "ParentName",
            StringToChildren = new Dictionary<string, IChild>
            {
                { "one", new Child { Name = "bob" } },
                { "two", new Child { Name = "jane" } }
            }
        };

        [Test]
        public void TestDeserializeWithDiscriminator()
        {
            var json = "{ '_id' : ObjectId('535a524abfcae300b04cea7b'), 'name' : 'ParentName', 'children' : { 'one' : { '_t' : 'Child', 'name' : 'bob' }, 'two' : { '_t' : 'Child', 'name' : 'jane' } } }";
            var deserializedParent = BsonSerializer.Deserialize<Parent>(json);
            Assert.IsTrue(ParentEquals(deserializedParent, _sampleParent));
        }

        [Test]
        public void TestDeserializeWithoutDiscriminator()
        {
            var json = "{ '_id' : ObjectId('535a524abfcae300b04cea7b'), 'name' : 'ParentName', 'children' : { 'one' : { 'name' : 'bob' }, 'two' : { 'name' : 'jane' } } }";
            Assert.Throws<FileFormatException>(() => { var deserializedParent = BsonSerializer.Deserialize<Parent>(json); });
        }

        [Test]
        public void TestSerialize()
        {
            var json = _sampleParent.ToJson();
            var expected = "{ '_id' : ObjectId('535a524abfcae300b04cea7b'), 'name' : 'ParentName', 'children' : { 'one' : { '_t' : 'Child', 'name' : 'bob' }, 'two' : { '_t' : 'Child', 'name' : 'jane' } } }";
            Assert.AreEqual(expected.Replace("'", "\""), json);
        }

        private static bool ParentEquals(Parent lhs, Parent rhs)
        {
            if (lhs.Id != rhs.Id) { return false; }
            if (lhs.Name != rhs.Name) { return false; }
            if (lhs.StringToChildren.Count != rhs.StringToChildren.Count) { return false; }
            foreach (var pair in lhs.StringToChildren)
            {
                var lhsChild = pair.Value;
                IChild rhsChild;
                if (!rhs.StringToChildren.TryGetValue(pair.Key, out rhsChild)) { return false; }
                if (lhsChild.Name != rhsChild.Name) { return false; }
            }
            return true;
        }
    }
}