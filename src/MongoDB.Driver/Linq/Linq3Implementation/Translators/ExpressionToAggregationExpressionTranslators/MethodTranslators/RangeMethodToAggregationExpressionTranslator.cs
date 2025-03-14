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

using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class RangeMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.Range))
            {
                var startExpression = arguments[0];
                var startTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, startExpression);
                SerializationHelper.EnsureRepresentationIsNumeric(expression, startExpression, startTranslation);
                var countExpression = arguments[1];
                var countTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, countExpression);
                SerializationHelper.EnsureRepresentationIsNumeric(expression, countExpression, countTranslation);

                var (startVar, startAst) = AstExpression.UseVarIfNotSimple("start", startTranslation.Ast);
                var (countVar, countAst) = AstExpression.UseVarIfNotSimple("count", countTranslation.Ast);

                var ast = AstExpression.Let(
                    startVar,
                    countVar,
                    AstExpression.Range(startAst, end: AstExpression.Add(startAst, countAst)));
                var serializer = IEnumerableSerializer.Create(new Int32Serializer());
                return new TranslatedExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
