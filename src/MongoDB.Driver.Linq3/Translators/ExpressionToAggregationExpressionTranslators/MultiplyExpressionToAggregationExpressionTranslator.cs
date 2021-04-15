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

using System;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators
{
    public static class MultiplyExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, BinaryExpression expression)
        {
            var leftExpression = ConvertHelper.RemoveWideningConvert(expression.Left);
            var leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, leftExpression);
            var rightExpression = ConvertHelper.RemoveWideningConvert(expression.Right);
            var rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rightExpression);
            var ast = AstExpression.Multiply(leftTranslation.Ast, rightTranslation.Ast);
            var serializer = BsonSerializer.LookupSerializer(expression.Type);
            return new AggregationExpression(expression, ast, serializer);
        }
    }
}
