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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class ToStringMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __dateTimeToStringMethods = new[]
        {
            DateTimeMethod.ToStringWithFormat,
            DateTimeMethod.ToStringWithFormatAndTimezone,
            NullableDateTimeMethod.ToStringWithFormatAndTimezoneAndOnNull,
        };

        private static readonly MethodInfo[] __dateTimeToStringMethodsWithTimezone = new[]
        {
            DateTimeMethod.ToStringWithFormatAndTimezone,
            NullableDateTimeMethod.ToStringWithFormatAndTimezoneAndOnNull,
        };

        private static readonly MethodInfo[] __dateTimeToStringMethodsWithOnNull = new[]
        {
            NullableDateTimeMethod.ToStringWithFormatAndTimezoneAndOnNull,
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments.ToArray();

            if (IsInstanceToStringMethodWithNoArguments(method))
            {
                return TranslateInstanceToStringMethodWithNoArguments(context, expression);
            }

            if (method.IsOneOf(__dateTimeToStringMethods))
            {
                return TranslateDateTimeToStringMethod(context, expression, method, arguments);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsInstanceToStringMethodWithNoArguments(MethodInfo method)
        {
            return
                !method.IsStatic &&
                method.Name == "ToString" &&
                method.ReturnType == typeof(string) &&
                method.GetParameters().Length == 0;
        }

        private static AggregationExpression TranslateDateTimeToStringMethod(TranslationContext context, MethodCallExpression expression, MethodInfo method, Expression[] arguments)
        {

            var dateTimeExpression = method.IsStatic ? arguments[0] : expression.Object;
            var dateTimeTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, dateTimeExpression);
            var dateAst = dateTimeTranslation.Ast;

            AstExpression formatAst = null;
            var formatExpression = method.IsStatic ? arguments[1] : arguments[0];
            if (!(formatExpression is ConstantExpression constantExprssion) || constantExprssion.Value != null)
            {
                var formatTranslataion = ExpressionToAggregationExpressionTranslator.Translate(context, formatExpression);
                formatAst = formatTranslataion.Ast;
            }

            AstExpression timezoneAst = null;
            if (method.IsOneOf(__dateTimeToStringMethodsWithTimezone))
            {
                var timezoneExpression = arguments[2];
                if (!(timezoneExpression is ConstantExpression constantExpression) || constantExpression.Value != null)
                {
                    var timezoneTranslataion = ExpressionToAggregationExpressionTranslator.Translate(context, timezoneExpression);
                    timezoneAst = timezoneTranslataion.Ast;
                }
            }

            AstExpression onNullAst = null;
            if (method.IsOneOf(__dateTimeToStringMethodsWithOnNull))
            {
                var onNullExpression = arguments[3];
                var onNullTranslataion = ExpressionToAggregationExpressionTranslator.Translate(context, onNullExpression);
                if (!(onNullExpression is ConstantExpression constantExpression) || constantExpression.Value != null)
                {
                    onNullAst = onNullTranslataion.Ast;
                }
            }

            var ast = AstExpression.DateToString(dateAst, formatAst, timezoneAst, onNullAst);
            return new AggregationExpression(expression, ast, StringSerializer.Instance);
        }

        private static AggregationExpression TranslateInstanceToStringMethodWithNoArguments(TranslationContext context, MethodCallExpression expression)
        {
            var objectExpression = expression.Object;
            var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);
            var ast = AstExpression.ToString(objectTranslation.Ast);
            return new AggregationExpression(expression, ast, StringSerializer.Instance);
        }
    }
}
