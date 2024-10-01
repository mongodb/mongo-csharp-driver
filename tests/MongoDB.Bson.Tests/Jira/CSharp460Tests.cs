﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp460Tests
    {
        public interface IFooBarList<T> : IList<T>
        {
        }

        public sealed class DummyCollection<T> : IFooBarList<T>
        {
            private T[] items;

            public DummyCollection()
            {
                this.items = new T[0];
            }

            // ICollection<T> Members

            public int Count
            {
                get { return this.items.Length; }
            }

            bool ICollection<T>.IsReadOnly
            {
                get { return false; }
            }

            public void Add(T item)
            {
                var index = this.items.Length;
                Array.Resize(ref this.items, index + 1);
                this.items[index] = item;
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            bool ICollection<T>.Contains(T item)
            {
                throw new NotImplementedException();
            }

            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public bool Remove(T item)
            {
                throw new NotImplementedException();
            }

            // IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return ((IEnumerable<T>)this.items).GetEnumerator();
            }

            // IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            // IList<T> members

            int IList<T>.IndexOf(T item)
            {
                throw new NotImplementedException();
            }

            void IList<T>.Insert(int index, T item)
            {
                throw new NotImplementedException();
            }

            void IList<T>.RemoveAt(int index)
            {
                throw new NotImplementedException();
            }

            T IList<T>.this[int index]
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }

        public class A
        {
            [BsonElement("a", Order = 0)]
            [BsonIgnoreIfDefault]
            public DummyCollection<int> Values1
            {
                get;
                set;
            }

            [BsonElement("b", Order = 0)]
            [BsonIgnoreIfDefault]
            public DummyCollection<KeyValuePair<int, int>> Values2
            {
                get;
                set;
            }
        }

        [Fact]
        public void TestUserCollection()
        {
            const int Count = 10;

            var collection1 = new DummyCollection<int>();
            var collection2 = new DummyCollection<KeyValuePair<int, int>>();
            for (var i = 0; i < Count; ++i)
            {
                collection1.Add(i);
                collection2.Add(new KeyValuePair<int, int>(i, i));
            }

            var document = new A
            {
                Values1 = collection1,
                Values2 = collection2,
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
            Assert.Equal(10, rehydrated.Values1.Count);
            Assert.Equal(10, rehydrated.Values2.Count);
        }

        [Fact]
        public void TestKeyValuePair1()
        {
            var p = new KeyValuePair<string, int>("a", 42);
            var json = p.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ \"k\" : \"a\", \"v\" : 42 }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = p.ToBson();
            var rehydrated = BsonSerializer.Deserialize<KeyValuePair<string, int>>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestKeyValuePair2()
        {
            var p = new KeyValuePair<string, int>("a", 42);
            var json = p.ToJson(serializer: new KeyValuePairSerializer<string, int>(BsonType.Array));
            var expected = ("[\"a\", 42]").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = p.ToBson();
            var rehydrated = BsonSerializer.Deserialize<KeyValuePair<string, int>>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
