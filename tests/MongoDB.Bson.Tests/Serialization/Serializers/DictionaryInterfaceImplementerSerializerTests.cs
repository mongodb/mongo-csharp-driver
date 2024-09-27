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
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class DictionaryInterfaceImplementerSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Hashtable>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Hashtable>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Hashtable>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Hashtable>();
            var y = new DictionaryInterfaceImplementerSerializer<Hashtable>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("dictionaryRepresentation")]
        [InlineData("keySerializer")]
        [InlineData("valueSerializer")]
        public void Equals_with_not_equal_fields_should_return_true(string notEqualFieldName)
        {
            var dictionaryRepresentation1 = DictionaryRepresentation.ArrayOfArrays;
            var dictionaryRepresentation2 = DictionaryRepresentation.ArrayOfDocuments;
            var keySerializer1 = new Int32Serializer(BsonType.Int32);
            var keySerializer2 = new Int32Serializer(BsonType.String);
            var valueSerializer1 = new Int32Serializer(BsonType.Int32);
            var valueSerializer2 = new Int32Serializer(BsonType.String);

            var x = new DictionaryInterfaceImplementerSerializer<Hashtable>(dictionaryRepresentation1, keySerializer1, valueSerializer1);
            var y = notEqualFieldName switch
            {
                "dictionaryRepresentation" => new DictionaryInterfaceImplementerSerializer<Hashtable>(dictionaryRepresentation2, keySerializer1, valueSerializer1),
                "keySerializer" => new DictionaryInterfaceImplementerSerializer<Hashtable>(dictionaryRepresentation1, keySerializer2, valueSerializer1),
                "valueSerializer" => new DictionaryInterfaceImplementerSerializer<Hashtable>(dictionaryRepresentation1, keySerializer1, valueSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Hashtable>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class DictionaryInterfaceImplementerSerializerGenericTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();
            var y = new DerivedFromDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();
            var y = new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("dictionaryRepresentation")]
        [InlineData("keySerializer")]
        [InlineData("valueSerializer")]
        public void Equals_with_not_equal_fields_should_return_true(string notEqualFieldName)
        {
            var dictionaryRepresentation1 = DictionaryRepresentation.ArrayOfArrays;
            var dictionaryRepresentation2 = DictionaryRepresentation.ArrayOfDocuments;
            var keySerializer1 = new Int32Serializer(BsonType.Int32);
            var keySerializer2 = new Int32Serializer(BsonType.String);
            var valueSerializer1 = new Int32Serializer(BsonType.Int32);
            var valueSerializer2 = new Int32Serializer(BsonType.String);

            var x = new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>(dictionaryRepresentation1, keySerializer1, valueSerializer1);
            var y = notEqualFieldName switch
            {
                "dictionaryRepresentation" => new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>(dictionaryRepresentation2, keySerializer1, valueSerializer1),
                "keySerializer" => new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>(dictionaryRepresentation1, keySerializer2, valueSerializer1),
                "valueSerializer" => new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>(dictionaryRepresentation1, keySerializer1, valueSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class DerivedFromDictionaryInterfaceImplementerSerializer<TDictionary, TKey, TValue> : DictionaryInterfaceImplementerSerializer<TDictionary, TKey, TValue>
            where TDictionary : class, IDictionary<TKey, TValue>
        {
        }
    }
}
