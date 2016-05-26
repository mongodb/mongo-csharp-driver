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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class SerializePolymorphicClassTests
    {
        private abstract class A
        {
            public string FA { get; set; }
        }

        [BsonDiscriminator(Required = true)]
        private abstract class B : A
        {
            public string FB { get; set; }
        }

        private class C : A
        {
            public string FC { get; set; }
        }

        private class D : B
        {
            public string FD { get; set; }
        }

        private class E : B
        {
            public string FE { get; set; }
        }

        private class T
        {
            public A FT { get; set; }
        }

        [Fact]
        public void TestSerializeCasA()
        {
            A a = new C { FA = "a", FC = "c" };
            var json = a.ToJson();
            var expected = ("{ '_t' : 'C', 'FA' : 'a', 'FC' : 'c' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeCasC()
        {
            C c = new C { FA = "a", FC = "c" };
            var json = c.ToJson();
            var expected = ("{ 'FA' : 'a', 'FC' : 'c' }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeDasA()
        {
            A a = new D { FA = "a", FB = "b", FD = "d" };
            var json = a.ToJson();
            var expected = ("{ '_t' : 'D', 'FA' : 'a', 'FB' : 'b', 'FD' : 'd' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeDasB()
        {
            B b = new D { FA = "a", FB = "b", FD = "d" };
            var json = b.ToJson();
            var expected = ("{ '_t' : 'D', 'FA' : 'a', 'FB' : 'b', 'FD' : 'd' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.Deserialize<B>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeDasD()
        {
            D d = new D { FA = "a", FB = "b", FD = "d" };
            var json = d.ToJson();
            var expected = ("{ '_t' : 'D', 'FA' : 'a', 'FB' : 'b', 'FD' : 'd' }").Replace("'", "\""); // has discriminator because B has DiscriminatorIsRequired true
            Assert.Equal(expected, json);

            var bson = d.ToBson();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEasA()
        {
            A a = new E { FA = "a", FB = "b", FE = "e" };
            var json = a.ToJson();
            var expected = ("{ '_t' : 'E', 'FA' : 'a', 'FB' : 'b', 'FE' : 'e' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEasB()
        {
            B b = new E { FA = "a", FB = "b", FE = "e" };
            var json = b.ToJson();
            var expected = ("{ '_t' : 'E', 'FA' : 'a', 'FB' : 'b', 'FE' : 'e' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.Deserialize<B>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEasE()
        {
            E e = new E { FA = "a", FB = "b", FE = "e" };
            var json = e.ToJson();
            var expected = ("{ '_t' : 'E', 'FA' : 'a', 'FB' : 'b', 'FE' : 'e' }").Replace("'", "\""); // has discriminator because B has DiscriminatorIsRequired true
            Assert.Equal(expected, json);

            var bson = e.ToBson();
            var rehydrated = BsonSerializer.Deserialize<E>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeTNull()
        {
            T t = new T { FT = null };
            var json = t.ToJson();
            var expected = ("{ 'FT' : null }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = t.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.Null(rehydrated.FT);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeTC()
        {
            T t = new T { FT = new C { FA = "a", FC = "c" } };
            var json = t.ToJson();
            var expected = ("{ 'FT' : { '_t' : 'C', 'FA' : 'a', 'FC' : 'c' } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = t.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<C>(rehydrated.FT);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeTD()
        {
            T t = new T { FT = new D { FA = "a", FB = "b", FD = "d" } };
            var json = t.ToJson();
            var expected = ("{ 'FT' : { '_t' : 'D', 'FA' : 'a', 'FB' : 'b', 'FD' : 'd' } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = t.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<D>(rehydrated.FT);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeTE()
        {
            T t = new T { FT = new E { FA = "a", FB = "b", FE = "e" } };
            var json = t.ToJson();
            var expected = ("{ 'FT' : { '_t' : 'E', 'FA' : 'a', 'FB' : 'b', 'FE' : 'e' } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = t.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<E>(rehydrated.FT);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
