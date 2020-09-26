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

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class LogMethodTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(MathMethod.Log, MathMethod.LogWithNewBase, MathMethod.Log10))
            {
                var argumentExpression = expression.Arguments[0];

                argumentExpression = ConvertHelper.RemoveUnnecessaryConvert(argumentExpression, typeof(double));
                var argumentTranslation = ExpressionTranslator.Translate(context, argumentExpression);
                AstExpression ast;
                if (expression.Method.Is(MathMethod.LogWithNewBase))
                {
                    var newBaseExpression = expression.Arguments[1];
                    var newBaseTranslation = ExpressionTranslator.Translate(context, newBaseExpression);
                    ast = new AstBinaryExpression(AstBinaryOperator.Log, argumentTranslation.Ast, newBaseTranslation.Ast);
                }
                else
                {
                    var @operator = expression.Method.Is(MathMethod.Log10) ? AstUnaryOperator.Log10 : AstUnaryOperator.Ln;
                    ast = new AstUnaryExpression(@operator, argumentTranslation.Ast);
                }

                return new ExpressionTranslation(expression, ast, new DoubleSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
