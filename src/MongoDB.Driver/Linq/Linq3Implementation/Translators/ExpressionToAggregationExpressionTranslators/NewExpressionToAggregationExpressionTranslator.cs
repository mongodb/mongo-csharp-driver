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
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, NewExpression expression, IBsonSerializer resultSerializer)
        {
            var expressionType = expression.Type;

            if (expressionType == typeof(BsonDocument))
            {
                return NewBsonDocumentExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            if (expressionType == typeof(DateTime))
            {
                return NewDateTimeExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            if (expressionType.IsConstructedGenericType && expressionType.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                return NewHashSetExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            if (expressionType.IsConstructedGenericType && expressionType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return NewListExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            if (NewTupleExpressionToAggregationExpressionTranslator.CanTranslate(expression))
            {
                return NewTupleExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            return MemberInitExpressionToAggregationExpressionTranslator.Translate(context, expression, expression, Array.Empty<MemberBinding>(), resultSerializer);
        }
    }
}
