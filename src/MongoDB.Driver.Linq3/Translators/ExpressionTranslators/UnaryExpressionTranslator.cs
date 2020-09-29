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

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class UnaryExpressionTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, UnaryExpression expression)
        {
            AstUnaryOperator? @operator = null;
            switch (expression.NodeType)
            {
                case ExpressionType.Convert: return ConvertUnaryExpressionTranslator.Translate(context, expression);
                case ExpressionType.Not: @operator = AstUnaryOperator.Not; break;
            }

            if (@operator != null)
            {
                var operandTranslation = ExpressionTranslator.Translate(context, expression.Operand);
                var ast = new AstUnaryExpression(@operator.Value, operandTranslation.Ast);

                return new ExpressionTranslation(expression, ast, operandTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
