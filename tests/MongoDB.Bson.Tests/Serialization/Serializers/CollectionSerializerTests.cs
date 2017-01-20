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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.CollectionSerializers
{
    [BsonDiscriminator("CollectionSerializers.C")] // "C" is an ambiguous discriminator when nominalType is System.Object
    public class C
    {
        public string P { get; set; }
    }

    public class CollectionSerializerTests
    {
        public class T
        {
            public ArrayList L { get; set; }
            public ICollection IC { get; set; }
            public IEnumerable IE { get; set; }
            public IList IL { get; set; }
            public Queue Q { get; set; }
            public Stack S { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new T { L = null, IC = null, IE = null, IL = null, Q = null, S = null };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
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
            var list = new ArrayList();
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue(list), S = new Stack(list) };
            var json = obj.ToJson();
            var rep = "[]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.IsType<ArrayList>(rehydrated.IC);
            Assert.IsType<ArrayList>(rehydrated.IE);
            Assert.IsType<ArrayList>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneC()
        {
            var list = new ArrayList(new[] { new C { P = "x" } });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue(list), S = new Stack(list) };
            var json = obj.ToJson();
            var rep = "[{ '_t' : 'CollectionSerializers.C', 'P' : 'x' }]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.IsType<ArrayList>(rehydrated.IC);
            Assert.IsType<ArrayList>(rehydrated.IE);
            Assert.IsType<ArrayList>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneInt()
        {
            var list = new ArrayList(new[] { 1 });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue(list), S = new Stack(list) };
            var json = obj.ToJson();
            var rep = "[1]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.IsType<ArrayList>(rehydrated.IC);
            Assert.IsType<ArrayList>(rehydrated.IE);
            Assert.IsType<ArrayList>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneString()
        {
            var list = new ArrayList(new[] { "x" });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue(list), S = new Stack(list) };
            var json = obj.ToJson();
            var rep = "['x']";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.IsType<ArrayList>(rehydrated.IC);
            Assert.IsType<ArrayList>(rehydrated.IE);
            Assert.IsType<ArrayList>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoCs()
        {
            var list = new ArrayList(new[] { new C { P = "x" }, new C { P = "y" } });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue(list), S = new Stack(list) };
            var json = obj.ToJson();
            var rep = "[{ '_t' : 'CollectionSerializers.C', 'P' : 'x' }, { '_t' : 'CollectionSerializers.C', 'P' : 'y' }]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.IsType<ArrayList>(rehydrated.IC);
            Assert.IsType<ArrayList>(rehydrated.IE);
            Assert.IsType<ArrayList>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoInts()
        {
            var list = new ArrayList(new[] { 1, 2 });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue(list), S = new Stack(list) };
            var json = obj.ToJson();
            var rep = "[1, 2]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.IsType<ArrayList>(rehydrated.IC);
            Assert.IsType<ArrayList>(rehydrated.IE);
            Assert.IsType<ArrayList>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoStrings()
        {
            var list = new ArrayList(new[] { "x", "y" });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue(list), S = new Stack(list) };
            var json = obj.ToJson();
            var rep = "['x', 'y']";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.IsType<ArrayList>(rehydrated.IC);
            Assert.IsType<ArrayList>(rehydrated.IE);
            Assert.IsType<ArrayList>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMixedPrimitiveTypes()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = string.Format("ISODate(\"{0}\")", dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var list = new ArrayList(new object[] { true, dateTime, 1.5, 1, 2L, guid, objectId, "x" });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue(list), S = new Stack(list) };
            var json = obj.ToJson();
            var rep = "[true, #Date, 1.5, 1, NumberLong(2), #Guid, #ObjectId, 'x']";
            rep = rep.Replace("#Date", isoDate);
            rep = rep.Replace("#Guid", "CSUUID('00000000-0000-0000-0000-000000000000')");
            rep = rep.Replace("#ObjectId", "ObjectId('000000000000000000000000')");
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.IsType<ArrayList>(rehydrated.IC);
            Assert.IsType<ArrayList>(rehydrated.IE);
            Assert.IsType<ArrayList>(rehydrated.IL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class CollectionSerializerNominalTypeObjectTests
    {
        public class T
        {
            public object L { get; set; }
            public object Q { get; set; }
            public object S { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new T { L = null, Q = null, S = null };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'L' : #R, 'Q' : #R, 'S' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.Null(rehydrated.L);
            Assert.Null(rehydrated.Q);
            Assert.Null(rehydrated.S);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var obj = new T { L = new ArrayList(), Q = new Queue(), S = new Stack() };
            var json = obj.ToJson();
            var rep = "[]";
#if NET45
            var arrayListDiscriminator = "System.Collections.ArrayList";
            var queueDiscriminator = "System.Collections.Queue";
            var stackDiscriminator = "System.Collections.Stack";
#else
            var arrayListDiscriminator = typeof(ArrayList).AssemblyQualifiedName;
            var queueDiscriminator = typeof(Queue).AssemblyQualifiedName;
            var stackDiscriminator = typeof(Stack).AssemblyQualifiedName;
#endif
            var expected = ("{ 'L' : { '_t' : '" + arrayListDiscriminator + "', '_v' : #R }, 'Q' : { '_t' : '" + queueDiscriminator + "', '_v' : #R }, 'S' : { '_t' : '" + stackDiscriminator + "', '_v' : #R } }").Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneInt()
        {
            var list = new ArrayList(new[] { 1 });
            var obj = new T { L = list, Q = new Queue(list), S = new Stack(list) };
            var json = obj.ToJson();
            var rep = "[1]";
#if NET45
            var arrayListDiscriminator = "System.Collections.ArrayList";
            var queueDiscriminator = "System.Collections.Queue";
            var stackDiscriminator = "System.Collections.Stack";
#else
            var arrayListDiscriminator = typeof(ArrayList).AssemblyQualifiedName;
            var queueDiscriminator = typeof(Queue).AssemblyQualifiedName;
            var stackDiscriminator = typeof(Stack).AssemblyQualifiedName;
#endif
            var expected = ("{ 'L' : { '_t' : '" + arrayListDiscriminator + "', '_v' : #R }, 'Q' : { '_t' : '" + queueDiscriminator + "', '_v' : #R }, 'S' : { '_t' : '" + stackDiscriminator + "', '_v' : #R } }").Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<ArrayList>(rehydrated.L);
            Assert.IsType<Queue>(rehydrated.Q);
            Assert.IsType<Stack>(rehydrated.S);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
