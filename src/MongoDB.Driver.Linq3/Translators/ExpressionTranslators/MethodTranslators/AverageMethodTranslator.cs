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
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class AverageMethodTranslator
    {
        private static MethodInfo[] __averageMethods =
        {
            EnumerableMethod.AverageDecimal,
            EnumerableMethod.AverageDecimalWithSelector,
            EnumerableMethod.AverageDouble,
            EnumerableMethod.AverageDoubleWithSelector,
            EnumerableMethod.AverageInt32,
            EnumerableMethod.AverageInt32WithSelector,
            EnumerableMethod.AverageInt64,
            EnumerableMethod.AverageInt64WithSelector,
            EnumerableMethod.AverageNullableDecimal,
            EnumerableMethod.AverageNullableDecimalWithSelector,
            EnumerableMethod.AverageNullableDouble,
            EnumerableMethod.AverageNullableDoubleWithSelector,
            EnumerableMethod.AverageNullableInt32,
            EnumerableMethod.AverageNullableInt32WithSelector,
            EnumerableMethod.AverageNullableInt64,
            EnumerableMethod.AverageNullableInt64WithSelector,
            EnumerableMethod.AverageNullableSingle,
            EnumerableMethod.AverageNullableSingleWithSelector,
            EnumerableMethod.AverageSingle,
            EnumerableMethod.AverageSingleWithSelector
        };

        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__averageMethods))
            {
                var sourceExpression = arguments[0];
                var selectorExpression = arguments.Count == 2 ? (LambdaExpression)arguments[1] : null;

                var sourceTranslation = ExpressionTranslator.Translate(context, sourceExpression);
                var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                AstExpression ast;
                if (selectorExpression == null)
                {
                    ast = new AstUnaryExpression(AstUnaryOperator.Avg, sourceTranslation.Ast);
                }
                else
                {
                    var selectorParameter = selectorExpression.Parameters[0];
                    var selectorSymbol = new Symbol("$" + selectorParameter.Name, sourceItemSerializer);
                    var selectorContext = context.WithSymbol(selectorParameter, selectorSymbol);
                    var selectorTranslation = ExpressionTranslator.Translate(selectorContext, selectorExpression.Body);

                    ast = new AstUnaryExpression(
                        AstUnaryOperator.Avg,
                        AstMapExpression.Create(
                            input: sourceTranslation.Ast,
                            @as: selectorParameter.Name,
                            @in: selectorTranslation.Ast));
                }
                var serverResultType = GetServerResultType(expression.Type);
                if (serverResultType != expression.Type)
                {
                    ast = new AstConvertExpression(ast, expression.Type);
                }
                var serializer = BsonSerializer.LookupSerializer(expression.Type); // TODO: find more specific serializer?

                return new ExpressionTranslation(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static Type GetServerResultType(Type averageResultType)
        {
            if (averageResultType.IsConstructedGenericType && averageResultType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var valueType = averageResultType.GetGenericArguments()[0];
                var serverResultType = GetServerResultType(valueType);
                return typeof(Nullable<>).MakeGenericType(serverResultType);
            }
            else
            {
                switch (averageResultType.FullName)
                {
                    case "System.Decimal":
                        return typeof(decimal);

                    case "System.Single":
                        return typeof(float); // actually double but no conversion needed

                    default:
                        return typeof(double);
                }
            }
        }
    }
}
