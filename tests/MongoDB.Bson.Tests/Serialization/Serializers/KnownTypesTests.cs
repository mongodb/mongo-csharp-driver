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
    public class KnownTypesTests
    {
        [BsonKnownTypes(typeof(B), typeof(C))]
        private class A
        {
            public string P { get; set; }
        }

        [BsonKnownTypes(typeof(D))]
        private class B : A
        {
        }

        [BsonDiscriminator(RootClass = true)]
        [BsonKnownTypes(typeof(E))]
        private class C : A
        {
        }

        private class D : B
        {
        }

        private class E : C
        {
        }

        static KnownTypesTests()
        {
            BsonClassMap.RegisterClassMap<A>();
        }

        [Fact]
        public void TestDeserializeDAsA()
        {
            var document = new BsonDocument
            {
                { "_t", "D" },
                { "P", "x" }
            };

            var bson = document.ToBson();
            var rehydrated = (D)BsonSerializer.Deserialize<A>(bson);
            Assert.IsType<D>(rehydrated);

            var json = rehydrated.ToJson<A>();
            var expected = "{ '_t' : 'D', 'P' : 'x' }".Replace("'", "\"");
            Assert.Equal(expected, json);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Fact]
        public void TestDeserializeEAsA()
        {
            var document = new BsonDocument
            {
                { "_t", new BsonArray { "C", "E" } },
                { "P", "x" }
            };

            var bson = document.ToBson();
            var rehydrated = (E)BsonSerializer.Deserialize<A>(bson);
            Assert.IsType<E>(rehydrated);

            var json = rehydrated.ToJson<A>();
            var expected = "{ '_t' : ['C', 'E'], 'P' : 'x' }".Replace("'", "\"");
            Assert.Equal(expected, json);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }
    }
}
