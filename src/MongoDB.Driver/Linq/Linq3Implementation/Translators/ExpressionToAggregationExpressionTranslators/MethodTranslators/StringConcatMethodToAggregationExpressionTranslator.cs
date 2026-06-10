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
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class StringConcatMethodToAggregationExpressionTranslator
    {
        public static bool CanTranslate(BinaryExpression expression, out MethodInfo method, out ReadOnlyCollection<Expression> arguments)
        {
            if (expression.NodeType == ExpressionType.Add &&
                expression.Method != null &&
                expression.Method.IsOneOf(StringMethod.ConcatWith2Objects, StringMethod.ConcatWith2Strings))
            {
                method = expression.Method;
                arguments = new ReadOnlyCollection<Expression>(new[] { expression.Left, expression.Right });
                return true;
            }

            method = null;
            arguments = null;
            return false;
        }

        public static bool CanTranslate(MethodCallExpression expression, out MethodInfo method, out ReadOnlyCollection<Expression> arguments)
        {
            if (expression.Method.IsOneOf(StringMethod.ConcatOverloads))
            {
                method = expression.Method;
                arguments = expression.Arguments;
                return true;
            }

            method = null;
            arguments = null;
            return false;
        }

        public static TranslatedExpression Translate(TranslationContext context, Expression expression, MethodInfo method, ReadOnlyCollection<Expression> arguments)
        {
            IEnumerable<AstExpression> argumentsTranslations = null;

            if (method.IsOneOf(
                StringMethod.ConcatWith2Strings,
                StringMethod.ConcatWith3Strings,
                StringMethod.ConcatWith4Strings))
            {
                argumentsTranslations =
                    arguments.Select(a => ExpressionToAggregationExpressionTranslator.Translate(context, a).Ast);
            }

            if (method.IsOneOf(
               StringMethod.ConcatWith1Object,
               StringMethod.ConcatWith2Objects,
               StringMethod.ConcatWith3Objects))
            {
                argumentsTranslations = arguments
                    .Select(a => ExpressionToAggregationExpressionTranslator.Translate(context, a))
                    .Select(ExpressionToString);
            }

            if (method.Is(StringMethod.ConcatWithStringArray))
            {
                var argumentTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, arguments.Single());
                if (argumentTranslation.Ast is AstComputedArrayExpression astArray)
                {
                    argumentsTranslations = astArray.Items;
                }
            }

            if (method.Is(StringMethod.ConcatWithObjectArray))
            {
                if (arguments.Single() is NewArrayExpression newArrayExpression)
                {
                    argumentsTranslations = newArrayExpression.Expressions
                        .Select(a => ExpressionToAggregationExpressionTranslator.Translate(context, a))
                        .Select(ExpressionToString);
                }
            }

            if (argumentsTranslations != null)
            {
                var ast = AstExpression.Concat(argumentsTranslations.ToArray());
                return new TranslatedExpression(expression, ast, StringSerializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);

            static AstExpression ExpressionToString(TranslatedExpression translatedExpression)
            {
                var astExpression = translatedExpression.Ast;
                if (translatedExpression.Serializer.ValueType == typeof(string))
                {
                    return astExpression;
                }
                else
                {
                    if (astExpression.IsConstant(out var constant))
                    {
                        var stringConstant = ValueToString(translatedExpression.Expression, constant);
                        return AstExpression.Constant(stringConstant);
                    }
                    else
                    {
                        return AstExpression.ToString(astExpression);
                    }
                }
            }

            static string ValueToString(Expression expression, BsonValue value)
            {
                return value switch
                {
                    BsonBoolean booleanValue => JsonConvert.ToString(booleanValue.Value),
                    BsonDateTime dateTimeValue => JsonConvert.ToString(dateTimeValue.ToUniversalTime()),
                    BsonDecimal128 decimalValue => JsonConvert.ToString(decimalValue.Value),
                    BsonDouble doubleValue => JsonConvert.ToString(doubleValue.Value),
                    BsonInt32 int32Value => JsonConvert.ToString(int32Value.Value),
                    BsonInt64 int64Value => JsonConvert.ToString(int64Value.Value),
                    BsonObjectId objectIdValue => objectIdValue.Value.ToString(),
                    BsonString stringValue => stringValue.Value,
                    _ => throw new ExpressionNotSupportedException(expression, because: $"values represented as BSON type {value.BsonType} are not supported by $toString")
                };
            }
        }
    }
}
