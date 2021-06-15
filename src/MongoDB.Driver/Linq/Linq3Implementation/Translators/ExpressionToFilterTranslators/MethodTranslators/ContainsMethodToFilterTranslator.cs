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
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class ContainsMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (StringExpressionToRegexFilterTranslator.CanTranslate(expression))
            {
                return StringExpressionToRegexFilterTranslator.Translate(context, expression);
            }

            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsStatic &&
                method.Name == "Contains" &&
                method.ReturnType == typeof(bool) &&
                arguments.Count == 2)
            {
                var sourceExpression = arguments[0];
                var sourceType = sourceExpression.Type;
                var itemExpression = arguments[1];
                var itemType = itemExpression.Type;
                if (TypeImplementsIEnumerable(sourceType, itemType))
                {
                    return Translate(context, expression, sourceExpression, itemExpression);
                }
            }

            if (!method.IsStatic &&
                method.Name == "Contains" &&
                method.ReturnType == typeof(bool) &&
                arguments.Count == 1)
            {
                var sourceExpression = expression.Object;
                var sourceType = sourceExpression.Type;
                var itemExpression = arguments[0];
                var itemType = itemExpression.Type;
                if (TypeImplementsIEnumerable(sourceType, itemType))
                {
                    return Translate(context, expression, sourceExpression, itemExpression);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilter Translate(TranslationContext context, Expression expression, Expression sourceExpression, Expression itemExpression)
        {
            if (itemExpression.NodeType == ExpressionType.Constant)
            {
                var sourceField = ExpressionToFilterFieldTranslator.Translate(context, sourceExpression);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceField.Serializer);
                var value = itemExpression.GetConstantValue<object>(containingExpression: expression);
                var serializedValue = SerializationHelper.SerializeValue(itemSerializer, value);
                return AstFilter.ElemMatch(sourceField, AstFilter.Eq(AstFilter.Field("@<elem>", itemSerializer), serializedValue)); // @<elem> represents the implied element 
            }

            var itemField = ExpressionToFilterFieldTranslator.Translate(context, itemExpression);
            var sourceValues = sourceExpression.GetConstantValue<IEnumerable>(containingExpression: expression);
            var serializedValues = SerializationHelper.SerializeValues(itemField.Serializer, sourceValues);
            return AstFilter.In(itemField, serializedValues);
        }

        private static bool TypeImplementsIEnumerable(Type type, Type itemType)
        {
            var ienumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
            return ienumerableType.IsAssignableFrom(type);
        }
    }
}
