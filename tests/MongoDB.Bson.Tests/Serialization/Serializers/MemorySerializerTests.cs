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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class MemorySerializerTests
    {
        public class ReadonlyMemoryHolder<T>
        {
            public ReadOnlyMemory<T> Items { get; set; }
        }

        public class ReadonlyMemoryHolderBytesAsArray
        {
            [BsonRepresentation(BsonType.Array)]
            public ReadOnlyMemory<byte> Items { get; set; }
        }

        public class ReadonlyMemoryHolderBytesAsBinary
        {
            [BsonRepresentation(BsonType.Binary)]
            public ReadOnlyMemory<byte> Items { get; set; }
        }

        public class MemoryHolder<T>
        {
            public Memory<T> Items { get; set; }
        }

        public class MemoryHolderBytesAsArray
        {
            [BsonRepresentation(BsonType.Array)]
            public Memory<byte> Items { get; set; }
        }

        public class MemoryHolderBytesAsBinary
        {
            [BsonRepresentation(BsonType.Binary)]
            public Memory<byte> Items { get; set; }
        }

        public class ArrayHolder<T>
        {
            public T[] Items { get; set; }
        }

        public class ArrayHolderDecimal
        {
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal[] Items { get; set; }
        }

        public class MultiHolder
        {
            public Memory<byte> ItemsBytes { get; set; }
            public Memory<float> ItemsFloat { get; set; }
            public ReadOnlyMemory<int> ItemsInt { get; set; }
            public ReadOnlyMemory<double> ItemsDouble { get; set; }
        }

        [Theory]
        [MemberData(nameof(NonSupportedTestData))]
        public void Memory_should_throw_on_non_primitive_numeric_types<T>(T[] notSupportedData)
        {
            var memoryHolder = new MemoryHolder<T>() { Items = notSupportedData };

            var exception = Record.Exception(() => memoryHolder.ToBson());
            var e = exception.Should()
                .BeOfType<BsonSerializationException>().Subject.InnerException.Should()
                .BeOfType<TargetInvocationException>().Subject.InnerException.Should()
                .BeOfType<NotSupportedException>().Subject;

            e.Message.Should().StartWith("Not supported memory type");
        }

        [Theory]
        [MemberData(nameof(NonSupportedTestData))]
        public void ReadonlyMemory_should_throw_on_non_primitive_numeric_types<T>(T[] notSupportedData)
        {
            var memoryHolder = new ReadonlyMemoryHolder<T>() { Items = notSupportedData };

            var exception = Record.Exception(() => memoryHolder.ToBson());
            var e = exception.Should()
                .BeOfType<BsonSerializationException>().Subject.InnerException.Should()
                .BeOfType<TargetInvocationException>().Subject.InnerException.Should()
                .BeOfType<NotSupportedException>().Subject;

            e.Message.Should().StartWith("Not supported memory type");
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Memory_should_roundtrip_equivalent_to_array<T>(T[] array)
        {
            var memoryHolder = new MemoryHolder<T>() { Items = array };
            var memoryBson = memoryHolder.ToBson();
            var arrayBson = GetArrayHolderBson(array);

            memoryBson.ShouldAllBeEquivalentTo(arrayBson);

            var memoryHolderMaterialized = BsonSerializer.Deserialize<MemoryHolder<T>>(memoryBson);
            memoryHolderMaterialized.Items.ToArray().ShouldAllBeEquivalentTo(memoryHolder.Items.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ReadonlyMemory_should_roundtrip_equivalent_to_array<T>(T[] array)
        {
            var memoryHolder = new ReadonlyMemoryHolder<T>() { Items = array };
            var memoryBson = memoryHolder.ToBson();
            var arrayBson = GetArrayHolderBson(array);

            memoryBson.ShouldAllBeEquivalentTo(arrayBson);

            var memoryHolderMaterialized = BsonSerializer.Deserialize<ReadonlyMemoryHolder<T>>(memoryBson);
            memoryHolderMaterialized.Items.ToArray().ShouldAllBeEquivalentTo(memoryHolder.Items.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestDataSpecialValues))]
        public void Memory_should_roundtrip_special_values<T>(T[] array)
        {
            var memoryHolder = new MemoryHolder<T>() { Items = array };
            var memoryBson = memoryHolder.ToBson();

            var memoryHolderMaterialized = BsonSerializer.Deserialize<MemoryHolder<T>>(memoryBson);
            memoryHolderMaterialized.Items.ToArray().ShouldAllBeEquivalentTo(memoryHolder.Items.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestDataSpecialValues))]
        public void ReadonlyMemory_should_roundtrip_special_values<T>(T[] array)
        {
            var memoryHolder = new ReadonlyMemoryHolder<T>() { Items = array };
            var memoryBson = memoryHolder.ToBson();

            var memoryHolderMaterialized = BsonSerializer.Deserialize<MemoryHolder<T>>(memoryBson);
            memoryHolderMaterialized.Items.ToArray().ShouldAllBeEquivalentTo(memoryHolder.Items.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Memory_should_roundtrip_special_values_correctly<T>(T[] array)
        {
            var memoryHolder = new MemoryHolder<T>() { Items = array };
            var memoryBson = memoryHolder.ToBson();

            var memoryHolderMaterialized = BsonSerializer.Deserialize<MemoryHolder<T>>(memoryBson);
            memoryHolderMaterialized.Items.ToArray().ShouldAllBeEquivalentTo(memoryHolder.Items.ToArray());
        }

        [Theory]
        [MemberData(nameof(NonSupportedRepresentationTypes))]
        public void MemorySerializer_should_throw_on_not_supported_representation<T>(T item, BsonType representation)
            where T : struct
        {
            var exception = Record.Exception(() => new MemorySerializer<T>(representation));
            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("representation");
        }

        [Fact]
        public void Memory_should_support_array_representation_for_bytes()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var memoryHolder = new MemoryHolderBytesAsArray() { Items = bytes };

            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(memoryHolder.ToBson());
            var itemsElement = bsonDocument[nameof(memoryHolder.Items)];

            itemsElement.BsonType.Should().Be(BsonType.Array);
            itemsElement.AsBsonArray.ShouldAllBeEquivalentTo(bytes);
        }

        [Fact]
        public void ReadonlyMemory_should_support_array_representation_for_bytes()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var memoryHolder = new ReadonlyMemoryHolderBytesAsArray() { Items = bytes };

            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(memoryHolder.ToBson());
            var itemsElement = bsonDocument[nameof(memoryHolder.Items)];

            itemsElement.BsonType.Should().Be(BsonType.Array);
            itemsElement.AsBsonArray.ShouldAllBeEquivalentTo(bytes);
        }

        [Fact]
        public void Memory_should_support_binary_representation_for_bytes()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var memoryHolder = new ReadonlyMemoryHolderBytesAsBinary() { Items = bytes };

            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(memoryHolder.ToBson());
            var itemsElement = bsonDocument[nameof(memoryHolder.Items)];

            itemsElement.BsonType.Should().Be(BsonType.Binary);
            itemsElement.AsByteArray.ShouldAllBeEquivalentTo(bytes);
        }

        [Fact]
        public void ReadonlyMemory_should_support_binary_representation_for_bytes()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var memoryHolder = new ReadonlyMemoryHolderBytesAsBinary() { Items = bytes };

            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(memoryHolder.ToBson());
            var itemsElement = bsonDocument[nameof(memoryHolder.Items)];

            itemsElement.BsonType.Should().Be(BsonType.Binary);
            itemsElement.AsByteArray.ShouldAllBeEquivalentTo(bytes);
        }

        [Fact]
        public void Mixed_memory_types_should_roundtrip_correctly()
        {
            var multiHolder = new MultiHolder()
            {
                ItemsBytes = new byte[] { 1, 2, 3 },
                ItemsDouble = new double[] { 1.1, 2.2, 3.3 },
                ItemsFloat = new float[] { 11.1f, 22.2f, 33.3f },
                ItemsInt = new int[] { 10, 100, 1000 }
            };

            var bson = multiHolder.ToBson();
            var materialized = BsonSerializer.Deserialize<MultiHolder>(bson);

            materialized.ItemsBytes.ToArray().ShouldAllBeEquivalentTo(multiHolder.ItemsBytes.ToArray());
            materialized.ItemsDouble.ToArray().ShouldAllBeEquivalentTo(multiHolder.ItemsDouble.ToArray());
            materialized.ItemsFloat.ToArray().ShouldAllBeEquivalentTo(multiHolder.ItemsFloat.ToArray());
            materialized.ItemsInt.ToArray().ShouldAllBeEquivalentTo(multiHolder.ItemsInt.ToArray());
        }

        [Fact]
        public void Empty_memory_should_roundtrip_correctly()
        {
            var multiHolder = new MultiHolder()
            {
                ItemsBytes = new byte[0],
                ItemsDouble = new double[0]
            };

            var bson = multiHolder.ToBson();
            var materialized = BsonSerializer.Deserialize<MultiHolder>(bson);

            materialized.ItemsBytes.ToArray().ShouldAllBeEquivalentTo(multiHolder.ItemsBytes.ToArray());
            materialized.ItemsDouble.ToArray().ShouldAllBeEquivalentTo(multiHolder.ItemsDouble.ToArray());
            materialized.ItemsFloat.ToArray().ShouldAllBeEquivalentTo(multiHolder.ItemsFloat.ToArray());
            materialized.ItemsInt.ToArray().ShouldAllBeEquivalentTo(multiHolder.ItemsInt.ToArray());
        }

        public readonly static IEnumerable<object[]> TestData =
        [
            [ GetArray(i => (bool)( i % 2 == 0)) ],
            [ GetArray(i => (sbyte)i) ],
            [ GetArray(i => (byte)i) ],
            [ GetArray(i => (short)i) ],
            [ GetArray(i => (ushort)i) ],
            [ GetArray(i => (char)i) ],
            [ GetArray(i => (int)i) ],
            [ GetArray(i => (uint)i) ],
            [ GetArray(i => (long)i) ],
            [ GetArray(i => (ulong)i) ],
            [ GetArray(i => (float)i) ],
            [ GetArray(i => (double)i) ],
            [ GetArray(i => (decimal)i) ]
        ];

        public static readonly IEnumerable<object[]> TestDataSpecialValues =
        [
            [ new[] { sbyte.MaxValue, sbyte.MinValue } ],
            [ new[] { byte.MaxValue, byte.MinValue } ],
            [ new[] { short.MaxValue, short.MinValue } ],
            [ new[] { ushort.MaxValue, ushort.MinValue } ],
            [ new[] { char.MaxValue, char.MinValue } ],
            [ new[] { int.MaxValue, int.MinValue } ],
            [ new[] { uint.MaxValue, uint.MinValue } ],
            [ new[] { long.MaxValue, long.MinValue } ],
            [ new[] { ulong.MaxValue, ulong.MinValue } ],
            [ new[] { float.MaxValue, float.MinValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN } ],
            [ new[] { double.MaxValue, double.MinValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN } ],
            [ new[] { decimal.MaxValue, decimal.MinValue, decimal.One, decimal.Zero, decimal.MinusOne }]
        ];

        public static readonly IEnumerable<object[]> NonSupportedTestData =
        [
            [ new[] { "str" } ],
            [ new[] { new object() }],
            [ new[] { new TimeSpan() }]
        ];

        public static IEnumerable<object[]> NonSupportedRepresentationTypes()
        {
            foreach (var bsonType in Enum.GetValues(typeof(BsonType)).Cast<BsonType>())
            {
                if (bsonType == BsonType.Array)
                    continue;

                yield return new object[] { 1, bsonType };
            }
        }

        private static T[] GetArray<T>(Func<int, T> converter) =>
            Enumerable.Range(0, 16).Select(converter).ToArray();

        private static byte[] GetArrayHolderBson<T>(T[] array) => typeof(T) switch
        {
            var t when t == typeof(decimal) => (new ArrayHolderDecimal() { Items = array as decimal[] }).ToBson(),
            _ => (new ArrayHolder<T>() { Items = array }).ToBson(),
        };
    }

    public class ReadOnlyMemorySerializerEqualsTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ReadonlyMemorySerializer<byte>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ReadonlyMemorySerializer<byte>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ReadonlyMemorySerializer<byte>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ReadonlyMemorySerializer<byte>();
            var y = new ReadonlyMemorySerializer<byte>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new ReadonlyMemorySerializer<byte>(BsonType.Binary);
            var y = new ReadonlyMemorySerializer<byte>(BsonType.Array);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ReadonlyMemorySerializer<byte>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class MemorySerializerEqualsTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new MemorySerializer<byte>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new MemorySerializer<byte>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new MemorySerializer<byte>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new MemorySerializer<byte>();
            var y = new MemorySerializer<byte>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new MemorySerializer<byte>(BsonType.Binary);
            var y = new MemorySerializer<byte>(BsonType.Array);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new MemorySerializer<byte>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class MemorySerializerBaseEqualsTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new ConcreteMemorySerializerBase<byte, Memory<byte>>();
            var y = new DerivedFromConcreteMemorySerializerBase<byte, Memory<byte>>();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ConcreteMemorySerializerBase<byte, Memory<byte>>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ConcreteMemorySerializerBase<byte, Memory<byte>>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ConcreteMemorySerializerBase<byte, Memory<byte>>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ConcreteMemorySerializerBase<byte, Memory<byte>>();
            var y = new ConcreteMemorySerializerBase<byte, Memory<byte>>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new ConcreteMemorySerializerBase<byte, Memory<byte>>(BsonType.Binary);
            var y = new ConcreteMemorySerializerBase<byte, Memory<byte>>(BsonType.Array);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ConcreteMemorySerializerBase<byte, Memory<byte>>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class ConcreteMemorySerializerBase<TItem, TMemory> : MemorySerializerBase<TItem, TMemory>
            where TMemory : struct
        {
            public ConcreteMemorySerializerBase() : base() { }
            public ConcreteMemorySerializerBase(BsonType representation) : base(representation) { }
            public override MemorySerializerBase<TItem, TMemory> WithRepresentation(BsonType representation) => throw new NotImplementedException();
            protected override TMemory CreateMemory(TItem[] items) => throw new NotImplementedException();
            protected override Memory<TItem> GetMemory(TMemory memory) => throw new NotImplementedException();
        }

        public class DerivedFromConcreteMemorySerializerBase<TItem, TMemory> : ConcreteMemorySerializerBase<TItem, TMemory>
            where TMemory : struct
        {
        }
    }
}
