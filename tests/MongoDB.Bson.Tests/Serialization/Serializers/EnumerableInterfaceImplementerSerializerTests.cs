/* Copyright 2015-present MongoDB Inc.
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
#if NET9_0_OR_GREATER
using System.Collections.ObjectModel;
#endif
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class EnumerableInterfaceImplementerSerializerTests
    {
        private static readonly IBsonSerializer __itemSerializer1;
        private static readonly IBsonSerializer __itemSerializer2;

        static EnumerableInterfaceImplementerSerializerTests()
        {
            __itemSerializer1 = new Int32Serializer(BsonType.Int32);
            __itemSerializer2 = new Int32Serializer(BsonType.String);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);
            var y = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);
            var y = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class EnumerableInterfaceImplementerSerializerGenericTests
    {
        private static readonly IBsonSerializer<int> __itemSerializer1;
        private static readonly IBsonSerializer<int> __itemSerializer2;

        static EnumerableInterfaceImplementerSerializerGenericTests()
        {
            __itemSerializer1 = new Int32Serializer(BsonType.Int32);
            __itemSerializer2 = new Int32Serializer(BsonType.String);
        }

        public class C : IEnumerable<C>
        {
            public int Id;
            public List<C> Children;

            public IEnumerator<C> GetEnumerator()
            {
                return Children.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Fact]
        public void LookupSerializer_should_not_throw_StackOverflowException()
        {
            var serializer = BsonSerializer.LookupSerializer<C>();

            serializer.Should().BeOfType<EnumerableInterfaceImplementerSerializer<C, C>>();
            var itemSerializer = ((EnumerableInterfaceImplementerSerializer<C, C>)serializer).ItemSerializer;
            itemSerializer.Should().BeSameAs(serializer);
        }

        [Fact]
        public void Serialize_should_return_expected_result()
        {
            var subject = CreateSubject();

            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var context = BsonSerializationContext.CreateRoot(jsonWriter);
                var value = new C { Id = 1, Children = new List<C> { new C { Id = 2, Children = new List<C>() } } };

                subject.Serialize(context, value);

                var json = stringWriter.ToString();
                json.Should().Be("[[]]");
            }
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);
            var y = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);
            var y = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        private IBsonSerializer<C> CreateSubject()
        {
            // create subject without using the global serializer registry
            var serializerRegistry = new BsonSerializerRegistry();
            var subject = new EnumerableInterfaceImplementerSerializer<C, C>(serializerRegistry);
            serializerRegistry.RegisterSerializer(typeof(C), subject);
            return subject;
        }

        // exercises the HashSet<TItem> ctor fallback in FinalizeResult: target type with
        // only an ISet<TItem> constructor — same shape as ReadOnlySet<T>
        public class SetOnlyCtorEnumerable<T> : IEnumerable<T>
        {
            private readonly HashSet<T> _items;
            public SetOnlyCtorEnumerable(ISet<T> items) { _items = new HashSet<T>(items); }
            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Fact]
        public void Deserialize_should_use_set_ctor_when_no_other_ctor_matches()
        {
            const string json = "[1, 2, 3, 4]";
            var subject = new EnumerableInterfaceImplementerSerializer<SetOnlyCtorEnumerable<int>, int>(__itemSerializer1);

            using var reader = new JsonReader(json);
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);

            result.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 });
        }

        [Fact]
        public void Deserialize_via_set_ctor_should_collapse_duplicates()
        {
            const string json = "[1, 2, 2, 3]";
            var subject = new EnumerableInterfaceImplementerSerializer<SetOnlyCtorEnumerable<int>, int>(__itemSerializer1);

            using var reader = new JsonReader(json);
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);

            result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        // target type with both an IEnumerable<T> ctor and an ISet<T> ctor — verifies the
        // existing IEnumerable<T> path still wins (otherwise duplicates would silently collapse)
        public class BothCtorsEnumerable<T> : IEnumerable<T>
        {
            private readonly List<T> _items;
            public BothCtorsEnumerable(IEnumerable<T> items) { _items = new List<T>(items); }
            public BothCtorsEnumerable(ISet<T> items) { _items = new List<T>(items); }
            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Fact]
        public void Deserialize_should_prefer_enumerable_ctor_over_set_ctor()
        {
            const string json = "[1, 2, 2, 3]";
            var subject = new EnumerableInterfaceImplementerSerializer<BothCtorsEnumerable<int>, int>(__itemSerializer1);

            using var reader = new JsonReader(json);
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);

            result.Should().Equal(1, 2, 2, 3);
        }

#if NET9_0_OR_GREATER
        public class ReadOnlySetHolder
        {
            public ReadOnlySet<int> X { get; set; }
        }

        [Fact]
        public void ReadOnlySet_should_roundtrip_via_BsonSerializer()
        {
            var original = new ReadOnlySetHolder
            {
                X = new ReadOnlySet<int>(new HashSet<int> { 1, 2, 3, 4 })
            };

            var bson = original.ToBson();
            var rehydrated = BsonSerializer.Deserialize<ReadOnlySetHolder>(bson);

            rehydrated.X.Should().BeEquivalentTo(original.X);
        }
#endif
    }
}
