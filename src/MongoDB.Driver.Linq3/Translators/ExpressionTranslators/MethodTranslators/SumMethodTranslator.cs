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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class SumMethodTranslator
    {
        private static MethodInfo[] __sumMethods =
        {
            EnumerableMethod.SumDecimal,
            EnumerableMethod.SumDecimalWithSelector,
            EnumerableMethod.SumDouble,
            EnumerableMethod.SumDoubleWithSelector,
            EnumerableMethod.SumInt32,
            EnumerableMethod.SumInt32WithSelector,
            EnumerableMethod.SumInt64,
            EnumerableMethod.SumInt64WithSelector,
            EnumerableMethod.SumNullableDecimal,
            EnumerableMethod.SumNullableDecimalWithSelector,
            EnumerableMethod.SumNullableDouble,
            EnumerableMethod.SumNullableDoubleWithSelector,
            EnumerableMethod.SumNullableInt32,
            EnumerableMethod.SumNullableInt32WithSelector,
            EnumerableMethod.SumNullableInt64,
            EnumerableMethod.SumNullableInt64WithSelector,
            EnumerableMethod.SumNullableSingle,
            EnumerableMethod.SumNullableSingleWithSelector,
            EnumerableMethod.SumSingle,
            EnumerableMethod.SumSingleWithSelector
        };

        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__sumMethods))
            {
                var sourceExpression = arguments[0];
                var selectorExpression = arguments.Count == 2 ? (LambdaExpression)arguments[1] : null;

                var sourceTranslation = ExpressionTranslator.Translate(context, sourceExpression);
                var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                AstExpression ast;
                IBsonSerializer serializer;
                if (selectorExpression == null)
                {
                    ast = new AstUnaryExpression(AstUnaryOperator.Sum, sourceTranslation.Ast);
                    serializer = sourceItemSerializer;
                }
                else
                {
                    var selectorParameter = selectorExpression.Parameters[0];
                    var selectorSymbol = new Symbol("$" + selectorParameter.Name, sourceItemSerializer);
                    var selectorContext = context.WithSymbol(selectorParameter, selectorSymbol);
                    var selectorTranslation = ExpressionTranslator.Translate(selectorContext, selectorExpression.Body);

                    ast = new AstUnaryExpression(
                        AstUnaryOperator.Sum,
                        AstMapExpression.Create(
                            input: sourceTranslation.Ast,
                            @as: selectorParameter.Name,
                            @in: selectorTranslation.Ast));
                    serializer = BsonSerializer.LookupSerializer(expression.Type); // TODO: find more specific serializer?
                }
                var serverResultType = GetServerResultType(expression.Type);
                if (serverResultType != expression.Type)
                {
                    ast = new AstConvertExpression(ast, expression.Type);
                }

                return new ExpressionTranslation(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static Type GetServerResultType(Type sumResultType)
        {
            if (sumResultType.IsConstructedGenericType && sumResultType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var valueType = sumResultType.GetGenericArguments()[0];
                var serverResultType = GetServerResultType(valueType);
                return typeof(Nullable<>).MakeGenericType(serverResultType);
            }
            else
            {
                switch (sumResultType.FullName)
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
