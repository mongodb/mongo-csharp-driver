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
#if NET6_0_OR_GREATER
using System.Collections.Immutable;
#endif
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class EnumerableSerializerBaseTests
    {
        private static readonly IBsonSerializer __itemSerializer1 = new Int32Serializer(BsonType.Int32);
        private static readonly IBsonSerializer __itemSerializer2 = new Int32Serializer(BsonType.String);

#if NET6_0_OR_GREATER
        [Fact]
        public void Deserialize_a_null_value_into_a_value_type_should_throw()
        {
            const string json = """{ "x" : null }""";
            var subject = new DerivedFromConcreteEnumerableSerializerBase<ImmutableArray<int>>(__itemSerializer1);

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);

            var exception = Record.Exception(() => subject.Deserialize(context));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Be("Cannot deserialize a null value into a value type (type: ImmutableArray<Int32>).");
        }
#endif

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (EnumerableSerializerBase<List<int>>)new ConcreteEnumerableSerializerBase<List<int>>(__itemSerializer1);
            var y = new DerivedFromConcreteEnumerableSerializerBase<List<int>>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (EnumerableSerializerBase<List<int>>)new ConcreteEnumerableSerializerBase<List<int>>(__itemSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (EnumerableSerializerBase<List<int>>)new ConcreteEnumerableSerializerBase<List<int>>(__itemSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (EnumerableSerializerBase<List<int>>)new ConcreteEnumerableSerializerBase<List<int>>(__itemSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (EnumerableSerializerBase<List<int>>)new ConcreteEnumerableSerializerBase<List<int>>(__itemSerializer1);
            var y = (EnumerableSerializerBase<List<int>>)new ConcreteEnumerableSerializerBase<List<int>>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = (EnumerableSerializerBase<List<int>>)new ConcreteEnumerableSerializerBase<List<int>>(__itemSerializer1);
            var y = (EnumerableSerializerBase<List<int>>)new ConcreteEnumerableSerializerBase<List<int>>(__itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (EnumerableSerializerBase<List<int>>)new ConcreteEnumerableSerializerBase<List<int>>(__itemSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class ConcreteEnumerableSerializerBase<TValue> : EnumerableSerializerBase<TValue>
            where TValue : IList, new()
        {
            public ConcreteEnumerableSerializerBase(IBsonSerializer itemSerializer)
                : base(itemSerializer)
            {
            }

            protected override void AddItem(object accumulator, object item) => throw new NotImplementedException();
            protected override object CreateAccumulator() => throw new NotImplementedException();
            protected override IEnumerable EnumerateItemsInSerializationOrder(TValue value) => throw new NotImplementedException();
            protected override TValue FinalizeResult(object accumulator) => throw new NotImplementedException();
        }

        public class DerivedFromConcreteEnumerableSerializerBase<TValue> : ConcreteEnumerableSerializerBase<TValue>
            where TValue : IList, new()
        {
            public DerivedFromConcreteEnumerableSerializerBase(IBsonSerializer itemSerializer)
                : base(itemSerializer)
            {
            }
        }
    }

    public class EnumerableSerializerBaseGenericTests
    {
        private static readonly IBsonSerializer<int> __itemSerializer1 = new Int32Serializer(BsonType.Int32);
        private static readonly IBsonSerializer<int> __itemSerializer2 = new Int32Serializer(BsonType.String);

#if NET6_0_OR_GREATER
        [Fact]
        public void Deserialize_a_null_value_into_a_value_type_should_throw()
        {
            const string json = """{ "x" : null }""";
            var subject = new ConcreteEnumerableSerializerBase<ImmutableArray<int>, int>(__itemSerializer1);

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);

            var exception = Record.Exception(() => subject.Deserialize(context));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Be("Cannot deserialize a null value into a value type (type: ImmutableArray<Int32>).");
        }
#endif

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (EnumerableSerializerBase<List<int>, int>)new ConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer1);
            var y = new DerivedFromConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (EnumerableSerializerBase<List<int>, int>)new ConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (EnumerableSerializerBase<List<int>, int>)new ConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (EnumerableSerializerBase<List<int>, int>)new ConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (EnumerableSerializerBase<List<int>, int>)new ConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer1);
            var y = (EnumerableSerializerBase<List<int>, int>)new ConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = (EnumerableSerializerBase<List<int>, int>)new ConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer1);
            var y = (EnumerableSerializerBase<List<int>, int>)new ConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (EnumerableSerializerBase<List<int>, int>)new ConcreteEnumerableSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class ConcreteEnumerableSerializerBase<TValue, TItem> : EnumerableSerializerBase<TValue, TItem>
            where TValue : IEnumerable<TItem>
        {
            public ConcreteEnumerableSerializerBase(IBsonSerializer<TItem> itemSerializer)
                : base(itemSerializer)
            {
            }

            protected override void AddItem(object accumulator, TItem item) => throw new NotImplementedException();
            protected override object CreateAccumulator() => throw new NotImplementedException();
            protected override IEnumerable<TItem> EnumerateItemsInSerializationOrder(TValue value) => throw new NotImplementedException();
            protected override TValue FinalizeResult(object accumulator) => throw new NotImplementedException();
        }

        public class DerivedFromConcreteEnumerableSerializerBase<TValue, TItem> : ConcreteEnumerableSerializerBase<TValue, TItem>
            where TValue : class, IEnumerable<TItem>
        {
            public DerivedFromConcreteEnumerableSerializerBase(IBsonSerializer<TItem> itemSerializer)
                : base(itemSerializer)
            {
            }
        }
    }
}
