﻿/* Copyright 2010-2012 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
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

        [Test]
        public void TestDeserializeDAsA()
        {
            var document = new BsonDocument
            {
                { "_t", "D" },
                { "P", "x" }
            };

            var bson = document.ToBson();
            var rehydrated = (D)BsonSerializer.Deserialize<A>(bson);
            Assert.IsInstanceOf<D>(rehydrated);

            var json = rehydrated.ToJson<A>();
            var expected = "{ '_t' : 'D', 'P' : 'x' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }

        [Test]
        public void TestDeserializeEAsA()
        {
            var document = new BsonDocument
            {
                { "_t", new BsonArray { "C", "E" } },
                { "P", "x" }
            };

            var bson = document.ToBson();
            var rehydrated = (E)BsonSerializer.Deserialize<A>(bson);
            Assert.IsInstanceOf<E>(rehydrated);

            var json = rehydrated.ToJson<A>();
            var expected = "{ '_t' : ['C', 'E'], 'P' : 'x' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson<A>()));
        }
    }
}
