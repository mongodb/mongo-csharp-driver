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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
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
            if (StringExpressionToRegexFilterTranslator.TryTranslate(context, expression, out var filter))
            {
                return filter;
            }

            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsStatic &&
                method.Name == "Contains" &&
                method.ReturnType == typeof(bool) &&
                arguments.Count == 2)
            {
                var fieldExpression = arguments[0];
                var fieldType = fieldExpression.Type;
                var itemExpression = arguments[1];
                var itemType = itemExpression.Type;

                if (TryTranslateDictionaryKeysOrValuesContains(context, expression, fieldExpression, itemExpression, out var dictionaryFilter))
                {
                    return dictionaryFilter;
                }

                if (TypeImplementsIEnumerable(fieldType, itemType))
                {
                    return Translate(context, expression, fieldExpression, itemExpression);
                }
            }

            if (!method.IsStatic &&
                method.Name == "Contains" &&
                method.ReturnType == typeof(bool) &&
                arguments.Count == 1)
            {
                var fieldExpression = expression.Object;
                var itemExpression = arguments[0];

                if (TryTranslateDictionaryKeysOrValuesContains(context, expression, fieldExpression, itemExpression, out var dictionaryFilter))
                {
                    return dictionaryFilter;
                }

                // Otherwise, handle as regular Contains on IEnumerable
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

        private static bool TryTranslateDictionaryKeysOrValuesContains(
            TranslationContext context,
            Expression expression,
            Expression fieldExpression,
            Expression itemExpression,
            out AstFilter filter)
        {
            filter = null;

            if (fieldExpression is not MemberExpression memberExpression)
            {
                return false;
            }

            var memberName = memberExpression.Member.Name;
            var declaringType = memberExpression.Member.DeclaringType;

            if (!declaringType.IsGenericType ||
                (declaringType.GetGenericTypeDefinition() != typeof(Dictionary<,>) &&
                declaringType.GetGenericTypeDefinition() != typeof(IDictionary<,>)))
            {
                return false;
            }

            switch (memberName)
            {
                case "Keys":
                {
                    var dictionaryExpression = memberExpression.Expression;
                    filter = ContainsKeyMethodToFilterTranslator.TranslateContainsKey(context, expression, dictionaryExpression, itemExpression);
                    return true;
                }
                case "Values":
                {
                    var dictionaryExpression = memberExpression.Expression;
                    filter = ContainsValueMethodToFilterTranslator.TranslateContainsValue(context, expression, dictionaryExpression, itemExpression);
                    return true;
                }
                default:
                    return false;
            }
        }
    }
}
