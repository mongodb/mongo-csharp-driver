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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class DateTimeAddOrSubtractMethodToAggregationExpressionTranslator
    {
        public static bool CanTranslate(MethodCallExpression expression)
        {
            return expression.Method.IsOneOf(DateTimeMethod.AddOrSubtractOverloads);
        }

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(DateTimeMethod.AddOrSubtractOverloads))
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

                AstExpression unit, amount;
                if (method.IsOneOf(DateTimeMethod.AddOrSubtractWithTimeSpanOverloads))
                {
                    if (valueExpression is ConstantExpression constantValueExpression)
                    {
                        var ticks = ((TimeSpan)constantValueExpression.Value).Ticks;
                        var milliseconds = ticks / (double)TimeSpan.TicksPerMillisecond;
                        (unit, amount) = ("millisecond", milliseconds);
                    }
                    else
                    {
                        var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                        if (!(valueTranslation.Serializer is TimeSpanSerializer timeSpanSerializer))
                        {
                            throw new ExpressionNotSupportedException(valueExpression, expression);
                        }
                        SerializationHelper.EnsureRepresentationIsNumeric(expression, valueExpression, timeSpanSerializer);

                        var valueAst = ConvertHelper.RemoveWideningConvert(valueTranslation);
                        var serializerUnits = timeSpanSerializer.Units;
                        (unit, amount) = serializerUnits switch
                        {
                            TimeSpanUnits.Ticks => ("millisecond", AstExpression.Divide(valueAst, (double)TimeSpan.TicksPerMillisecond)),
                            TimeSpanUnits.Nanoseconds => ("millisecond", AstExpression.Divide(valueAst, 1000000.0)),
                            TimeSpanUnits.Microseconds => ("millisecond", AstExpression.Divide(valueAst, 1000.0)),
                            TimeSpanUnits.Milliseconds => ("millisecond", valueAst),
                            TimeSpanUnits.Seconds => ("second", valueAst),
                            TimeSpanUnits.Minutes => ("minute", valueAst),
                            TimeSpanUnits.Hours => ("hour", valueAst),
                            TimeSpanUnits.Days => ("day", valueAst),
                            _ => throw new ExpressionNotSupportedException(valueExpression, expression)
                        };
                    }
                }
                else if (method.IsOneOf(DateTimeMethod.AddOrSubtractWithUnitOverloads))
                {
                    var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                    var valueAst = ConvertHelper.RemoveWideningConvert(valueTranslation);
                    var unitExpression = arguments[2];
                    var unitConstant = unitExpression.GetConstantValue<DateTimeUnit>(expression);
                    (unit, amount) = (unitConstant.Unit, valueAst);
                }
                else
                {
                    var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                    SerializationHelper.EnsureRepresentationIsNumeric(expression, valueExpression, valueTranslation);

                    var valueAst = ConvertHelper.RemoveWideningConvert(valueTranslation);
                    (unit, amount) = method.Name switch
                    {
                        "AddTicks" => ("millisecond", AstExpression.Divide(valueAst, (double)TimeSpan.TicksPerMillisecond)),
                        "AddMilliseconds" => ("millisecond", valueAst),
                        "AddSeconds" => ("second", valueAst),
                        "AddMinutes" => ("minute", valueAst),
                        "AddHours" => ("hour", valueAst),
                        "AddDays" => ("day", valueAst),
                        "AddWeeks" => ("week", valueAst),
                        "AddMonths" => ("month", valueAst),
                        "AddQuarters" => ("quarter", valueAst),
                        "AddYears" => ("year", valueAst),
                        _ => throw new ExpressionNotSupportedException(expression)
                    };
                }

                AstExpression timezone = null;
                if (method.IsOneOf(DateTimeMethod.AddOrSubtractWithTimezoneOverloads))
                {
                    var timezoneExpression = arguments.Last();
                    var timezoneTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, timezoneExpression);
                    timezone = timezoneTranslation.Ast;
                }

                var ast = method.IsOneOf(DateTimeMethod.SubtractReturningDateTimeOverloads) ?
                    AstExpression.DateSubtract(thisTranslation.Ast, unit, amount, timezone) :
                    AstExpression.DateAdd(thisTranslation.Ast, unit, amount, timezone);
                var serializer = DateTimeSerializer.UtcInstance;
                return new TranslatedExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
