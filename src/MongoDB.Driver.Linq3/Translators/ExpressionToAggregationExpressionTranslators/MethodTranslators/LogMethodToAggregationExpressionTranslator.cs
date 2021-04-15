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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class LogMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(MathMethod.Log, MathMethod.LogWithNewBase, MathMethod.Log10))
            {
                var argumentExpression = ConvertHelper.RemoveWideningConvert(arguments[0]);
                var argumentTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, argumentExpression);
                AstExpression ast;
                if (method.Is(MathMethod.LogWithNewBase))
                {
                    var newBaseExpression = arguments[1];
                    var newBaseTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, newBaseExpression);
                    ast = AstExpression.Log(argumentTranslation.Ast, newBaseTranslation.Ast);
                }
                else
                {
                    ast = method.Is(MathMethod.Log10) ?
                        AstExpression.Log10(argumentTranslation.Ast) :
                        AstExpression.Ln(argumentTranslation.Ast);
                }
                return new AggregationExpression(expression, ast, new DoubleSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
