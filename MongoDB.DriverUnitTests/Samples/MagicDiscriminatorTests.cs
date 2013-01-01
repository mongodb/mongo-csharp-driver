/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Samples
{
    [TestFixture]
    public class MagicDiscriminatorTests
    {
        [BsonKnownTypes(typeof(B), typeof(C))]
        private class A
        {
            static A()
            {
                BsonSerializer.RegisterDiscriminatorConvention(typeof(A), new MagicDiscriminatorConvention());
            }

            public string InA { get; set; }
        }

        [BsonIgnoreExtraElements] // ignore _id
        private class B : A
        {
            public string OnlyInB { get; set; }
        }

        [BsonIgnoreExtraElements] // ignore _id
        private class C : A
        {
            public string OnlyInC { get; set; }
        }

        private class MagicDiscriminatorConvention : IDiscriminatorConvention
        {
            public string ElementName { get { return null; } }

            public Type GetActualType(BsonReader bsonReader, Type nominalType)
            {
                var bookmark = bsonReader.GetBookmark();
                bsonReader.ReadStartDocument();
                var actualType = nominalType;
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var name = bsonReader.ReadName();
                    if (name == "OnlyInB")
                    {
                        actualType = typeof(B);
                        break;
                    }
                    else if (name == "OnlyInC")
                    {
                        actualType = typeof(C);
                        break;
                    }
                    bsonReader.SkipValue();
                }
                bsonReader.ReturnToBookmark(bookmark);
                return actualType;
            }

            public BsonValue GetDiscriminator(Type nominalType, Type actualType)
            {
                return null;
            }
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<A> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<A>();
        }

        [Test]
        public void TestBAsA()
        {
            var b = new B { InA = "a", OnlyInB = "b" };

            var json = b.ToJson();
            var expected = "{ 'InA' : 'a', 'OnlyInB' : 'b' }".Replace("'", "\""); // note: no _t discriminator!
            Assert.AreEqual(expected, json);

            _collection.RemoveAll();
            _collection.Insert(b);
            var copy = (B)_collection.FindOne();
            Assert.IsInstanceOf<B>(copy);
            Assert.AreEqual("a", copy.InA);
            Assert.AreEqual("b", copy.OnlyInB);
        }

        [Test]
        public void TestCAsA()
        {
            var c = new C { InA = "a", OnlyInC = "c" };

            var json = c.ToJson();
            var expected = "{ 'InA' : 'a', 'OnlyInC' : 'c' }".Replace("'", "\""); // note: no _t discriminator!
            Assert.AreEqual(expected, json);

            _collection.RemoveAll();
            _collection.Insert(c);
            var copy = (C)_collection.FindOne();
            Assert.IsInstanceOf<C>(copy);
            Assert.AreEqual("a", copy.InA);
            Assert.AreEqual("c", copy.OnlyInC);
        }
    }
}
