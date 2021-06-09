﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public static class ContainsMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsStatic &&
                method.Name == "Contains" &&
                method.ReturnType == typeof(bool) &&
                arguments.Count == 2)
            {
                var sourceExpression = arguments[0];
                var itemExpression = arguments[1];

                var sourceType = sourceExpression.Type;
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
                var itemExpression = arguments[0];

                var sourceType = sourceExpression.Type;
                var itemType = itemExpression.Type;
                if (TypeImplementsIEnumerable(sourceType, itemType))
                {
                    return Translate(context, expression, sourceExpression, itemExpression);
                }
            }

            throw new ExpressionNotSupportedException(expression);

            bool TypeImplementsIEnumerable(Type type, Type itemType)
            {
                var ienumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
                return ienumerableType.IsAssignableFrom(type);
            }
        }

        private static AstFilter Translate(TranslationContext context, Expression expression, Expression sourceExpression, Expression itemExpression)
        {
            if (sourceExpression is ConstantExpression constantSourceExpression)
            {
                var field = ExpressionToFilterFieldTranslator.Translate(context, itemExpression);
                var sourceValues = (IEnumerable)constantSourceExpression.Value;
                var sourceSerializer = IEnumerableSerializer.Create(field.Serializer);
                var serializedValues = SerializationHelper.SerializeValues(field.Serializer, sourceValues);
                return new AstInFilter(field, serializedValues);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
