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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators;
using MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators;

public class ComparisonExpressionToFilterTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedComparisonTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedTranslation)
    {
        var translationContext = CreateTranslationContext(expression);
        var resultAst = ComparisonExpressionToFilterTranslator.Translate(translationContext, (BinaryExpression)expression.Body);

        resultAst.Render().Should().Be(BsonDocument.Parse(expectedTranslation));
    }

    // TODO: Probably need to be merged with CSharp2422Tests and CSharp4401Tests.
    public static IEnumerable<object[]> SupportedComparisonTestCases =
    [
        // Comparison to constant test cases: eq
        CreateTestCase<MyModel>(CreateLambda<MyModel, int>(x => x.Int), Expression.Constant(5), ExpressionType.Equal, "{ Int : { $eq : 5 } }"),
        CreateTestCase<MyModel>(CreateLambda<MyModel, int?>(x => x.NullableInt), Expression.Constant(5, typeof(int?)), ExpressionType.Equal, "{ NullableInt : { $eq : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5), CreateLambda<MyModel, int>(x => x.Int), ExpressionType.Equal, "{ Int : { $eq : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5, typeof(int?)), CreateLambda<MyModel, int?>(x => x.NullableInt), ExpressionType.Equal, "{ NullableInt : { $eq : 5 } }"),

        // Comparison to constant test cases: gt
        CreateTestCase<MyModel>(CreateLambda<MyModel, int>(x => x.Int), Expression.Constant(5), ExpressionType.GreaterThan, "{ Int : { $gt : 5 } }"),
        CreateTestCase<MyModel>(CreateLambda<MyModel, int?>(x => x.NullableInt), Expression.Constant(5, typeof(int?)), ExpressionType.GreaterThan, "{ NullableInt : { $gt : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5), CreateLambda<MyModel, int>(x => x.Int), ExpressionType.GreaterThan, "{ Int : { $lt : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5, typeof(int?)), CreateLambda<MyModel, int?>(x => x.NullableInt), ExpressionType.GreaterThan, "{ NullableInt : { $lt : 5 } }"),

        // Comparison to constant test cases: lt
        CreateTestCase<MyModel>(CreateLambda<MyModel, int>(x => x.Int), Expression.Constant(5), ExpressionType.LessThan, "{ Int : { $lt : 5 } }"),
        CreateTestCase<MyModel>(CreateLambda<MyModel, int?>(x => x.NullableInt), Expression.Constant(5, typeof(int?)), ExpressionType.LessThan, "{ NullableInt : { $lt : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5), CreateLambda<MyModel, int>(x => x.Int), ExpressionType.LessThan, "{ Int : { $gt : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5, typeof(int?)), CreateLambda<MyModel, int?>(x => x.NullableInt), ExpressionType.LessThan, "{ NullableInt : { $gt : 5 } }"),

        // Comparison to constant test cases: gte
        CreateTestCase<MyModel>(CreateLambda<MyModel, int>(x => x.Int), Expression.Constant(5), ExpressionType.GreaterThanOrEqual, "{ Int : { $gte : 5 } }"),
        CreateTestCase<MyModel>(CreateLambda<MyModel, int?>(x => x.NullableInt), Expression.Constant(5, typeof(int?)), ExpressionType.GreaterThanOrEqual, "{ NullableInt : { $gte : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5), CreateLambda<MyModel, int>(x => x.Int), ExpressionType.GreaterThanOrEqual, "{ Int : { $lte : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5, typeof(int?)), CreateLambda<MyModel, int?>(x => x.NullableInt), ExpressionType.GreaterThanOrEqual, "{ NullableInt : { $lte : 5 } }"),

        // Comparison to constant test cases: lte
        CreateTestCase<MyModel>(CreateLambda<MyModel, int>(x => x.Int), Expression.Constant(5), ExpressionType.LessThanOrEqual, "{ Int : { $lte : 5 } }"),
        CreateTestCase<MyModel>(CreateLambda<MyModel, int?>(x => x.NullableInt), Expression.Constant(5, typeof(int?)), ExpressionType.LessThanOrEqual, "{ NullableInt : { $lte : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5), CreateLambda<MyModel, int>(x => x.Int), ExpressionType.LessThanOrEqual, "{ Int : { $gte : 5 } }"),
        CreateTestCase<MyModel>(Expression.Constant(5, typeof(int?)), CreateLambda<MyModel, int?>(x => x.NullableInt), ExpressionType.LessThanOrEqual, "{ NullableInt : { $gte : 5 } }"),
    ];

    private static LambdaExpression CreateLambda<TModel, TField>(Expression<Func<TModel, TField>> expression) => expression;

    private static object[] CreateTestCase<TModel>(
        Expression left,
        Expression right,
        ExpressionType expressionType,
        string expectedTranslation)
    {
        var modelParameter = Expression.Parameter(typeof(TModel));
        var leftExpression = ReplaceParameters(left, modelParameter);
        var rightExpression = ReplaceParameters(right, modelParameter);

        var comparisonExpression = expressionType switch
        {
            ExpressionType.Equal => Expression.Equal(leftExpression, rightExpression),
            ExpressionType.GreaterThan => Expression.GreaterThan(leftExpression, rightExpression),
            ExpressionType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(leftExpression, rightExpression),
            ExpressionType.LessThan => Expression.LessThan(leftExpression, rightExpression),
            ExpressionType.LessThanOrEqual => Expression.LessThanOrEqual(leftExpression, rightExpression),
            _ => throw new ArgumentOutOfRangeException(),
        };

        return [Expression.Lambda(comparisonExpression, modelParameter), expectedTranslation];

        static Expression ReplaceParameters(Expression expression, ParameterExpression newParameter)
        {
            if (expression is not LambdaExpression lambda)
            {
                return expression;
            }

            var replaceParametersVisitor = new ExpressionCombiner.ParameterUpdateVisitor(lambda.Parameters.Single(), newParameter);
            return replaceParametersVisitor.Visit(lambda.Body);
        }
    }

    private static TranslationContext CreateTranslationContext(LambdaExpression expression)
    {
        var modelParameter = expression.Parameters.Single();
        var modelSerializer = BsonSerializer.LookupSerializer(modelParameter.Type);
        var nodeSerializers = new SerializerMap();
        nodeSerializers.AddSerializer(modelParameter, modelSerializer);

        SerializerFinder.FindSerializers(expression, null, nodeSerializers);
        var context = TranslationContext.Create(null, nodeSerializers);
        var symbol = context.CreateRootSymbol(modelParameter, modelSerializer);
        return context.WithSymbol(symbol);
    }

    private class MyModel
    {
        public int Int {get; set; }

        public int? NullableInt {get; set; }
    }
}
