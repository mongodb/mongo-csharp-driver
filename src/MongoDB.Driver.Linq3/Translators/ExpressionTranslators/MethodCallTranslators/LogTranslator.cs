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
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodCallTranslators
{
    public static class LogTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(MathMethod.Log, MathMethod.LogWithNewBase, MathMethod.Log10))
            {
                var argument = expression.Arguments[0];
                if (IsConvertThatCanBeRemoved(argument))
                {
                    argument = ((UnaryExpression)argument).Operand;
                }
                var translatedArgument = ExpressionTranslator.Translate(context, argument);

                AstExpression translation;
                if (expression.Method.Is(MathMethod.LogWithNewBase))
                {
                    var newBase = expression.Arguments[1];
                    var translatedNewBase = ExpressionTranslator.Translate(context, newBase);

                    translation = new AstBinaryExpression(AstBinaryOperator.Log, translatedArgument.Translation, translatedNewBase.Translation);
                }
                else
                {
                    var @operator = expression.Method.Is(MathMethod.Log10) ? AstUnaryOperator.Log10 : AstUnaryOperator.Ln;
                    translation = new AstUnaryExpression(@operator, translatedArgument.Translation);
                }

                var serializer = new DoubleSerializer();

                return new TranslatedExpression(expression, translation, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsConvertThatCanBeRemoved(Expression value)
        {
            return true;
        }
    }
}
