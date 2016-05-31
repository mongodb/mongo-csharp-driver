/* Copyright 2010-2016 MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp265
{
    public class CSharp265Tests
    {
        public class GDA
        {
            public int Id;
            [BsonRepresentation(BsonType.Array)]
            public Dictionary<string, int> Data;
        }

        public class GDD
        {
            public int Id;
            [BsonRepresentation(BsonType.Document)]
            public Dictionary<string, int> Data;
        }

        public class GDX
        {
            public int Id;
            public Dictionary<string, int> Data;
        }

        public class HA
        {
            public int Id;
            [BsonRepresentation(BsonType.Array)]
            public Hashtable Data;
        }

        public class HD
        {
            public int Id;
            [BsonRepresentation(BsonType.Document)]
            public Hashtable Data;
        }

        public class HX
        {
            public int Id;
            public Hashtable Data;
        }

        private static MongoCollection<GDA> __collection;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);

        public CSharp265Tests()
        {
            var _ = __lazyOneTimeSetup.Value;
        }

        private static bool OneTimeSetup()
        {
            __collection = LegacyTestConfiguration.GetCollection<GDA>();
            __collection.Drop();
            return true;
        }

        [Fact]
        public void TestGenericDictionaryArrayRepresentationWithDollar()
        {
            var d = new GDA { Id = 1, Data = new Dictionary<string, int> { { "$a", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : [['$a', 1]] }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            __collection.Insert(d);
            var r = __collection.FindOne(Query.EQ("_id", d.Id));
            Assert.Equal(d.Id, r.Id);
            Assert.Equal(1, r.Data.Count);
            Assert.Equal(1, r.Data["$a"]);
        }

        [Fact]
        public void TestGenericDictionaryArrayRepresentationWithDot()
        {
            var d = new GDA { Id = 1, Data = new Dictionary<string, int> { { "a.b", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : [['a.b', 1]] }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            __collection.Insert(d);
            var r = __collection.FindOne(Query.EQ("_id", d.Id));
            Assert.Equal(d.Id, r.Id);
            Assert.Equal(1, r.Data.Count);
            Assert.Equal(1, r.Data["a.b"]);
        }

        [Fact]
        public void TestGenericDictionaryDocumentRepresentationWithDollar()
        {
            var d = new GDD { Id = 1, Data = new Dictionary<string, int> { { "$a", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { '$a' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            Assert.Throws<BsonSerializationException>(() => { __collection.Insert(d); });
        }

        [Fact]
        public void TestGenericDictionaryDocumentRepresentationWithDot()
        {
            var d = new GDD { Id = 1, Data = new Dictionary<string, int> { { "a.b", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { 'a.b' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            Assert.Throws<BsonSerializationException>(() => { __collection.Insert(d); });
        }

        [Fact]
        public void TestGenericDictionaryDynamicRepresentationNormal()
        {
            var d = new GDX { Id = 1, Data = new Dictionary<string, int> { { "abc", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { 'abc' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            __collection.Insert(d);
            var r = __collection.FindOne(Query.EQ("_id", d.Id));
            Assert.Equal(d.Id, r.Id);
            Assert.Equal(1, r.Data.Count);
            Assert.Equal(1, r.Data["abc"]);
        }

        [Fact]
        public void TestGenericDictionaryDynamicRepresentationWithDollar()
        {
            var d = new GDX { Id = 1, Data = new Dictionary<string, int> { { "$a", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { '$a' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            Assert.Throws<BsonSerializationException>(() => __collection.Insert(d));
        }

        [Fact]
        public void TestGenericDictionaryDynamicRepresentationWithDot()
        {
            var d = new GDX { Id = 1, Data = new Dictionary<string, int> { { "a.b", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { 'a.b' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            Assert.Throws<BsonSerializationException>(() => __collection.Insert(d));
        }

        [Fact]
        public void TestHashtableArrayRepresentationWithDollar()
        {
            var d = new HA { Id = 1, Data = new Hashtable { { "$a", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : [['$a', 1]] }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            __collection.Insert(d);
            var r = __collection.FindOne(Query.EQ("_id", d.Id));
            Assert.Equal(d.Id, r.Id);
            Assert.Equal(1, r.Data.Count);
            Assert.Equal(1, r.Data["$a"]);
        }

        [Fact]
        public void TestHashtableArrayRepresentationWithDot()
        {
            var d = new HA { Id = 1, Data = new Hashtable { { "a.b", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : [['a.b', 1]] }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            __collection.Insert(d);
            var r = __collection.FindOne(Query.EQ("_id", d.Id));
            Assert.Equal(d.Id, r.Id);
            Assert.Equal(1, r.Data.Count);
            Assert.Equal(1, r.Data["a.b"]);
        }

        [Fact]
        public void TestHashtableDocumentRepresentationWithDollar()
        {
            var d = new HD { Id = 1, Data = new Hashtable { { "$a", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { '$a' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            Assert.Throws<BsonSerializationException>(() => { __collection.Insert(d); });
        }

        [Fact]
        public void TestHashtableDocumentRepresentationWithDot()
        {
            var d = new HD { Id = 1, Data = new Hashtable { { "a.b", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { 'a.b' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            Assert.Throws<BsonSerializationException>(() => { __collection.Insert(d); });
        }

        [Fact]
        public void TestHashtableDynamicRepresentationNormal()
        {
            var d = new HX { Id = 1, Data = new Hashtable { { "abc", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { 'abc' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            __collection.Insert(d);
            var r = __collection.FindOne(Query.EQ("_id", d.Id));
            Assert.Equal(d.Id, r.Id);
            Assert.Equal(1, r.Data.Count);
            Assert.Equal(1, r.Data["abc"]);
        }

        [Fact]
        public void TestHashtableDynamicRepresentationWithDollar()
        {
            var d = new HX { Id = 1, Data = new Hashtable { { "$a", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { '$a' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            Assert.Throws<BsonSerializationException>(() => __collection.Insert(d));
        }

        [Fact]
        public void TestHashtableDynamicRepresentationWithDot()
        {
            var d = new HX { Id = 1, Data = new Hashtable { { "a.b", 1 } } };
            var expected = "{ '_id' : 1, 'Data' : { 'a.b' : 1 } }".Replace("'", "\"");
            var json = d.ToJson();
            Assert.Equal(expected, json);

            __collection.RemoveAll();
            Assert.Throws<BsonSerializationException>(() => __collection.Insert(d));
        }
    }
}
