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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class AbsMethodToAggregationExpressionTranslator
    {
        private static MethodInfo[] __absMethods =
        {
            MathMethod.AbsDecimal,
            MathMethod.AbsDouble,
            MathMethod.AbsInt32,
            MathMethod.AbsInt64
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__absMethods))
            {
                var valueExpression = arguments[0];
                var valueExpressionWithConvertsRemoved = ConvertHelper.RemoveUnnecessaryConvert(valueExpression, impliedType: expression.Type);
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpressionWithConvertsRemoved);
                var ast = AstExpression.Abs(valueTranslation.Ast);
                return new AggregationExpression(expression, ast, valueTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
