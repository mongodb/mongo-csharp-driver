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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

internal static class EncStrMethodToAggregationExpressionTranslator
{
    public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
    {
        var method = expression.Method;
        var arguments = expression.Arguments;

        if (method.IsOneOf(MqlMethod.EncStrMethodOverloads))
        {
            var inputTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]);
            var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[1]);
            EnsureSerializedAsString(inputTranslation, arguments[0], expression);
            EnsureSerializedAsString(valueTranslation, arguments[1], expression);

            var @operator = method.Name switch
            {
                "EncStrContains" => AstEncStrOperator.Contains,
                "EncStrEndsWith" => AstEncStrOperator.EndsWith,
                "EncStrNormalizedEq" => AstEncStrOperator.NormalizedEq,
                "EncStrStartsWith" => AstEncStrOperator.StartsWith,
                _ => throw new InvalidOperationException($"Unexpected method: {method.Name}")
            };

            var ast = AstExpression.EncStrExpression(@operator, inputTranslation.Ast, valueTranslation.Ast);
            return new TranslatedExpression(expression, ast, BooleanSerializer.Instance);
        }

        throw new ExpressionNotSupportedException(expression);
    }

    private static void EnsureSerializedAsString(TranslatedExpression translation, Expression argument, Expression containingExpression)
    {
        if (translation.Serializer is IRepresentationConfigurable representationConfigurable &&
            representationConfigurable.Representation != BsonType.String)
        {
            throw new ExpressionNotSupportedException(argument, containingExpression, because: "it is not serialized as a string");
        }
    }
}
