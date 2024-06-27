/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class DictionarySerializerBaseTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (DictionarySerializerBase<Hashtable>)new ConcreteDictionarySerializerBase<Hashtable>();
            var y = new DerivedFromConcreteDictionarySerializerBase<Hashtable>();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (DictionarySerializerBase<Hashtable>)new ConcreteDictionarySerializerBase<Hashtable>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (DictionarySerializerBase<Hashtable>)new ConcreteDictionarySerializerBase<Hashtable>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (DictionarySerializerBase<Hashtable>)new ConcreteDictionarySerializerBase<Hashtable>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (DictionarySerializerBase<Hashtable>)new ConcreteDictionarySerializerBase<Hashtable>();
            var y = (DictionarySerializerBase<Hashtable>)new ConcreteDictionarySerializerBase<Hashtable>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("dictionaryRepresentation")]
        [InlineData("keySerializer")]
        [InlineData("valueSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var discriminatorConvention1 = new ScalarDiscriminatorConvention("_t");
            var discriminatorConvention2 = new HierarchicalDiscriminatorConvention("_t");
            var objectSerializer1 = new ObjectSerializer(discriminatorConvention1);
            var objectSerializer2 = new ObjectSerializer(discriminatorConvention2);

            var x = (DictionarySerializerBase<Hashtable>)new ConcreteDictionarySerializerBase<Hashtable>(DictionaryRepresentation.Document, objectSerializer1, objectSerializer1);
            var y = (DictionarySerializerBase<Hashtable>)(notEqualFieldName switch
            {
                "dictionaryRepresentation" => new ConcreteDictionarySerializerBase<Hashtable>(DictionaryRepresentation.ArrayOfArrays, objectSerializer1, objectSerializer1),
                "keySerializer" => new ConcreteDictionarySerializerBase<Hashtable>(DictionaryRepresentation.Document, objectSerializer2, objectSerializer1),
                "valueSerializer" => new ConcreteDictionarySerializerBase<Hashtable>(DictionaryRepresentation.Document, objectSerializer1, objectSerializer2),
                _ => throw new Exception()
            });

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (DictionarySerializerBase<Hashtable>)new ConcreteDictionarySerializerBase<Hashtable>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class ConcreteDictionarySerializerBase<TDictionary> : DictionarySerializerBase<TDictionary>
            where TDictionary : class, IDictionary
        {
            public ConcreteDictionarySerializerBase() { }

            public ConcreteDictionarySerializerBase(
                DictionaryRepresentation dictionaryRepresentation,
                IBsonSerializer keySerializer,
                IBsonSerializer valueSerializer)
                : base(dictionaryRepresentation, keySerializer, valueSerializer)
            {
            }

            protected override TDictionary CreateInstance() => throw new NotImplementedException();
        }

        public class DerivedFromConcreteDictionarySerializerBase<TDictionary> : ConcreteDictionarySerializerBase<TDictionary>
            where TDictionary : class, IDictionary
        {
        }
    }

    public class DictionarySerializerBaseGenericTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (DictionarySerializerBase<Dictionary<int, int>, int, int>)new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>();
            var y = new DerivedFromConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (DictionarySerializerBase<Dictionary<int, int>, int, int>)new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (DictionarySerializerBase<Dictionary<int, int>, int, int>)new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (DictionarySerializerBase<Dictionary<int, int>, int, int>)new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (DictionarySerializerBase<Dictionary<int, int>, int, int>)new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>();
            var y = (DictionarySerializerBase<Dictionary<int, int>, int, int>)new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("dictionaryRepresentation")]
        [InlineData("keySerializer")]
        [InlineData("valueSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var representation1 = DictionaryRepresentation.Document;
            var representation2 = DictionaryRepresentation.ArrayOfDocuments;
            var int32Serializer1 = new Int32Serializer(BsonType.Int32);
            var int32Serializer2 = new Int32Serializer(BsonType.String);

            var x = (DictionarySerializerBase<Dictionary<int, int>, int, int>)new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>(representation1, int32Serializer1, int32Serializer1);
            var y = (DictionarySerializerBase<Dictionary<int, int>, int, int>)(notEqualFieldName switch
            {
                "dictionaryRepresentation" => new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>(representation2, int32Serializer1, int32Serializer1),
                "keySerializer" => new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>(representation1, int32Serializer2, int32Serializer1),
                "valueSerializer" => new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>(representation1, int32Serializer1, int32Serializer2),
                _ => throw new Exception()
            });

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (DictionarySerializerBase<Dictionary<int, int>, int, int>)new ConcreteDictionarySerializerBase<Dictionary<int, int>, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class ConcreteDictionarySerializerBase<TDictionary, TKey, TValue> : DictionarySerializerBase<TDictionary, TKey, TValue>
            where TDictionary : class, IEnumerable<KeyValuePair<TKey, TValue>>
        {
            public ConcreteDictionarySerializerBase() { }

            public ConcreteDictionarySerializerBase(
                DictionaryRepresentation dictionaryRepresentation,
                IBsonSerializer<TKey> keySerializer,
                IBsonSerializer<TValue> valueSerializer)
                : base(dictionaryRepresentation, keySerializer, valueSerializer)
            {
            }
        }

        public class DerivedFromConcreteDictionarySerializerBase<TDictionary, TKey, TValue> : ConcreteDictionarySerializerBase<TDictionary, TKey, TValue>
            where TDictionary : class, IEnumerable<KeyValuePair<TKey, TValue>>
        {
        }
    }
}
