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

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class BsonDocumentBackedClassSerializerTests
    {
        [Test]
        public void TestDynamicMemberName()
        {
            var query = Query<TestDocument>.Where(x => x["Dynamic-Awesome"] == true);
            var expected = "{ \"Dynamic-Awesome\" : true }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIndexerWithKnownMemberName()
        {
            var query = Query<TestDocument>.Where(x => x["Name"] == "awesome");
            var expected = "{ \"name\" : \"awesome\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIndexerWithEnum()
        {
            var query = Query<TestDocument>.Where(x => x[KnownPropertyNames.Name] == "awesome");
            var expected = "{ \"name\" : \"awesome\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIndexerWithObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            var query = Query<TestDocument>.Where(x => x[objectId] == "awesome");
            var expected = string.Format("{{ \"{0}\" : \"awesome\" }}", objectId);
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIndexerOnList()
        {
            var query = Query<TestDocument>.Where(x => x.Colors[0] == 12);
            var expected = "{ \"colors.0\" : \"12\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestElementAtOnList()
        {
            var query = Query<TestDocument>.Where(x => x.Colors.ElementAt(0) == 12);
            var expected = "{ \"colors.0\" : \"12\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIndexerOnArray()
        {
            var query = Query<TestDocument>.Where(x => x.Colors2[0] == 12);
            var expected = "{ \"colors2.0\" : \"12\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestThrowsExceptionWhenMemberDoesNotExistAndIsNotDynamic()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Query<TestDocument>.Where(x => x["ThrowMe"] == 42));
        }

        public enum KnownPropertyNames
        {
            Name
        }

        [Serializable]
        [BsonSerializer(typeof(TestDocumentClassSerializer))]
        public class TestDocument : BsonDocumentBackedClass
        {
            public TestDocument(BsonDocument backingDocument, IBsonDocumentSerializer serializer)
                : base(backingDocument, serializer)
            { }

            public BsonValue this[string fieldname]
            {
                get { return this.BackingDocument[fieldname]; }
                set { this.BackingDocument[fieldname] = value; }
            }

            public BsonValue this[KnownPropertyNames name]
            {
                get
                {
                    switch(name)
                    {
                        case KnownPropertyNames.Name:
                            return this["Name"];
                    }

                    throw new NotSupportedException();
                }
            }

            public BsonValue this[ObjectId objectId]
            {
                get { return this.BackingDocument[objectId.ToString()]; }
            }

            [BsonId]
            public ObjectId Id
            {
                get { return this.BackingDocument["_id"].AsObjectId; }
                set { this.BackingDocument["_id"] = value; }
            }

            public List<int> Colors
            {
                get { return this.BackingDocument["colors"].AsBsonArray.Select(x => x.AsInt32).ToList(); }
                set { this.BackingDocument["colors"] = new BsonArray(value); }
            }

            public int[] Colors2
            {
                get { return this.BackingDocument["colors"].AsBsonArray.Select(x => x.AsInt32).ToArray(); }
                set { this.BackingDocument["colors"] = new BsonArray(value); }
            }
        }

        public class TestDocumentClassSerializer : BsonDocumentBackedClassSerializer<TestDocument>
        {
            public TestDocumentClassSerializer()
            {
                this.RegisterMember("Id", "_id", new ObjectIdSerializer(), typeof(ObjectId), null);
                this.RegisterMember("Name", "name", new StringSerializer(), typeof(string), null);
                this.RegisterMember("Colors", "colors", new EnumerableSerializer<int>(), typeof(IEnumerable<int>), new ArraySerializationOptions(new RepresentationSerializationOptions(BsonType.String)));
                this.RegisterMember("Colors2", "colors2", new EnumerableSerializer<int>(), typeof(IEnumerable<int>), new ArraySerializationOptions(new RepresentationSerializationOptions(BsonType.String)));
            }

            public override BsonSerializationInfo GetMemberSerializationInfo(string memberName)
            {
                // Dynamic members are allowed in a TestDocument if 
                // they start with Dynamic- or are an ObjectId.
                // Otherwise, we fall back to the base-class' definition.

                ObjectId objectId;
                if (memberName.StartsWith("Dynamic-"))
                {
                    return new BsonSerializationInfo(
                        memberName,
                        BsonValueSerializer.Instance,
                        typeof(BsonValue),
                        BsonValueSerializer.Instance.GetDefaultSerializationOptions());
                }
                else if (ObjectId.TryParse(memberName, out objectId))
                {
                    return new BsonSerializationInfo(
                        memberName,
                        BsonValueSerializer.Instance,
                        typeof(BsonValue),
                        BsonValueSerializer.Instance.GetDefaultSerializationOptions());
                }

                return base.GetMemberSerializationInfo(memberName);
            }

            protected override TestDocument CreateInstance(BsonDocument backingDocument)
            {
                return new TestDocument(backingDocument, this);
            }
        }
    }
}