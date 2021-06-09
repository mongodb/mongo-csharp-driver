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
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class CompareToMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IsInstanceCompareToMethod(method))
            {
                var objectExpression = expression.Object;
                var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);
                var otherExpression = arguments[0];
                var otherTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, otherExpression);
                var ast = AstExpression.Cmp(objectTranslation.Ast, otherTranslation.Ast);
                return new AggregationExpression(expression, ast, new Int32Serializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsInstanceCompareToMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return
                !method.IsStatic &&
                method.ReturnParameter.ParameterType == typeof(int) &&
                method.Name == "CompareTo" &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == method.DeclaringType;
        }
    }
}
