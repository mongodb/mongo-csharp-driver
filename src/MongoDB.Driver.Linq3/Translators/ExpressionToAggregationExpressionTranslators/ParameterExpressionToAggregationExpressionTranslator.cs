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
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators
{
    public static class ParameterExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, ParameterExpression expression)
        {
            var symbolTable = context.SymbolTable;
            if (symbolTable.TryGetSymbol(expression, out Symbol symbol))
            {
                var field = symbol == symbolTable.Current ? "$CURRENT" : symbol.Name;
                var ast = AstExpression.Field(field);
                return new AggregationExpression(expression, ast, symbol.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
