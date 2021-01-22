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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators
{
    public static class NewListExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, NewExpression expression)
        {
            var listType = expression.Type;
            var listItemType = listType.GetGenericArguments()[0];
            var arguments = expression.Arguments;

            if (arguments.Count == 1)
            {
                var argument = arguments[0];
                var argumentType = argument.Type;
                if (argumentType.IsConstructedGenericType && argumentType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var argumentItemType = argumentType.GetGenericArguments()[0];
                    if (argumentItemType == listItemType)
                    {
                        var collectionExpression = argument;
                        var collectionTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, collectionExpression);
                        var listSerializerType = typeof(EnumerableInterfaceImplementerSerializer<,>).MakeGenericType(listType, listItemType);
                        var listItemSerializer = ArraySerializerHelper.GetItemSerializer(collectionTranslation.Serializer);
                        var listSerializer = (IBsonSerializer)Activator.CreateInstance(listSerializerType, listItemSerializer);

                        return new AggregationExpression(expression, collectionTranslation.Ast, listSerializer);
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
