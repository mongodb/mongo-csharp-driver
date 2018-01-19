/* Copyright 2018-present MongoDB Inc.
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class ReadOnlyDictionarySerializerTests
    {
        // container classes for various dictionaries

        public class IReadOnlyDictionaryBox
        {
            public IReadOnlyDictionary<object, object> Map;
        }

        public class ReadOnlyDictionaryBox
        {
            public ReadOnlyDictionary<object, object> Map;
        }

        public class ReadOnlyDictionarySubclassBox
        {
            public ReadOnlyDictionarySubclass<object, object> Map;
        }

        public class CustomIReadOnlyDictionaryBox
        {
            public CustomIReadOnlyDictionary<object, object> Map;
        }

        // "user" created readonly dictionaries

        public class ReadOnlyDictionarySubclass<TKey, TValue> : ReadOnlyDictionary<TKey, TValue>
        {
            public ReadOnlyDictionarySubclass(IDictionary<TKey, TValue> m) : base(m)
            {
            }
        }

        public class CustomIReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        {
            private ReadOnlyDictionary<TKey, TValue> _map;

            public CustomIReadOnlyDictionary(IDictionary<TKey, TValue> map)
            {
                _map = new ReadOnlyDictionary<TKey, TValue>(map);
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _map.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _map).GetEnumerator();

            public int Count => _map.Count;

            public bool ContainsKey(TKey key) => _map.ContainsKey(key);

            public bool TryGetValue(TKey key, out TValue value) => _map.TryGetValue(key, out value);

            public TValue this[TKey key] => _map[key];

            public IEnumerable<TKey> Keys => _map.Keys;
            public IEnumerable<TValue> Values => _map.Values;
        }

        // Tests where nominal type is IReadOnlyDictionary

        [Fact]
        public void TestNominalTypeIReadOnlyDictionaryActualTypeReadOnlyDictionary()
        {
            var map = new Dictionary<object, object> { { "A", 42 } };
            var obj = new IReadOnlyDictionaryBox { Map = new ReadOnlyDictionary<object, object>(map) };
            var json = obj.ToJson();
            var rep = "{ 'A' : 42 }";
            var expected = "{ 'Map' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<IReadOnlyDictionaryBox>(bson);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.Map);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNominalTypeIReadOnlyDictionaryActualTypeCustomIReadOnlyDictionary()
        {
            var map = new Dictionary<object, object> { { "A", 42 } };
            var obj = new IReadOnlyDictionaryBox { Map = new CustomIReadOnlyDictionary<object, object>(map) };
            var json = obj.ToJson();
            var rep = "{ 'A' : 42 }";
            var expected = "{ 'Map' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<IReadOnlyDictionaryBox>(bson);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.Map);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNominalTypeIReadOnlyDictionaryActualTypeReadOnlyDictionarySubclass()
        {
            var map = new Dictionary<object, object> { { "A", 42 } };
            var obj = new IReadOnlyDictionaryBox { Map = new ReadOnlyDictionarySubclass<object, object>(map) };
            var json = obj.ToJson();
            var rep = "{ 'A' : 42 }";
            var expected = "{ 'Map' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<IReadOnlyDictionaryBox>(bson);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.Map);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        // Tests where nominal type is ReadOnlyDictionary

        [Fact]
        public void TestNominalTypeReadOnlyDictionaryActualTypeReadOnlyDictionary()
        {
            var map = new Dictionary<object, object> { { "A", 42 } };
            var obj = new ReadOnlyDictionaryBox { Map = new ReadOnlyDictionary<object, object>(map) };
            var json = obj.ToJson();
            var rep = "{ 'A' : 42 }";
            var expected = "{ 'Map' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<ReadOnlyDictionaryBox>(bson);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.Map);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNominalTypeReadOnlyDictionaryActualTypeReadOnlyDictionarySubclass()
        {
            var map = new Dictionary<object, object> { { "A", 42 } };
            var obj = new ReadOnlyDictionaryBox { Map = new ReadOnlyDictionarySubclass<object, object>(map) };
            var json = obj.ToJson();
            var rep = "{ 'A' : 42 }";
            var expected = "{ 'Map' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<ReadOnlyDictionaryBox>(bson);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.Map);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        // Tests where nominal type is ReadOnlyDictionary subclass

        [Fact]
        public void TestNominalTypeReadOnlyDictionarySubclassActualTypeReadOnlyDictionarySubclass()
        {
            var map = new Dictionary<object, object> { { "A", 42 } };
            var obj = new ReadOnlyDictionarySubclassBox { Map = new ReadOnlyDictionarySubclass<object, object>(map) };
            var json = obj.ToJson();
            var rep = "{ 'A' : 42 }";
            var expected = "{ 'Map' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);
            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<ReadOnlyDictionarySubclassBox>(bson);
            Assert.IsType<ReadOnlyDictionarySubclass<object, object>>(rehydrated.Map);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        // Tests where nominal type is a class that implements IReadOnlyDictionary 

        [Fact]
        public void TestNominalTypeCustomIReadOnlyDictionaryActualTypeCustomIReadOnlyDictionary()
        {
            var map = new Dictionary<object, object> { { "A", 42 } };
            var obj = new CustomIReadOnlyDictionaryBox { Map = new CustomIReadOnlyDictionary<object, object>(map) };
            var json = obj.ToJson();
            var rep = "{ 'A' : 42 }";
            var expected = "{ 'Map' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<CustomIReadOnlyDictionaryBox>(bson);
            Assert.IsType<CustomIReadOnlyDictionary<object, object>>(rehydrated.Map);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

    }
}
