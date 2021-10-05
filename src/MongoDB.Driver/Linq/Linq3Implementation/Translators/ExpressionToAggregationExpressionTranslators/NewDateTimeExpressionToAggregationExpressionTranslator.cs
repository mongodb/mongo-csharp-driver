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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewDateTimeExpressionToAggregationExpressionTranslator
    {
        private static readonly ConstructorInfo[] __dateTimeConstructors =
        {
            DateTimeConstructor.WithYearMonthDay,
            DateTimeConstructor.WithYearMonthDayHourMinuteSecond,
            DateTimeConstructor.WithYearMonthDayHourMinuteSecondMillisecond
        };

        public static AggregationExpression Translate(TranslationContext context, NewExpression expression)
        {
            var constructor = expression.Constructor;
            var arguments = expression.Arguments;

            if (constructor.IsOneOf(__dateTimeConstructors))
            {
                var yearExpression = arguments[0];
                var monthExpression = arguments[1];
                var dayExpression = arguments[2];
                var hourExpression = arguments.Count >= 4 ? arguments[3] : null;
                var minuteExpression = arguments.Count >= 4 ? arguments[4] : null;
                var secondExpression = arguments.Count >= 4 ? arguments[5] : null;
                var millisecondExpression = arguments.Count == 7 ? arguments[6] : null;

                var yearTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, yearExpression);
                var monthTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, monthExpression);
                var dayTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, dayExpression);
                var hourTranslation = hourExpression != null ? ExpressionToAggregationExpressionTranslator.Translate(context, hourExpression) : null;
                var minuteTranslation = minuteExpression != null ? ExpressionToAggregationExpressionTranslator.Translate(context, minuteExpression) : null;
                var secondTranslation = secondExpression != null ? ExpressionToAggregationExpressionTranslator.Translate(context, secondExpression) : null;
                var millisecondTranslation = millisecondExpression != null ? ExpressionToAggregationExpressionTranslator.Translate(context, millisecondExpression) : null;

                var ast = AstExpression.DateFromParts(yearTranslation.Ast, monthTranslation.Ast, dayTranslation.Ast, hourTranslation?.Ast, minuteTranslation?.Ast, secondTranslation?.Ast, millisecondTranslation?.Ast);
                var serializer = context.KnownSerializersRegistry.GetSerializer(expression);

                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
