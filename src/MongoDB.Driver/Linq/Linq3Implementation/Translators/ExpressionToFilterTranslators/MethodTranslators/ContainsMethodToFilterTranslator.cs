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
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class ContainsMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (StringExpressionToRegexFilterTranslator.TryTranslate(context, expression, out var filter))
            {
                return filter;
            }

            var method = expression.Method;
            var arguments = expression.Arguments;

            if (EnumerableMethod.IsContainsMethod(expression, out var fieldExpression, out var itemExpression))
            {
                var fieldType = fieldExpression.Type;
                var itemType = itemExpression.Type;

                if (TypeImplementsIEnumerable(fieldType, itemType))
                {
                    return Translate(context, expression, fieldExpression, itemExpression);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilter Translate(TranslationContext context, Expression expression, Expression fieldExpression, Expression itemExpression)
        {
            if (itemExpression.NodeType == ExpressionType.Constant)
            {
                var fieldTranslation = ExpressionToFilterFieldTranslator.TranslateEnumerable(context, fieldExpression);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(fieldTranslation.Serializer);
                var value = itemExpression.GetConstantValue<object>(containingExpression: expression);
                var serializedValue = SerializationHelper.SerializeValue(itemSerializer, value);
                return AstFilter.ElemMatch(fieldTranslation.Ast, AstFilter.Eq(AstFilter.Field("@<elem>"), serializedValue)); // @<elem> represents the implied element
            }

            var itemTranslation = ExpressionToFilterFieldTranslator.Translate(context, itemExpression);
            var sourceValues = fieldExpression.GetConstantValue<IEnumerable>(containingExpression: expression);
            var serializedValues = SerializationHelper.SerializeValues(itemTranslation.Serializer, sourceValues);
            return AstFilter.In(itemTranslation.Ast, serializedValues);
        }

        private static bool TypeImplementsIEnumerable(Type type, Type itemType)
        {
            var ienumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
            return ienumerableType.IsAssignableFrom(type);
        }
    }
}
