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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class ExceptMethodTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.Except))
            {
                var firstExpression = expression.Arguments[0];
                var secondExpression = expression.Arguments[1];

                var firstTranslation = ExpressionTranslator.Translate(context, firstExpression);
                var secondTranslation = ExpressionTranslator.Translate(context, secondExpression);
                var ast = new AstBinaryExpression(AstBinaryOperator.SetDifference, firstTranslation.Ast, secondTranslation.Ast);

                return new ExpressionTranslation(expression, ast, firstTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
