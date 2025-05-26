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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewArrayInitExpressionToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, NewArrayExpression expression)
        {
            var items = new List<AstExpression>();
            IBsonSerializer itemSerializer = null;
            foreach (var itemExpression in expression.Expressions)
            {
                var itemTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, itemExpression);
                items.Add(itemTranslation.Ast);
                itemSerializer ??= itemTranslation.Serializer;

                // make sure all items are serialized using the same serializer
                if (!itemTranslation.Serializer.Equals(itemSerializer))
                {
                    throw new ExpressionNotSupportedException(expression, because: "all items in the array must be serialized using the same serializer");
                }
            }

            var ast = AstExpression.ComputedArray(items);

            var arrayType = expression.Type;
            var itemType = arrayType.GetElementType();
            itemSerializer ??= context.SerializationDomain.LookupSerializer(itemType); // if the array is empty itemSerializer will be null
            var arraySerializerType = typeof(ArraySerializer<>).MakeGenericType(itemType);
            var arraySerializer = (IBsonSerializer)Activator.CreateInstance(arraySerializerType, itemSerializer);

            return new TranslatedExpression(expression, ast, arraySerializer);
        }
    }
}
