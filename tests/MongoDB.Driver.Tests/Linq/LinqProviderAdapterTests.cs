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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public class LinqProviderAdapterTests
    {
        [Fact]
        public void AsQueryable_should_return_expected_result()
        {
            var collection = Mock.Of<IMongoCollection<C>>();
            var session = Mock.Of<IClientSessionHandle>();
            var options = new AggregateOptions();

            var result = LinqProviderAdapter.AsQueryable(collection, session, options);

            var queryable = result.Should().BeOfType<MongoQuery<C, C>>().Subject;
            var provider = queryable.Provider.Should().BeOfType<MongoQueryProvider<C>>().Subject;
            provider._collection().Should().BeSameAs(collection);
            provider._options().Should().BeSameAs(options);
        }

        [Fact]
        public void TranslateExpressionToAggregateExpression_should_return_expected_result()
        {
            Expression<Func<C, int>> expression = c => c.X;
            var serializationDomain = BsonSerializer.DefaultSerializationDomain;
            var sourceSerializer = serializationDomain.SerializerRegistry.GetSerializer<C>();

            var result = LinqProviderAdapter.TranslateExpressionToAggregateExpression(expression, sourceSerializer, serializationDomain, translationOptions: null);

            result.Should().Be("'$X'");
        }

        [Fact]
        public void TranslateExpressionToField_with_untyped_lambda_should_return_expected_result()
        {
            LambdaExpression expression = (Expression<Func<C, int>>)(c => c.X);
            var serializationDomain = BsonSerializer.DefaultSerializationDomain;
            var documentSerializer = serializationDomain.SerializerRegistry.GetSerializer<C>();

            var result = LinqProviderAdapter.TranslateExpressionToField(expression, documentSerializer, serializationDomain, translationOptions: null);

            result.FieldName.Should().Be("X");
            result.FieldSerializer.Should().BeOfType(typeof(Int32Serializer));
        }

        [Fact]
        public void TranslateExpressionToField_with_typed_lambda_should_return_expected_result()
        {
            Expression<Func<C, int>> expression = c => c.X;
            var serializationDomain = BsonSerializer.DefaultSerializationDomain;
            var documentSerializer = serializationDomain.SerializerRegistry.GetSerializer<C>();

            var result = LinqProviderAdapter.TranslateExpressionToField(expression, documentSerializer, serializationDomain, translationOptions: null, allowScalarValueForArrayField: false);

            result.FieldName.Should().Be("X");
            result.FieldSerializer.Should().BeOfType(typeof(Int32Serializer));
        }

        [Fact]
        public void TranslateExpressionToFilter_should_return_expected_result()
        {
            Expression<Func<C, bool>> expression = c => c.X == 0;
            var serializationDomain = BsonSerializer.DefaultSerializationDomain;
            var documentSerializer = serializationDomain.SerializerRegistry.GetSerializer<C>();

            var result = LinqProviderAdapter.TranslateExpressionToFilter(expression, documentSerializer, serializationDomain, translationOptions: null);

            result.Should().Be("{ X : 0 }");
        }

        [Fact]
        public void TranslateExpressionToFindProjection_should_return_expected_result()
        {
            Expression<Func<C, int>> expression = c => c.X;
            var serializationDomain = BsonSerializer.DefaultSerializationDomain;
            var documentSerializer = serializationDomain.SerializerRegistry.GetSerializer<C>();

            var result = LinqProviderAdapter.TranslateExpressionToFindProjection(expression, documentSerializer, serializationDomain, translationOptions: null);

            result.Document.Should().Be("{ X : 1, _id : 0 }");
            result.ProjectionSerializer.ValueType.Should().Be(typeof(int));
        }

        [Fact]
        public void TranslateExpressionToProjection_should_return_expected_result()
        {
            WithAnonymousOutputType(c => new { id = c.Id, x = c.X });

            void WithAnonymousOutputType<TOutput>(Expression<Func<C, TOutput>> expression)
            {
                var serializationDomain = BsonSerializer.DefaultSerializationDomain;
                var inputSerializer = serializationDomain.SerializerRegistry.GetSerializer<C>();

                var result = LinqProviderAdapter.TranslateExpressionToProjection(expression, inputSerializer, serializationDomain, translationOptions: null);

                result.Document.Should().Be("{ _id : '$_id', x : '$X' }");
                result.ProjectionSerializer.ValueType.Should().Be(typeof(TOutput));
            }
        }

        // nested types
        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
