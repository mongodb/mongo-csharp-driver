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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp515
{
    [TestFixture]
    public class CSharp515Tests
    {
        public class C
        {
            public int Id { get; set; }
            public ReadOnlyCollection<int> R { get; set; }
            public S<int> S { get; set; }
            public ReadOnlyCollection<int> RS { get; set; } // actual value will be of type S<int>
            public object OR { get; set; } // actual value will be of type ReadOnlyCollection<int>
            public object OS { get; set; } // actual value will be of type S<int>
        }

        public class S<T> : ReadOnlyCollection<T>
        {
            public S(IList<T> list)
                : base(list)
            {
            }
        }

        private string _jsonTemplate = "{ '_id' : 1, 'R' : #V, 'S' : #V, 'RS' : { '_t' : 'S`1', '_v' : #V }, 'OR' : { '_t' : 'System.Collections.ObjectModel.ReadOnlyCollection`1[System.Int32]', '_v' : #V }, 'OS' : { '_t' : 'MongoDB.BsonUnitTests.Jira.CSharp515.CSharp515Tests+S`1[System.Int32], MongoDB.BsonUnitTests', '_v' : #V } }".Replace("'", "\"");

        [Test]
        public void TestNull()
        {
            var c = new C { Id = 1, R = null, S = null, RS = null, OR = null, OS = null };

            var json = "{ '_id' : 1, 'R' : null, 'S' : null, 'RS' : null, 'OR' : null, 'OS' : null }".Replace("'", "\"");
            Assert.AreEqual(json, c.ToJson());

            var rehydrated = BsonSerializer.Deserialize<C>(c.ToBson());
            Assert.AreEqual(c.Id, rehydrated.Id);
            Assert.AreEqual(null, rehydrated.R);
            Assert.AreEqual(null, rehydrated.S);
            Assert.AreEqual(null, rehydrated.RS);
            Assert.AreEqual(null, rehydrated.OR);
            Assert.AreEqual(null, rehydrated.OS);
        }

        [Test]
        public void TestLength0()
        {
            var list = new List<int>();
            var r = new ReadOnlyCollection<int>(list);
            var s = new S<int>(list);
            var c = new C { Id = 1, R = r, S = s, RS = s, OR = r, OS = s };

            var json = _jsonTemplate.Replace("#V", "[]");
            Assert.AreEqual(json, c.ToJson());

            var rehydrated = BsonSerializer.Deserialize<C>(c.ToBson());
            Assert.AreEqual(c.Id, rehydrated.Id);
            Assert.IsInstanceOf<ReadOnlyCollection<int>>(rehydrated.R);
            Assert.IsInstanceOf<S<int>>(rehydrated.S);
            Assert.IsInstanceOf<S<int>>(rehydrated.RS);
            Assert.IsInstanceOf<ReadOnlyCollection<int>>(rehydrated.OR);
            Assert.IsInstanceOf<S<int>>(rehydrated.OS);
            Assert.AreEqual(0, rehydrated.R.Count);
            Assert.AreEqual(0, rehydrated.S.Count);
            Assert.AreEqual(0, rehydrated.RS.Count);
            Assert.AreEqual(0, ((ReadOnlyCollection<int>)rehydrated.OR).Count);
            Assert.AreEqual(0, ((S<int>)rehydrated.OS).Count);
        }

        [Test]
        public void TestLength1()
        {
            var list = new List<int>() { 1 };
            var r = new ReadOnlyCollection<int>(list);
            var s = new S<int>(list);
            var c = new C { Id = 1, R = r, S = s, RS = s, OR = r, OS = s };

            var json = _jsonTemplate.Replace("#V", "[1]");
            Assert.AreEqual(json, c.ToJson());

            var rehydrated = BsonSerializer.Deserialize<C>(c.ToBson());
            Assert.IsInstanceOf<ReadOnlyCollection<int>>(rehydrated.R);
            Assert.IsInstanceOf<S<int>>(rehydrated.S);
            Assert.IsInstanceOf<S<int>>(rehydrated.RS);
            Assert.IsInstanceOf<ReadOnlyCollection<int>>(rehydrated.OR);
            Assert.IsInstanceOf<S<int>>(rehydrated.OS);
            Assert.AreEqual(c.Id, rehydrated.Id);
            Assert.AreEqual(1, rehydrated.R.Count);
            Assert.AreEqual(1, rehydrated.S.Count);
            Assert.AreEqual(1, rehydrated.RS.Count);
            Assert.AreEqual(1, ((ReadOnlyCollection<int>)rehydrated.OR).Count);
            Assert.AreEqual(1, ((S<int>)rehydrated.OS).Count);
            Assert.AreEqual(1, rehydrated.R[0]);
            Assert.AreEqual(1, rehydrated.S[0]);
            Assert.AreEqual(1, rehydrated.RS[0]);
            Assert.AreEqual(1, ((ReadOnlyCollection<int>)rehydrated.OR)[0]);
            Assert.AreEqual(1, ((S<int>)rehydrated.OS)[0]);
        }

        [Test]
        public void TestLength2()
        {
            var list = new List<int>() { 1, 2 };
            var r = new ReadOnlyCollection<int>(list);
            var s = new S<int>(list);
            var c = new C { Id = 1, R = r, S = s, RS = s, OR = r, OS = s };

            var json = _jsonTemplate.Replace("#V", "[1, 2]");
            Assert.AreEqual(json, c.ToJson());

            var rehydrated = BsonSerializer.Deserialize<C>(c.ToBson());
            Assert.AreEqual(c.Id, rehydrated.Id);
            Assert.IsInstanceOf<ReadOnlyCollection<int>>(rehydrated.R);
            Assert.IsInstanceOf<S<int>>(rehydrated.S);
            Assert.IsInstanceOf<S<int>>(rehydrated.RS);
            Assert.IsInstanceOf<ReadOnlyCollection<int>>(rehydrated.OR);
            Assert.IsInstanceOf<S<int>>(rehydrated.OS);
            Assert.AreEqual(2, rehydrated.R.Count);
            Assert.AreEqual(2, rehydrated.S.Count);
            Assert.AreEqual(2, rehydrated.RS.Count);
            Assert.AreEqual(2, ((ReadOnlyCollection<int>)rehydrated.OR).Count);
            Assert.AreEqual(2, ((S<int>)rehydrated.OS).Count);
            Assert.AreEqual(1, rehydrated.R[0]);
            Assert.AreEqual(1, rehydrated.S[0]);
            Assert.AreEqual(1, rehydrated.RS[0]);
            Assert.AreEqual(1, ((ReadOnlyCollection<int>)rehydrated.OR)[0]);
            Assert.AreEqual(1, ((S<int>)rehydrated.OS)[0]);
            Assert.AreEqual(2, rehydrated.R[1]);
            Assert.AreEqual(2, rehydrated.S[1]);
            Assert.AreEqual(2, rehydrated.RS[1]);
            Assert.AreEqual(2, ((ReadOnlyCollection<int>)rehydrated.OR)[1]);
            Assert.AreEqual(2, ((S<int>)rehydrated.OS)[1]);
        }
    }
}
