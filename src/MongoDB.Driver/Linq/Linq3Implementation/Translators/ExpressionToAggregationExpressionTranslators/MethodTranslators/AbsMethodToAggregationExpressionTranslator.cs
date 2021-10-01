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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class AbsMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __absMethods =
        {
            MathMethod.AbsDecimal,
            MathMethod.AbsDouble,
            MathMethod.AbsInt16,
            MathMethod.AbsInt32,
            MathMethod.AbsInt64,
            MathMethod.AbsSByte,
            MathMethod.AbsSingle
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__absMethods))
            {
                var valueExpression = ConvertHelper.RemoveWideningConvert(arguments[0]);
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                var ast = AstExpression.Abs(valueTranslation.Ast);
                return new AggregationExpression(expression, ast, valueTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
