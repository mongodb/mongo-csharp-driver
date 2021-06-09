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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class PowMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(MathMethod.Pow))
            {
                var xExpression = expression.Arguments[0];
                var yExpression = expression.Arguments[1];

                xExpression = ConvertHelper.RemoveUnnecessaryConvert(xExpression, typeof(double));
                yExpression = ConvertHelper.RemoveUnnecessaryConvert(yExpression, typeof(double));
                var xTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, xExpression);
                var yTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, yExpression);
                var ast = new AstBinaryExpression(AstBinaryOperator.Pow, xTranslation.Ast, yTranslation.Ast);

                return new AggregationExpression(expression, ast, new DoubleSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
