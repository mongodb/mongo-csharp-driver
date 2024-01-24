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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class DateFromStringMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __dateFromStringMethods =
        {
            MqlMethod.DateFromString,
            MqlMethod.DateFromStringWithFormat,
            MqlMethod.DateFromStringWithFormatAndTimezone,
            MqlMethod.DateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull
        };

        private static readonly MethodInfo[] __withFormatMethods =
        {
            MqlMethod.DateFromStringWithFormat,
            MqlMethod.DateFromStringWithFormatAndTimezone,
            MqlMethod.DateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull
        };

        private static readonly MethodInfo[] __withTimezoneMethods =
        {
            MqlMethod.DateFromStringWithFormatAndTimezone,
            MqlMethod.DateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__dateFromStringMethods))
            {
                var dateStringExpression = arguments[0];
                var dateStringTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, dateStringExpression);
                var dateStringAst = dateStringTranslation.Ast;
                IBsonSerializer resultSerializer = DateTimeSerializer.Instance;

                AstExpression format = null;
                if (method.IsOneOf(__withFormatMethods))
                {
                    var formatExpression = arguments[1];
                    var formatTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, formatExpression);
                    format = formatTranslation.Ast;
                }

                AstExpression timezoneAst = null;
                if (method.IsOneOf(__withTimezoneMethods))
                {
                    var timezoneExpression = arguments[2];
                    var timezoneTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, timezoneExpression);
                    timezoneAst = timezoneTranslation.Ast;
                }

                AstExpression onErrorAst = null;
                AstExpression onNullAst = null;
                if (method.Is(MqlMethod.DateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull))
                {
                    var onErrorExpression = arguments[3];
                    var onErrorTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, onErrorExpression);
                    onErrorAst = onErrorTranslation.Ast;

                    var onNullExpression = arguments[4];
                    var onNullTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, onNullExpression);
                    onNullAst = onNullTranslation.Ast;

                    resultSerializer = NullableSerializer.Create(resultSerializer);
                }

                var ast = AstExpression.DateFromString(dateStringAst, format, timezoneAst, onErrorAst, onNullAst);
                return new AggregationExpression(expression, ast, resultSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
