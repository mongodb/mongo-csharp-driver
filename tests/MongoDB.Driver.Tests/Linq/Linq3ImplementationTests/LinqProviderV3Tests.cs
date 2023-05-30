﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests
{
    public class LinqProviderV3Tests
    {
        [Fact]
        public void AsQueryable_should_return_expected_result()
        {
            var subject = LinqProviderAdapter.V3;
            var collection = Mock.Of<IMongoCollection<C>>();
            var session = Mock.Of<IClientSessionHandle>();
            var options = new AggregateOptions();

            var result = subject.AsQueryable(collection, session, options);

            var queryable = result.Should().BeOfType<MongoQuery<C, C>>().Subject;
            var provider = queryable.Provider.Should().BeOfType<MongoQueryProvider<C>>().Subject;
            provider._collection().Should().BeSameAs(collection);
            provider._options().Should().BeSameAs(options);
        }

        [Fact]
        public void TranslateExpressionToAggregateExpression_should_return_expected_result()
        {
            var subject = LinqProviderAdapter.V3;
            Expression<Func<C, int>> expression = c => c.X;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var sourceSerializer = serializerRegistry.GetSerializer<C>();
            var translationOptions = new ExpressionTranslationOptions();

            var result = subject.TranslateExpressionToAggregateExpression(expression, sourceSerializer, serializerRegistry, translationOptions);

            var expectedResult = LinqProviderAdapter.V2.TranslateExpressionToAggregateExpression(expression, sourceSerializer, serializerRegistry, translationOptions);
            expectedResult.Should().Be("'$X'");
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void TranslateExpressionToBucketOutputProjection_should_throw()
        {
            var subject = LinqProviderAdapter.V3;
            Expression<Func<C, int>> valueExpression = c => c.X;
            Expression<Func<IGrouping<int, C>, int>> outputExpression = g => g.Count();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<C>();
            var translationOptions = new ExpressionTranslationOptions();

            var exception = Record.Exception(() => subject.TranslateExpressionToBucketOutputProjection(valueExpression, outputExpression, documentSerializer, serializerRegistry, translationOptions));

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void TranslateExpressionToField_with_untyped_lambda_should_return_expected_result()
        {
            var subject = LinqProviderAdapter.V3;
            LambdaExpression expression = (Expression<Func<C, int>>)(c => c.X);
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<C>();

            var result = subject.TranslateExpressionToField(expression, documentSerializer, serializerRegistry);

            var expectedResult = LinqProviderAdapter.V2.TranslateExpressionToField(expression, documentSerializer, serializerRegistry);
            expectedResult.FieldName.Should().Be("X");
            expectedResult.FieldSerializer.Should().BeOfType<Int32Serializer>();
            result.FieldName.Should().Be(expectedResult.FieldName);
            result.FieldSerializer.Should().BeOfType(expectedResult.FieldSerializer.GetType());
        }

        [Fact]
        public void TranslateExpressionToField_with_typed_lambda_should_return_expected_result()
        {
            var subject = LinqProviderAdapter.V3;
            Expression<Func<C, int>> expression = c => c.X;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<C>();

            var result = subject.TranslateExpressionToField(expression, documentSerializer, serializerRegistry, allowScalarValueForArrayField: false);

            var expectedResult = LinqProviderAdapter.V2.TranslateExpressionToField(expression, documentSerializer, serializerRegistry, allowScalarValueForArrayField: false);
            expectedResult.FieldName.Should().Be("X");
            expectedResult.FieldSerializer.Should().BeOfType<Int32Serializer>();
            result.FieldName.Should().Be(expectedResult.FieldName);
            result.FieldSerializer.Should().BeOfType(expectedResult.FieldSerializer.GetType());
        }

        [Fact]
        public void TranslateExpressionToFilter_should_return_expected_result()
        {
            var subject = LinqProviderAdapter.V3;
            Expression<Func<C, bool>> expression = c => c.X == 0;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<C>();

            var result = subject.TranslateExpressionToFilter(expression, documentSerializer, serializerRegistry);

            var expectedResult = LinqProviderAdapter.V2.TranslateExpressionToFilter(expression, documentSerializer, serializerRegistry);
            expectedResult.Should().Be("{ X : 0 }");
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void TranslateExpressionToFindProjection_should_return_expected_result()
        {
            var subject = LinqProviderAdapter.V3;
            Expression<Func<C, int>> expression = c => c.X;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<C>();

            var result = subject.TranslateExpressionToFindProjection(expression, documentSerializer, serializerRegistry);

            result.Document.Should().Be("{ _v : '$X', _id : 0 }");
            result.ProjectionSerializer.ValueType.Should().Be(typeof(int));
        }

        [Fact]
        public void TranslateExpressionToGroupProjection_should_throw()
        {
            WithAnonymousOutputType(g => new { count = g.Count() });

            void WithAnonymousOutputType<TOutput>(Expression<Func<IGrouping<int, C>, TOutput>> groupExpression)
            {
                var subject = LinqProviderAdapter.V3;
                Expression<Func<C, int>> idExpression = c => c.X;
                var serializerRegistry = BsonSerializer.SerializerRegistry;
                var documentSerializer = serializerRegistry.GetSerializer<C>();
                var translationOptions = new ExpressionTranslationOptions();

                var exception = Record.Exception(() => subject.TranslateExpressionToGroupProjection(idExpression, groupExpression, documentSerializer, serializerRegistry, translationOptions));

                exception.Should().BeOfType<InvalidOperationException>();
            }
        }

        [Fact]
        public void TranslateExpressionToProjection_should_return_expected_result()
        {
            WithAnonymousOutputType(c => new { id = c.Id, x = c.X });

            void WithAnonymousOutputType<TOutput>(Expression<Func<C, TOutput>> expression)
            {
                var subject = LinqProviderAdapter.V3;
                var serializerRegistry = BsonSerializer.SerializerRegistry;
                var inputSerializer = serializerRegistry.GetSerializer<C>();
                var translationOptions = new ExpressionTranslationOptions();

                var result = subject.TranslateExpressionToProjection(expression, inputSerializer, serializerRegistry, translationOptions);

                var expectedResult = LinqProviderAdapter.V2.TranslateExpressionToProjection(expression, inputSerializer, serializerRegistry, translationOptions);
                expectedResult.Document.Should().Be("{ id : '$_id', x : '$X', _id : 0 }");
                expectedResult.ProjectionSerializer.ValueType.Should().Be(typeof(TOutput));
                result.Document.Should().Be("{ _id : '$_id', x : '$X' }");
                result.ProjectionSerializer.ValueType.Should().Be(expectedResult.ProjectionSerializer.ValueType);
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
