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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers.KnownSerializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Serializers.KnownSerializers
{
    public class KnownSerializerFinderTests
    {
        private enum E { A, B }

        private class C
        {
            public int P { get; set; }
            [BsonRepresentation(BsonType.Int32)]
            public E Ei { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E Es { get; set; }
            public A A { get; set; }
        }

        private class A
        {
            public B B { get; set; }
        }

        private class B { }

        private class View
        {
            public A A { get; set; }
        }

        [Fact]
        public void Identity_expression_should_return_collection_serializer()
        {
            Expression<Func<C, C>> expression = x => x;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var serializer = result.GetSerializer(expression.Body);
            serializer.Should().Be(collectionSerializer);
        }

        [Fact]
        public void Int_property_expression_should_return_int_serializer()
        {
            Expression<Func<C, int>> expression = x => x.P;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var serializer = result.GetSerializer(expression.Body);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.P), out var expectedPropertySerializationInfo).Should().BeTrue();
            serializer.Should().Be(expectedPropertySerializationInfo.Serializer);
        }

        [Fact]
        public void Enum_property_expression_should_return_enum_serializer_with_int_representation()
        {
            Expression<Func<C, E>> expression = x => x.Ei;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var serializer = result.GetSerializer(expression.Body);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.Ei), out var expectedPropertySerializationInfo).Should().BeTrue();
            serializer.Should().Be(expectedPropertySerializationInfo.Serializer);
        }

        [Fact]
        public void Enum_comparison_expression_should_return_enum_serializer_with_int_representation()
        {
            Expression<Func<C, bool>> expression = x => x.Ei == E.A;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var equalsExpression = (BinaryExpression)expression.Body;
            var serializer = result.GetSerializer(equalsExpression.Right);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.Ei), out var expectedPropertySerializationInfo).Should().BeTrue();
            serializer.Should().Be(expectedPropertySerializationInfo.Serializer);
        }

        [Fact]
        public void Enum_property_expression_should_return_enum_serializer_with_string_representation()
        {
            Expression<Func<C, E>> expression = x => x.Es;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var serializer = result.GetSerializer(expression.Body);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.Es), out var expectedPropertySerializationInfo).Should().BeTrue();
            serializer.Should().Be(expectedPropertySerializationInfo.Serializer);
        }

        [Fact]
        public void Enum_comparison_expression_should_return_enum_serializer_with_string_representation()
        {
            Expression<Func<C, bool>> expression = x => x.Es == E.A;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var equalsExpression = (BinaryExpression)expression.Body;
            var serializer = result.GetSerializer(equalsExpression.Right);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.Es), out var expectedPropertySerializationInfo).Should().BeTrue();
            serializer.Should().Be(expectedPropertySerializationInfo.Serializer);
        }

        [Fact]
        public void Conditional_expression_should_return_enum_serializer_with_int_representation()
        {
            Expression<Func<C, E>> expression = x => x.Ei == E.A ? E.B : x.Ei;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var conditionalExpression = (ConditionalExpression)expression.Body;
            var serializer = result.GetSerializer(conditionalExpression.IfTrue);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.Ei), out var expectedPropertySerializationInfo).Should().BeTrue();
            serializer.Should().Be(expectedPropertySerializationInfo.Serializer);
        }

        [Fact]
        public void Conditional_expression_should_return_enum_serializer_with_string_representation()
        {
            Expression<Func<C, E>> expression = x => x.Es == E.A ? E.B : x.Es;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var conditionalExpression = (ConditionalExpression)expression.Body;
            var serializer = result.GetSerializer(conditionalExpression.IfTrue);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.Es), out var expectedPropertySerializationInfo).Should().BeTrue();
            serializer.Should().Be(expectedPropertySerializationInfo.Serializer);
        }

        [Fact]
        public void Conditional_expression_with_different_enum_representations_should_throw()
        {
            Expression<Func<C, E>> expression = x => x.Ei == E.A ? E.B : x.Es;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var conditionalExpression = (ConditionalExpression)expression.Body;
            Assert.Throws<InvalidOperationException>(() => result.GetSerializer(conditionalExpression.IfTrue));
        }

        [Fact]
        public void Property_chain_should_return_correct_nested_serializer()
        {
            Expression<Func<C, B>> expression = x => x.A.B;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var serializer = result.GetSerializer(expression.Body);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.A), out var aSerializationInfo).Should().BeTrue();
            ((BsonClassMapSerializer<A>)aSerializationInfo.Serializer).TryGetMemberSerializationInfo(nameof(A.B), out var bSerializationInfo).Should().BeTrue();
            serializer.Should().Be(bSerializationInfo.Serializer);
        }

        [Fact]
        public void Two_property_chains_should_each_return_correct_nested_serializer()
        {
            Expression<Func<C, E>> expression = x => x.A.B == null ? x.Ei : x.Es;
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var conditionalExpression = (ConditionalExpression)expression.Body;
            var trueBranchSerializer = result.GetSerializer(conditionalExpression.IfTrue);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.Ei), out var eiSerializationInfo).Should().BeTrue();
            trueBranchSerializer.Should().Be(eiSerializationInfo.Serializer);
            var falseBranchSerializer = result.GetSerializer(conditionalExpression.IfFalse);
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.Es), out var esSerializationInfo).Should().BeTrue();
            falseBranchSerializer.Should().Be(esSerializationInfo.Serializer);
        }

        [Fact]
        public void Projection_into_new_type_should_return_correct_serializer()
        {
            Expression<Func<C, View>> expression = x => new View { A = x.A };
            var collectionSerializer = GetCollectionSerializer();

            var result = KnownSerializerFinder.FindKnownSerializers(expression, collectionSerializer);

            var serializer = result.GetSerializer(expression.Body);
            serializer.Should().BeOfType<BsonClassMapSerializer<View>>();
            collectionSerializer.TryGetMemberSerializationInfo(nameof(C.A), out var caSerializationInfo).Should().BeTrue();
            ((BsonClassMapSerializer<View>)serializer).TryGetMemberSerializationInfo(nameof(View.A), out var viewaSerializationInfo).Should().BeTrue();
            viewaSerializationInfo.Serializer.Should().Be(caSerializationInfo.Serializer);
        }

        private IBsonDocumentSerializer GetCollectionSerializer()
        {
            return (IBsonDocumentSerializer)BsonSerializer.LookupSerializer<C>();
        }
    }
}
