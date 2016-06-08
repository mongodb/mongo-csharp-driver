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
    public class DiscriminatorTests
    {
        [BsonDiscriminator("A~")] // make discriminators unique with respect to object
        private class A
        {
            public string P { get; set; }
        }

        [BsonDiscriminator("B~")]
        private class B : A
        {
        }

        [BsonDiscriminator("C~", Required = true)]
        private class C : A
        {
        }

        [BsonDiscriminator("D~", RootClass = true)]
        private class D : A
        {
        }

        [BsonDiscriminator("E~")]
        private class E : B
        {
        }

        [BsonDiscriminator("F~")]
        private class F : C
        {
        }

        [BsonDiscriminator("G~")]
        private class G : D
        {
        }

        [BsonDiscriminator("H~")]
        private class H : G
        {
        }

        [Fact]
        public void TestSerializeObjectasObject()
        {
            object o = new object();
            var json = o.ToJson<object>();
            Assert.Equal("{ }", json);

            var bson = o.ToBson<object>();
            var rehydrated = BsonSerializer.Deserialize<object>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
        }

        [Fact]
        public void TestSerializeAAsObject()
        {
            A a = new A { P = "x" };
            var json = a.ToJson<object>();
            var expected = ("{ '_t' : 'A~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = a.ToBson<object>();
            var rehydrated = BsonSerializer.Deserialize<object>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
        }

        [Fact]
        public void TestSerializeAAsA()
        {
            A a = new A { P = "x" };
            var json = a.ToJson<A>();
            var expected = ("{ 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = a.ToBson<A>();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Fact]
        public void TestSerializeBAsObject()
        {
            B b = new B { P = "x" };
            var json = b.ToJson<object>();
            var expected = ("{ '_t' : 'B~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = b.ToBson<object>();
            var rehydrated = BsonSerializer.Deserialize<object>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
        }

        [Fact]
        public void TestSerializeBAsA()
        {
            B b = new B { P = "x" };
            var json = b.ToJson<A>();
            var expected = ("{ '_t' : 'B~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = b.ToBson<A>();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Fact]
        public void TestSerializeBAsB()
        {
            B b = new B { P = "x" };
            var json = b.ToJson<B>();
            var expected = ("{ 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = b.ToBson<B>();
            var rehydrated = BsonSerializer.Deserialize<B>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<B>()));
        }

        [Fact]
        public void TestSerializeCAsObject()
        {
            C c = new C { P = "x" };
            var json = c.ToJson<object>();
            var expected = ("{ '_t' : 'C~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson<object>();
            var rehydrated = BsonSerializer.Deserialize<object>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
        }

        [Fact]
        public void TestSerializeCAsA()
        {
            C c = new C { P = "x" };
            var json = c.ToJson<A>();
            var expected = ("{ '_t' : 'C~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson<A>();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Fact]
        public void TestSerializeCAsC()
        {
            C c = new C { P = "x" };
            var json = c.ToJson<C>();
            var expected = ("{ '_t' : 'C~', 'P' : 'x' }").Replace("'", "\""); // discriminator is required
            Assert.Equal(expected, json);

            var bson = c.ToBson<C>();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<C>()));
        }

        [Fact]
        public void TestSerializeDAsObject()
        {
            D d = new D { P = "x" };
            var json = d.ToJson<object>();
            var expected = ("{ '_t' : 'D~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = d.ToBson<object>();
            var rehydrated = BsonSerializer.Deserialize<object>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
        }

        [Fact]
        public void TestSerializeDAsA()
        {
            D d = new D { P = "x" };
            var json = d.ToJson<A>();
            var expected = ("{ '_t' : 'D~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = d.ToBson<A>();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Fact]
        public void TestSerializeDAsD()
        {
            D d = new D { P = "x" };
            var json = d.ToJson<D>();
            var expected = ("{ '_t' : 'D~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = d.ToBson<D>();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<D>()));
        }

        [Fact]
        public void TestSerializeEAsObject()
        {
            E e = new E { P = "x" };
            var json = e.ToJson<object>();
            var expected = ("{ '_t' : 'E~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = e.ToBson<object>();
            var rehydrated = BsonSerializer.Deserialize<object>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
        }

        [Fact]
        public void TestSerializeEAsA()
        {
            E e = new E { P = "x" };
            var json = e.ToJson<A>();
            var expected = ("{ '_t' : 'E~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = e.ToBson<A>();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Fact]
        public void TestSerializeEAsB()
        {
            E e = new E { P = "x" };
            var json = e.ToJson<B>();
            var expected = ("{ '_t' : 'E~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = e.ToBson<B>();
            var rehydrated = BsonSerializer.Deserialize<B>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<B>()));
        }

        [Fact]
        public void TestSerializeEAsE()
        {
            E e = new E { P = "x" };
            var json = e.ToJson<E>();
            var expected = ("{ 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = e.ToBson<E>();
            var rehydrated = BsonSerializer.Deserialize<E>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<E>()));
        }

        [Fact]
        public void TestSerializeFAsObject()
        {
            F f = new F { P = "x" };
            var json = f.ToJson<object>();
            var expected = ("{ '_t' : 'F~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = f.ToBson<object>();
            var rehydrated = BsonSerializer.Deserialize<object>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
        }

        [Fact]
        public void TestSerializeFAsA()
        {
            F f = new F { P = "x" };
            var json = f.ToJson<A>();
            var expected = ("{ '_t' : 'F~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = f.ToBson<A>();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Fact]
        public void TestSerializeFAsC()
        {
            F f = new F { P = "x" };
            var json = f.ToJson<C>();
            var expected = ("{ '_t' : 'F~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = f.ToBson<C>();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<C>()));
        }

        [Fact]
        public void TestSerializeFAsF()
        {
            F f = new F { P = "x" };
            var json = f.ToJson<F>();
            var expected = ("{ '_t' : 'F~', 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = f.ToBson<F>();
            var rehydrated = BsonSerializer.Deserialize<F>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<F>()));
        }

        [Fact]
        public void TestSerializeGAsObject()
        {
            G g = new G { P = "x" };
            var json = g.ToJson<object>();
            var expected = ("{ '_t' : ['D~', 'G~'], 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = g.ToBson<object>();
            var rehydrated = BsonSerializer.Deserialize<object>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
        }

        [Fact]
        public void TestSerializeGAsA()
        {
            G g = new G { P = "x" };
            var json = g.ToJson<A>();
            var expected = ("{ '_t' : ['D~', 'G~'], 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = g.ToBson<A>();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Fact]
        public void TestSerializeGAsD()
        {
            G g = new G { P = "x" };
            var json = g.ToJson<D>();
            var expected = ("{ '_t' : ['D~', 'G~'], 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = g.ToBson<D>();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<D>()));
        }

        [Fact]
        public void TestSerializeGAsG()
        {
            G g = new G { P = "x" };
            var json = g.ToJson<G>();
            var expected = ("{ '_t' : ['D~', 'G~'], 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = g.ToBson<G>();
            var rehydrated = BsonSerializer.Deserialize<G>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<G>()));
        }

        [Fact]
        public void TestSerializeHAsObject()
        {
            H h = new H { P = "x" };
            var json = h.ToJson<object>();
            var expected = ("{ '_t' : ['D~', 'G~', 'H~'], 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = h.ToBson<object>();
            var rehydrated = BsonSerializer.Deserialize<object>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
        }

        [Fact]
        public void TestSerializeHAsA()
        {
            H h = new H { P = "x" };
            var json = h.ToJson<A>();
            var expected = ("{ '_t' : ['D~', 'G~', 'H~'], 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = h.ToBson<A>();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Fact]
        public void TestSerializeHAsD()
        {
            H h = new H { P = "x" };
            var json = h.ToJson<D>();
            var expected = ("{ '_t' : ['D~', 'G~', 'H~'], 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = h.ToBson<D>();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<D>()));
        }

        [Fact]
        public void TestSerializeHAsG()
        {
            H h = new H { P = "x" };
            var json = h.ToJson<G>();
            var expected = ("{ '_t' : ['D~', 'G~', 'H~'], 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = h.ToBson<G>();
            var rehydrated = BsonSerializer.Deserialize<G>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<G>()));
        }

        [Fact]
        public void TestSerializeHAsH()
        {
            H h = new H { P = "x" };
            var json = h.ToJson<H>();
            var expected = ("{ '_t' : ['D~', 'G~', 'H~'], 'P' : 'x' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = h.ToBson<H>();
            var rehydrated = BsonSerializer.Deserialize<H>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<H>()));
        }
    }
}
