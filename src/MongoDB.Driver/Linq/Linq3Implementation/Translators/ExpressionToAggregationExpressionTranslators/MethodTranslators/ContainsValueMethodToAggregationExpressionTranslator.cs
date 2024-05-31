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

using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class ContainsValueMethodToAggregationExpressionTranslator
    {
        // public methods
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IsContainsValueMethod(method))
            {
                var dictionaryExpression = expression.Object;
                var valueExpression = arguments[0];

                var dictionaryTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, dictionaryExpression);
                var dictionarySerializer = GetDictionarySerializer(expression, dictionaryTranslation);
                var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;

                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                var (valueBinding, valueAst) = AstExpression.UseVarIfNotSimple("value", valueTranslation.Ast);

                AstExpression ast;
                switch (dictionaryRepresentation)
                {
                    case DictionaryRepresentation.Document:
                        ast = AstExpression.Let(
                            var: valueBinding,
                            @in: AstExpression.Reduce(
                                input: AstExpression.ObjectToArray(dictionaryTranslation.Ast),
                                initialValue: false,
                                @in: AstExpression.Cond(
                                    @if: AstExpression.Var("value"),
                                    @then: true,
                                    @else: AstExpression.Eq(AstExpression.GetField(AstExpression.Var("this"), "v"), valueAst))));
                        break;

                    case DictionaryRepresentation.ArrayOfArrays:
                        ast = AstExpression.Let(
                            var: valueBinding,
                            @in: AstExpression.Reduce(
                                input: dictionaryTranslation.Ast,
                                initialValue: false,
                                @in: AstExpression.Cond(
                                    @if: AstExpression.Var("value"),
                                    @then: true,
                                    @else: AstExpression.Eq(AstExpression.ArrayElemAt(AstExpression.Var("this"), 1), valueAst))));
                        break;

                    case DictionaryRepresentation.ArrayOfDocuments:
                        ast = AstExpression.Let(
                            var: valueBinding,
                            @in: AstExpression.Reduce(
                                input: dictionaryTranslation.Ast,
                                initialValue: false,
                                @in: AstExpression.Cond(
                                    @if: AstExpression.Var("value"),
                                    @then: true,
                                    @else: AstExpression.Eq(AstExpression.GetField(AstExpression.Var("this"), "v"), valueAst))));
                        break;

                    default:
                        throw new ExpressionNotSupportedException(expression, because: $"ContainsValue is not supported when DictionaryRepresentation is: {dictionaryRepresentation}");
                }

                return new AggregationExpression(expression, ast, BooleanSerializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static IBsonDictionarySerializer GetDictionarySerializer(Expression expression, AggregationExpression dictionaryTranslation)
        {
            if (dictionaryTranslation.Serializer is IBsonDictionarySerializer dictionarySerializer)
            {
                return dictionarySerializer;
            }

            throw new ExpressionNotSupportedException(expression, because: $"class {dictionaryTranslation.Serializer.GetType().FullName} does not implement the IBsonDictionarySerializer interface");
        }

        private static bool IsContainsValueMethod(MethodInfo method)
        {
            return
                !method.IsStatic &&
                method.IsPublic &&
                method.ReturnType == typeof(bool) &&
                method.Name == "ContainsValue" &&
                method.GetParameters() is var parameters &&
                parameters.Length == 1;
        }
    }
}
