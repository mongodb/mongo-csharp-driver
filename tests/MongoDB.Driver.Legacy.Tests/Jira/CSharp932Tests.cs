/* Copyright 2010-2015 MongoDB Inc.
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
using System.Linq;using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp932Tests
    {
        private class C
        {
            public int Id { get; set; }
            public BsonArray Array { get; set; }
            public BsonBinaryData Binary { get; set; }
            public BsonBoolean Boolean { get; set; }
            public BsonDateTime DateTime { get; set; }
            public BsonDocument Document { get; set; }
            public BsonDouble Double { get; set; }
            public BsonInt32 Int32 { get; set; }
            public BsonInt64 Int64 { get; set; }
            public BsonJavaScript JavaScript { get; set; }
            public BsonJavaScriptWithScope JavaScriptWithScope { get; set; }
            public BsonMaxKey MaxKey { get; set; }
            public BsonMinKey MinKey { get; set; }
            public BsonNull Null { get; set; }
            public BsonObjectId ObjectId { get; set; }
            public BsonRegularExpression RegularExpression { get; set; }
            public BsonString String { get; set; }
            public BsonSymbol Symbol { get; set; }
            public BsonTimestamp Timestamp { get; set; }
            public BsonUndefined Undefined { get; set; }
            public BsonValue Value { get; set; }
        }

        [Fact]
        public void TestBsonArrayEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Array == null);
            var json = query.ToJson();
            var expected = "{ 'Array' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonArrayEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Array == new BsonArray { 1, 2, 3 });
            var json = query.ToJson();
            var expected = "{ 'Array' : [1, 2, 3] }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonBinaryDataEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Binary == null);
            var json = query.ToJson();
            var expected = "{ 'Binary' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonBinaryDataEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Binary == new BsonBinaryData(new byte[0]));
            var json = query.ToJson();
            var expected = "{ 'Binary' : new BinData(0, '') }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonBooleanEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Boolean == null);
            var json = query.ToJson();
            var expected = "{ 'Boolean' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonBooleanEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Boolean == true);
            var json = query.ToJson();
            var expected = "{ 'Boolean' : true }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonDateTimeEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.DateTime == null);
            var json = query.ToJson();
            var expected = "{ 'DateTime' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonDateTimeEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.DateTime == new DateTime(2014, 4, 22, 1, 2, 3, DateTimeKind.Utc));
            var json = query.ToJson();
            var expected = "{ 'DateTime' : ISODate('2014-04-22T01:02:03Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonDocumentEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Document == null);
            var json = query.ToJson();
            var expected = "{ 'Document' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonDocumentEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Document == new BsonDocument("x", 1));
            var json = query.ToJson();
            var expected = "{ 'Document' : { 'x' : 1 } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonDoubleEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Double == null);
            var json = query.ToJson();
            var expected = "{ 'Double' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonDoubleEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Double == 1.5);
            var json = query.ToJson();
            var expected = "{ 'Double' : 1.5 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonInt32EqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Int32 == null);
            var json = query.ToJson();
            var expected = "{ 'Int32' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonInt32EqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Int32 == 123);
            var json = query.ToJson();
            var expected = "{ 'Int32' : 123 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonInt64EqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Int64 == null);
            var json = query.ToJson();
            var expected = "{ 'Int64' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonInt64EqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Int64 == 123);
            var json = query.ToJson();
            var expected = "{ 'Int64' : NumberLong(123) }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonJavaScriptEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.JavaScript == null);
            var json = query.ToJson();
            var expected = "{ 'JavaScript' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonJavaScriptEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.JavaScript == new BsonJavaScript("code"));
            var json = query.ToJson();
            var expected = "{ 'JavaScript' : { '$code' : 'code' } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonJavaScriptWithScopeEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.JavaScriptWithScope == null);
            var json = query.ToJson();
            var expected = "{ 'JavaScriptWithScope' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonJavaScriptWithScopeEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.JavaScriptWithScope == new BsonJavaScriptWithScope("code", new BsonDocument("x", 1)));
            var json = query.ToJson();
            var expected = "{ 'JavaScriptWithScope' : { '$code' : 'code', '$scope' : { 'x' : 1 } } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonMaxKeyEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.MaxKey == null);
            var json = query.ToJson();
            var expected = "{ 'MaxKey' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonMaxKeyEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.MaxKey == BsonMaxKey.Value);
            var json = query.ToJson();
            var expected = "{ 'MaxKey' : MaxKey }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonMinKeyEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.MinKey == null);
            var json = query.ToJson();
            var expected = "{ 'MinKey' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonMinKeyEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.MinKey == BsonMinKey.Value);
            var json = query.ToJson();
            var expected = "{ 'MinKey' : MinKey }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonNullEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Null == null);
            var json = query.ToJson();
            var expected = "{ 'Null' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonNullEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Null == BsonNull.Value);
            var json = query.ToJson();
            var expected = "{ 'Null' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonObjectIdEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.ObjectId == null);
            var json = query.ToJson();
            var expected = "{ 'ObjectId' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonObjectIdEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.ObjectId == new ObjectId("0102030405060708090a0b0c"));
            var json = query.ToJson();
            var expected = "{ 'ObjectId' : ObjectId('0102030405060708090a0b0c') }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonRegularExpressionEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.RegularExpression == null);
            var json = query.ToJson();
            var expected = "{ 'RegularExpression' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonRegularExpressionEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.RegularExpression == new BsonRegularExpression("pattern"));
            var json = query.ToJson();
            var expected = "{ 'RegularExpression' : /pattern/ }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonStringEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.String == null);
            var json = query.ToJson();
            var expected = "{ 'String' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonStringEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.String == "abc");
            var json = query.ToJson();
            var expected = "{ 'String' : 'abc' }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonSymbolEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Symbol == null);
            var json = query.ToJson();
            var expected = "{ 'Symbol' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonSymbolEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Symbol == BsonSymbolTable.Lookup("abc"));
            var json = query.ToJson();
            var expected = "{ 'Symbol' : { '$symbol' : 'abc' } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonTimestampEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Timestamp == null);
            var json = query.ToJson();
            var expected = "{ 'Timestamp' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonTimestampEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Timestamp == new BsonTimestamp(1, 2));
            var json = query.ToJson();
            var expected = "{ 'Timestamp' : Timestamp(1, 2) }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonUndefinedEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Undefined == null);
            var json = query.ToJson();
            var expected = "{ 'Undefined' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonUndefinedEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Undefined == BsonUndefined.Value);
            var json = query.ToJson();
            var expected = "{ 'Undefined' : undefined }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonValueEqualsCSharpNull()
        {
            var query = Query<C>.Where(c => c.Value == null);
            var json = query.ToJson();
            var expected = "{ 'Value' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBsonValueEqualsNotNull()
        {
            var query = Query<C>.Where(c => c.Value == 1);
            var json = query.ToJson();
            var expected = "{ 'Value' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestLinqArrayQuery()
        {
            var collection = LegacyTestConfiguration.GetCollection<C>();
            collection.Drop();
            collection.Insert(new C { Id = 1, Array = null });
            collection.Insert(new C { Id = 2, Array = new BsonArray { 2 } });

            var results = collection.AsQueryable<C>().Where(c => c.Array == null).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(1, results[0].Id);

            results = collection.AsQueryable<C>().Where(c => c.Array[0] == 2).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(2, results[0].Id);
        }

        [Fact]
        public void TestLinqDocumentQuery()
        {
            var collection = LegacyTestConfiguration.GetCollection<C>();
            collection.Drop();
            collection.Insert(new C { Id = 1, Document = null });
            collection.Insert(new C { Id = 2, Document = new BsonDocument("x", 2) });

            var results = collection.AsQueryable<C>().Where(c => c.Document == null).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(1, results[0].Id);

            results = collection.AsQueryable<C>().Where(c => c.Document["x"] == 2).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(2, results[0].Id);
        }

        [Fact]
        public void TestLinqObjectIdQuery()
        {
            var collection = LegacyTestConfiguration.GetCollection<C>();
            collection.Drop();
            var id = ObjectId.GenerateNewId();
            collection.Insert(new C { Id = 1, ObjectId = null });
            collection.Insert(new C { Id = 2, ObjectId = id });

            var results = collection.AsQueryable<C>().Where(c => c.ObjectId == null).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(1, results[0].Id);

            results = collection.AsQueryable<C>().Where(c => c.ObjectId == id).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(2, results[0].Id);
        }

        [Fact]
        public void TestLinqValueQuery()
        {
            var collection = LegacyTestConfiguration.GetCollection<C>();
            collection.Drop();
            collection.Insert(new C { Id = 1, Value = null });
            collection.Insert(new C { Id = 2, Value = 2 });

            var results = collection.AsQueryable<C>().Where(c => c.Value == null).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(1, results[0].Id);

            results = collection.AsQueryable<C>().Where(c => c.Value == 2).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(2, results[0].Id);
        }
    }
}