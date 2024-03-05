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
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class EnumerableInterfaceImplementerSerializerBaseTests
    {
        private static readonly IBsonSerializer __itemSerializer1;
        private static readonly IBsonSerializer __itemSerializer2;

        static EnumerableInterfaceImplementerSerializerBaseTests()
        {
            __itemSerializer1 = new Int32Serializer(BsonType.Int32);
            __itemSerializer2 = new Int32Serializer(BsonType.String);
        }

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer1);
            var y = new DerivedFromConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer1);
            var y = (EnumerableInterfaceImplementerSerializerBase<List<int>>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer1);
            var y = (EnumerableInterfaceImplementerSerializerBase<List<int>>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>>(__itemSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class ConcreteEnumerableInterfaceImplementerSerializerBase<TValue> : EnumerableInterfaceImplementerSerializerBase<TValue>
            where TValue : class, IList, new()
        {
            public ConcreteEnumerableInterfaceImplementerSerializerBase(IBsonSerializer itemSerializer)
                : base(itemSerializer)
            {
            }

            protected override object CreateAccumulator() => throw new NotImplementedException();
        }

        public class DerivedFromConcreteEnumerableInterfaceImplementerSerializerBase<TValue> : ConcreteEnumerableInterfaceImplementerSerializerBase<TValue>
            where TValue : class, IList, new()
        {
            public DerivedFromConcreteEnumerableInterfaceImplementerSerializerBase(IBsonSerializer itemSerializer)
                : base(itemSerializer)
            {
            }
        }
    }

    public class EnumerableInterfaceImplementerSerializerBaseGenericTests
    {
        private static readonly IBsonSerializer<int> __itemSerializer1;
        private static readonly IBsonSerializer<int> __itemSerializer2;

        static EnumerableInterfaceImplementerSerializerBaseGenericTests()
        {
            __itemSerializer1 = new Int32Serializer(BsonType.Int32);
            __itemSerializer2 = new Int32Serializer(BsonType.String);
        }

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>, int>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer1);
            var y = new DerivedFromConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>, int>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>, int>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>, int>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>, int>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer1);
            var y = (EnumerableInterfaceImplementerSerializerBase<List<int>, int>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>, int>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer1);
            var y = (EnumerableInterfaceImplementerSerializerBase<List<int>, int>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (EnumerableInterfaceImplementerSerializerBase<List<int>, int>)new ConcreteEnumerableInterfaceImplementerSerializerBase<List<int>, int>(__itemSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class ConcreteEnumerableInterfaceImplementerSerializerBase<TValue, TItem> : EnumerableInterfaceImplementerSerializerBase<TValue, TItem>
            where TValue : class, IEnumerable<TItem>
        {
            public ConcreteEnumerableInterfaceImplementerSerializerBase(IBsonSerializer<TItem> itemSerializer)
                : base(itemSerializer)
            {
            }

            protected override object CreateAccumulator() => throw new NotImplementedException();
        }

        public class DerivedFromConcreteEnumerableInterfaceImplementerSerializerBase<TValue, TItem> : ConcreteEnumerableInterfaceImplementerSerializerBase<TValue, TItem>
            where TValue : class, IEnumerable<TItem>
        {
            public DerivedFromConcreteEnumerableInterfaceImplementerSerializerBase(IBsonSerializer<TItem> itemSerializer)
                : base(itemSerializer)
            {
            }
        }
    }
}
