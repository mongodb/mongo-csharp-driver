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
    internal static class RoundMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __roundMethods =
        {
            MathMethod.RoundWithDecimal,
            MathMethod.RoundWithDecimalAndDecimals,
            MathMethod.RoundWithDouble,
            MathMethod.RoundWithDoubleAndDigits
        };

        private static readonly MethodInfo[] __roundWithPlaceMethods =
        {
            MathMethod.RoundWithDecimalAndDecimals,
            MathMethod.RoundWithDoubleAndDigits
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__roundMethods))
            {
                var argumentExpression = arguments[0];
                var argumentTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, argumentExpression);
                SerializationHelper.EnsureRepresentationIsNumeric(expression, argumentExpression, argumentTranslation);

                var argumentAst = ConvertHelper.RemoveWideningConvert(argumentTranslation);
                AstExpression ast;
                if (method.IsOneOf(__roundWithPlaceMethods))
                {
                    var placeExpression = arguments[1];
                    var placeTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, placeExpression);
                    SerializationHelper.EnsureRepresentationIsNumeric(expression, placeExpression, placeTranslation);
                    ast = AstExpression.Round(argumentAst, placeTranslation.Ast);
                }
                else
                {
                    ast = AstExpression.Round(argumentAst);
                }

                return new AggregationExpression(expression, ast, argumentTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
