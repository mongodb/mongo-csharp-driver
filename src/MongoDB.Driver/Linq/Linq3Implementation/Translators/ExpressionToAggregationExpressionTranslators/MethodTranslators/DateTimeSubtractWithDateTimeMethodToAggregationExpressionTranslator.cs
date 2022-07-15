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

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class DateTimeSubtractWithDateTimeMethodToAggregationExpressionTranslator
    {
        private readonly static MethodInfo[] __dateTimeSubtractWithDateTimeMethods =
        {
            DateTimeMethod.SubtractWithDateTime,
            DateTimeMethod.SubtractWithDateTimeAndTimezone,
            DateTimeMethod.SubtractWithDateTimeAndUnit,
            DateTimeMethod.SubtractWithDateTimeAndUnitAndTimezone
        };

        private readonly static MethodInfo[] __dateTimeSubtractWithTimezoneMethods =
        {
            DateTimeMethod.SubtractWithDateTimeAndTimezone,
            DateTimeMethod.SubtractWithDateTimeAndUnitAndTimezone
        };

        private readonly static MethodInfo[] __dateTimeSubtractWithUnitMethods =
        {
            DateTimeMethod.SubtractWithDateTimeAndUnit,
            DateTimeMethod.SubtractWithDateTimeAndUnitAndTimezone
        };

        public static bool CanTranslate(MethodCallExpression expression)
        {
            return expression.Method.IsOneOf(__dateTimeSubtractWithDateTimeMethods);
        }

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__dateTimeSubtractWithDateTimeMethods))
            {
                Expression thisExpression, valueExpression;
                if (method.IsStatic)
                {
                    thisExpression = arguments[0];
                    valueExpression = arguments[1];
                }
                else
                {
                    thisExpression = expression.Object;
                    valueExpression = arguments[0];
                }

                var thisTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, thisExpression);
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);

                AstExpression unit, startOfWeek;
                IBsonSerializer serializer;
                if (method.IsOneOf(__dateTimeSubtractWithUnitMethods))
                {
                    var unitExpression = arguments[2];
                    var unitConstant = unitExpression.GetConstantValue<DateTimeUnit>(containingExpression: expression);
                    unit = unitConstant.Unit;
                    if (unitConstant is WeekWithStartOfWeekDayTimeUnit unitConstantWithStartOfWeek)
                    {
                        startOfWeek = unitConstantWithStartOfWeek.StartOfWeek;
                    }
                    else
                    {
                        startOfWeek = null;
                    }
                    serializer = Int64Serializer.Instance;
                }
                else
                {
                    unit = "millisecond";
                    startOfWeek = null;
                    serializer = new TimeSpanSerializer(representation: BsonType.Int64, units: TimeSpanUnits.Milliseconds);
                }

                AstExpression timezone = null;
                if (method.IsOneOf(__dateTimeSubtractWithTimezoneMethods))
                {
                    var timezoneExpression = arguments.Last();
                    var timezoneTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, timezoneExpression);
                    timezone = timezoneTranslation.Ast;
                }

                var ast = AstExpression.DateDiff(valueTranslation.Ast, thisTranslation.Ast, unit, timezone, startOfWeek);
                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
