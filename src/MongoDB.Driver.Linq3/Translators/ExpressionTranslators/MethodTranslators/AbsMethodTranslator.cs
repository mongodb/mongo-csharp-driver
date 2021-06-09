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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class AbsMethodTranslator
    {
        private static MethodInfo[] __absMethods =
        {
            MathMethod.AbsDecimal,
            MathMethod.AbsDouble,
            MathMethod.AbsInt32,
            MathMethod.AbsInt64
        };

        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(__absMethods))
            {
                var valueExpression = expression.Arguments[0];

                var serverType = expression.Type;
                valueExpression = ConvertHelper.RemoveUnnecessaryConvert(valueExpression, serverType);
                var argumentTranslation = ExpressionTranslator.Translate(context, valueExpression);
                var ast = new AstUnaryExpression(AstUnaryOperator.Abs, argumentTranslation.Ast);

                return new ExpressionTranslation(expression, ast, argumentTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
