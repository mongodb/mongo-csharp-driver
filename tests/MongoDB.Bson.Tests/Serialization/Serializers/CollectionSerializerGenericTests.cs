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
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.CollectionSerializersGeneric
{
    [BsonDiscriminator("CollectionSerializersGeneric.C")] // "C" is an ambiguous discriminator when nominalType is System.Object
    public class C
    {
        public string P { get; set; }
    }

    public class EnumerableSerializerTests
    {
        public class T
        {
            public List<object> L { get; set; }
            public ICollection<object> IC { get; set; }
            public IEnumerable<object> IE { get; set; }
            public IList<object> IL { get; set; }
            public Queue<object> Q { get; set; }
            public Stack<object> S { get; set; }
            public HashSet<object> H { get; set; }
            public LinkedList<object> LL { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new T { L = null, IC = null, IE = null, IL = null, Q = null, S = null, H = null, LL = null };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.Null(rehydrated.L);
            Assert.Null(rehydrated.Q);
            Assert.Null(rehydrated.S);
            Assert.Null(rehydrated.IC);
            Assert.Null(rehydrated.IE);
            Assert.Null(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var list = new List<object>();
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<List<object>>(rehydrated.IC);
            Assert.IsType<List<object>>(rehydrated.IE);
            Assert.IsType<List<object>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneC()
        {
            var list = new List<object>(new[] { new C { P = "x" } });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[{ '_t' : 'CollectionSerializersGeneric.C', 'P' : 'x' }]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<List<object>>(rehydrated.IC);
            Assert.IsType<List<object>>(rehydrated.IE);
            Assert.IsType<List<object>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneInt()
        {
            var list = new List<object>(new object[] { 1 });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[1]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<List<object>>(rehydrated.IC);
            Assert.IsType<List<object>>(rehydrated.IE);
            Assert.IsType<List<object>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneString()
        {
            var list = new List<object>(new[] { "x" });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "['x']";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<List<object>>(rehydrated.IC);
            Assert.IsType<List<object>>(rehydrated.IE);
            Assert.IsType<List<object>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoCs()
        {
            var list = new List<object>(new[] { new C { P = "x" }, new C { P = "y" } });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[{ '_t' : 'CollectionSerializersGeneric.C', 'P' : 'x' }, { '_t' : 'CollectionSerializersGeneric.C', 'P' : 'y' }]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<List<object>>(rehydrated.IC);
            Assert.IsType<List<object>>(rehydrated.IE);
            Assert.IsType<List<object>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoInts()
        {
            var list = new List<object>(new object[] { 1, 2 });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[1, 2]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<List<object>>(rehydrated.IC);
            Assert.IsType<List<object>>(rehydrated.IE);
            Assert.IsType<List<object>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoStrings()
        {
            var list = new List<object>(new[] { "x", "y" });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "['x', 'y']";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<List<object>>(rehydrated.IC);
            Assert.IsType<List<object>>(rehydrated.IE);
            Assert.IsType<List<object>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMixedPrimitiveTypes()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = string.Format("ISODate(\"{0}\")", dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var list = new List<object>(new object[] { true, dateTime, 1.5, 1, 2L, guid, objectId, "x" });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[true, #Date, 1.5, 1, NumberLong(2), #Guid, #ObjectId, 'x']";
            rep = rep.Replace("#Date", isoDate);
            rep = rep.Replace("#Guid", "CSUUID('00000000-0000-0000-0000-000000000000')");
            rep = rep.Replace("#ObjectId", "ObjectId('000000000000000000000000')");
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<List<object>>(rehydrated.IC);
            Assert.IsType<List<object>>(rehydrated.IE);
            Assert.IsType<List<object>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class EnumerableSerializerNominalTypeObjectTests
    {
        public class T
        {
            public object L { get; set; }
            public object Q { get; set; }
            public object S { get; set; }
            public object H { get; set; }
            public object LL { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new T { L = null, Q = null, S = null, H = null, LL = null };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'L' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.Null(rehydrated.L);
            Assert.Null(rehydrated.Q);
            Assert.Null(rehydrated.S);
            Assert.Null(rehydrated.H);
            Assert.Null(rehydrated.LL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var list = new List<object>();
            var obj = new T { L = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson(configurator: c => c.IsDynamicType = t => false);
            var rep = "[]";
            var expected = "{ 'L' : { '_t' : 'System.Collections.Generic.List`1[System.Object]', '_v' : #R }, 'Q' : { '_t' : 'System.Collections.Generic.Queue`1[System.Object]', '_v' : #R }, 'S' : { '_t' : 'System.Collections.Generic.Stack`1[System.Object]', '_v' : #R }, 'H' : { '_t' : 'System.Collections.Generic.HashSet`1[System.Object]', '_v' : #R }, 'LL' : { '_t' : 'System.Collections.Generic.LinkedList`1[System.Object]', '_v' : #R } }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson(configurator: c => c.IsDynamicType = t => false);
            var rehydrated = BsonSerializer.Deserialize<T>(bson, configurator: c => 
            {
                c.DynamicArraySerializer = null;
                c.DynamicDocumentSerializer = null;
            });
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<HashSet<object>>(rehydrated.H);
            Assert.IsType<LinkedList<object>>(rehydrated.LL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson(configurator: c => c.IsDynamicType = t => false)));
        }

        [Fact]
        public void TestOneInt()
        {
            var list = new List<object>(new object[] { 1 });
            var obj = new T { L = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson(configurator: c => c.IsDynamicType = t => false);
            var rep = "[1]";
            var expected = "{ 'L' : { '_t' : 'System.Collections.Generic.List`1[System.Object]', '_v' : #R }, 'Q' : { '_t' : 'System.Collections.Generic.Queue`1[System.Object]', '_v' : #R }, 'S' : { '_t' : 'System.Collections.Generic.Stack`1[System.Object]', '_v' : #R }, 'H' : { '_t' : 'System.Collections.Generic.HashSet`1[System.Object]', '_v' : #R }, 'LL' : { '_t' : 'System.Collections.Generic.LinkedList`1[System.Object]', '_v' : #R } }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson(configurator: c => c.IsDynamicType = t => false);
            var rehydrated = BsonSerializer.Deserialize<T>(bson, configurator: c =>
            {
                c.DynamicArraySerializer = null;
                c.DynamicDocumentSerializer = null;
            });
            Assert.IsType<List<object>>(rehydrated.L);
            Assert.IsType<Queue<object>>(rehydrated.Q);
            Assert.IsType<Stack<object>>(rehydrated.S);
            Assert.IsType<HashSet<object>>(rehydrated.H);
            Assert.IsType<LinkedList<object>>(rehydrated.LL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson(configurator: c => c.IsDynamicType = t => false)));
        }
    }

    public class EnumerableSerializerWithItemSerializationOptionsTests
    {
        public enum E
        {
            None,
            A,
            B
        }

        public class T
        {
            [BsonRepresentation(BsonType.String)]
            public List<E> L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public ICollection<E> IC { get; set; }
            [BsonRepresentation(BsonType.String)]
            public IEnumerable<E> IE { get; set; }
            [BsonRepresentation(BsonType.String)]
            public IList<E> IL { get; set; }
            [BsonRepresentation(BsonType.String)]
            public Queue<E> Q { get; set; }
            [BsonRepresentation(BsonType.String)]
            public Stack<E> S { get; set; }
            [BsonRepresentation(BsonType.String)]
            public HashSet<E> H { get; set; }
            [BsonRepresentation(BsonType.String)]
            public LinkedList<E> LL { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new T { L = null, IC = null, IE = null, IL = null, Q = null, S = null, H = null, LL = null };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.Null(rehydrated.L);
            Assert.Null(rehydrated.Q);
            Assert.Null(rehydrated.S);
            Assert.Null(rehydrated.IC);
            Assert.Null(rehydrated.IE);
            Assert.Null(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var list = new List<E>();
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<E>(list), S = new Stack<E>(list), H = new HashSet<E>(list), LL = new LinkedList<E>(list) };
            var json = obj.ToJson();
            var rep = "[]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<E>>(rehydrated.L);
            Assert.IsType<Queue<E>>(rehydrated.Q);
            Assert.IsType<Stack<E>>(rehydrated.S);
            Assert.IsType<List<E>>(rehydrated.IC);
            Assert.IsType<List<E>>(rehydrated.IE);
            Assert.IsType<List<E>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneE()
        {
            var list = new List<E>(new[] { E.A });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<E>(list), S = new Stack<E>(list), H = new HashSet<E>(list), LL = new LinkedList<E>(list) };
            var json = obj.ToJson();
            var rep = "['A']";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<E>>(rehydrated.L);
            Assert.IsType<Queue<E>>(rehydrated.Q);
            Assert.IsType<Stack<E>>(rehydrated.S);
            Assert.IsType<List<E>>(rehydrated.IC);
            Assert.IsType<List<E>>(rehydrated.IE);
            Assert.IsType<List<E>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoEs()
        {
            var list = new List<E>(new[] { E.A, E.B });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<E>(list), S = new Stack<E>(list), H = new HashSet<E>(list), LL = new LinkedList<E>(list) };
            var json = obj.ToJson();
            var rep = "['A', 'B']";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<E>>(rehydrated.L);
            Assert.IsType<Queue<E>>(rehydrated.Q);
            Assert.IsType<Stack<E>>(rehydrated.S);
            Assert.IsType<List<E>>(rehydrated.IC);
            Assert.IsType<List<E>>(rehydrated.IE);
            Assert.IsType<List<E>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class EnumerableSerializerWithStringToObjectIdTests
    {
        public class T
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public List<string> L { get; set; }
            [BsonRepresentation(BsonType.ObjectId)]
            public ICollection<string> IC { get; set; }
            [BsonRepresentation(BsonType.ObjectId)]
            public IEnumerable<string> IE { get; set; }
            [BsonRepresentation(BsonType.ObjectId)]
            public IList<string> IL { get; set; }
            [BsonRepresentation(BsonType.ObjectId)]
            public Queue<string> Q { get; set; }
            [BsonRepresentation(BsonType.ObjectId)]
            public Stack<string> S { get; set; }
            [BsonRepresentation(BsonType.ObjectId)]
            public HashSet<string> H { get; set; }
            [BsonRepresentation(BsonType.ObjectId)]
            public LinkedList<string> LL { get; set; }
        }

        private static string id1 = "123456789012345678901234";
        private static string id2 = "432109876543210987654321";

        [Fact]
        public void TestNull()
        {
            var obj = new T { L = null, IC = null, IE = null, IL = null, Q = null, S = null, H = null, LL = null };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.Null(rehydrated.L);
            Assert.Null(rehydrated.Q);
            Assert.Null(rehydrated.S);
            Assert.Null(rehydrated.IC);
            Assert.Null(rehydrated.IE);
            Assert.Null(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var list = new List<string>();
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<string>(list), S = new Stack<string>(list), H = new HashSet<string>(list), LL = new LinkedList<string>(list) };
            var json = obj.ToJson();
            var rep = "[]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<string>>(rehydrated.L);
            Assert.IsType<Queue<string>>(rehydrated.Q);
            Assert.IsType<Stack<string>>(rehydrated.S);
            Assert.IsType<List<string>>(rehydrated.IC);
            Assert.IsType<List<string>>(rehydrated.IE);
            Assert.IsType<List<string>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneString()
        {
            var list = new List<string>(new[] { id1 });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<string>(list), S = new Stack<string>(list), H = new HashSet<string>(list), LL = new LinkedList<string>(list) };
            var json = obj.ToJson();
            var rep = "[ObjectId(\"123456789012345678901234\")]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<string>>(rehydrated.L);
            Assert.IsType<Queue<string>>(rehydrated.Q);
            Assert.IsType<Stack<string>>(rehydrated.S);
            Assert.IsType<List<string>>(rehydrated.IC);
            Assert.IsType<List<string>>(rehydrated.IE);
            Assert.IsType<List<string>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoStrings()
        {
            var list = new List<string>(new[] { id1, id2 });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<string>(list), S = new Stack<string>(list), H = new HashSet<string>(list), LL = new LinkedList<string>(list) };
            var json = obj.ToJson();
            var rep = "[ObjectId(\"123456789012345678901234\"), ObjectId(\"432109876543210987654321\")]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<List<string>>(rehydrated.L);
            Assert.IsType<Queue<string>>(rehydrated.Q);
            Assert.IsType<Stack<string>>(rehydrated.S);
            Assert.IsType<List<string>>(rehydrated.IC);
            Assert.IsType<List<string>>(rehydrated.IE);
            Assert.IsType<List<string>>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
