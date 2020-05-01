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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{

    public class DictionarySerializerBaseSubclassTests
    {
        private class CustomDictionarySerializer<TKey, TValue>
            : DictionarySerializerBase<Dictionary<TKey, TValue>, TKey, TValue>
        {
            [Obsolete]
            protected override Dictionary<TKey, TValue> CreateInstance()
            {
                return new Dictionary<TKey, TValue>();
            }
        }

        // Tests to ensure that subclasses of DictionarySerializeBase that override the obselete "CreateInstance" work
        [Fact]
        public void TestCustomDictionarySerializerSerializeAndDeserialize()
        {
            var map = new Dictionary<object, object> { { "power", 9001 } };
            var serializer = new CustomDictionarySerializer<object, object>();
            var expected = @"{ ""power"" : 9001 }";

            var json = map.ToJson(writerSettings: null, serializer: serializer);

            Assert.Equal(expected, json);

            var bson = map.ToBson(serializer: serializer);
            using (var buffer = new ByteArrayBuffer(bytes: bson, isReadOnly: true))
            using (var stream = new ByteBufferStream(buffer))
            using (var bsonReader = new BsonBinaryReader(stream))
            {
                var context = BsonDeserializationContext.CreateRoot(reader: bsonReader, configurator: null);

                var rehydrated = serializer.Deserialize(context);

                Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
            }
        }

    }
}
