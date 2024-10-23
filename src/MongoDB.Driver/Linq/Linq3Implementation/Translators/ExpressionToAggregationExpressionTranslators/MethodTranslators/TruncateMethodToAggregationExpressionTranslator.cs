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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class TruncateMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(DateTimeMethod.Truncate, DateTimeMethod.TruncateWithBinSize, DateTimeMethod.TruncateWithBinSizeAndTimezone))
            {
                var dateExpression = arguments[0];
                var dateTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, dateExpression);

                var unitExpression = arguments[1];
                var unitConstant = unitExpression.GetConstantValue<DateTimeUnit>(expression);
                AstExpression unit = unitConstant.Unit;
                AstExpression startOfWeek;
                if (unitConstant is WeekWithStartOfWeekDayTimeUnit unitConstantWithStartOfWeek)
                {
                    startOfWeek = unitConstantWithStartOfWeek.StartOfWeek;
                }
                else
                {
                    startOfWeek = null;
                }

                AstExpression binSize = null;
                if (method.IsOneOf(DateTimeMethod.TruncateWithBinSize, DateTimeMethod.TruncateWithBinSizeAndTimezone))
                {
                    var binSizeExpression = arguments[2];
                    var binSizeTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, binSizeExpression);
                    binSize = ConvertHelper.RemoveWideningConvert(binSizeTranslation);
                }

                AstExpression timezone = null;
                if (method.Is(DateTimeMethod.TruncateWithBinSizeAndTimezone))
                {
                    var timezoneExpression = arguments[3];
                    var timezoneTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, timezoneExpression);
                    timezone = timezoneTranslation.Ast;
                }

                var ast = AstExpression.DateTrunc(dateTranslation.Ast, unit, binSize, timezone, startOfWeek);
                var serializer = DateTimeSerializer.UtcInstance;
                return new AggregationExpression(expression, ast, serializer);
            }

            if (method.IsOneOf(MathMethod.TruncateDecimal, MathMethod.TruncateDouble))
            {
                var argumentExpression = arguments[0];
                var argumentTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, argumentExpression);
                SerializationHelper.EnsureRepresentationIsNumeric(expression, argumentExpression, argumentTranslation);

                var argumentAst = ConvertHelper.RemoveWideningConvert(argumentTranslation);
                var ast = AstExpression.Trunc(argumentAst);
                return new AggregationExpression(expression, ast, argumentTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
