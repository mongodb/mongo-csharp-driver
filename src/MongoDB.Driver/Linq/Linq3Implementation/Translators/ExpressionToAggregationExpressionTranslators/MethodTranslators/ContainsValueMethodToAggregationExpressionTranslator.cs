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
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IsContainsValueMethod(method))
            {
                var dictionaryExpression = expression.Object;
                var valueExpression = arguments[0];
                return TranslateContainsValue(context, expression, dictionaryExpression, valueExpression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static TranslatedExpression TranslateContainsValue(TranslationContext context, Expression expression, Expression dictionaryExpression, Expression valueExpression)
        {
            var dictionaryTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, dictionaryExpression);
            var dictionarySerializer = GetDictionarySerializer(expression, dictionaryTranslation);
            var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;

            var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
            var valueAst = valueTranslation.Ast;

            var kvpVar = AstExpression.Var("kvp");
            AstExpression ast;
            switch (dictionaryRepresentation)
            {
                case DictionaryRepresentation.ArrayOfArrays:
                    {
                        var valuesArray = AstExpression.Map(
                            input: dictionaryTranslation.Ast,
                            @as: kvpVar,
                            @in: AstExpression.ArrayElemAt(kvpVar, 1));
                        ast = AstExpression.In(valueAst, valuesArray);
                        break;
                    }

                case DictionaryRepresentation.ArrayOfDocuments:
                    {
                        var valuesArray = AstExpression.Map(
                            input: dictionaryTranslation.Ast,
                            @as: kvpVar,
                            @in: AstExpression.GetField(kvpVar, "v"));
                        ast = AstExpression.In(valueAst, valuesArray);
                        break;
                    }

                default:
                    throw new ExpressionNotSupportedException(expression, because: $"Unexpected dictionary representation: {dictionaryRepresentation}");
            }

            return new TranslatedExpression(expression, ast, BooleanSerializer.Instance);
        }

        private static IBsonDictionarySerializer GetDictionarySerializer(Expression expression, TranslatedExpression dictionaryTranslation)
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
