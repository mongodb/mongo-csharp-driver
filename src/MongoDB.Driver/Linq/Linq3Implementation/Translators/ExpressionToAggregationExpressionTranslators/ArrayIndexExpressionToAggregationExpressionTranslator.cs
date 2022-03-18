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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ArrayIndexExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, BinaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.ArrayIndex)
            {
                var arrayExpression = expression.Left;
                var arrayTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, arrayExpression);
                var indexExpression = expression.Right;
                var indexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, indexExpression);
                var ast = AstExpression.ArrayElemAt(arrayTranslation.Ast, indexTranslation.Ast);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(arrayTranslation.Serializer);
                return new AggregationExpression(expression, ast, itemSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
