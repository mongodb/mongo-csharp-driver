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

using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal static class GetItemMethodToFilterFieldTranslator
    {
        // public static methods
        public static TranslatedFilterField Translate(TranslationContext context, MethodCallExpression expression)
        {
            var fieldExpression = expression.Object;
            var method = expression.Method;
            var arguments = expression.Arguments;
            return Translate(context, expression, method, fieldExpression, arguments);
        }

        public static TranslatedFilterField Translate(TranslationContext context, Expression expression, MethodInfo method, Expression fieldExpression, ReadOnlyCollection<Expression> arguments)
        {
            if (BsonValueMethod.IsGetItemWithIntMethod(method))
            {
                return TranslateBsonValueGetItemWithInt(context, expression, fieldExpression, arguments[0]);
            }

            if (BsonValueMethod.IsGetItemWithStringMethod(method))
            {
                return TranslateBsonValueGetItemWithString(context, expression, fieldExpression, arguments[0]);
            }

            if (IListMethod.IsGetItemWithIntMethod(method))
            {
                return TranslateIListGetItemWithInt(context, expression, fieldExpression, arguments[0]);
            }

            if (DictionaryMethod.IsGetItemWithKeyMethod(method))
            {
                return TranslateDictionaryGetItemWithKey(context, expression, fieldExpression, arguments[0]);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static TranslatedFilterField TranslateBsonValueGetItemWithInt(TranslationContext context, Expression expression, Expression fieldExpression, Expression indexExpression)
        {
            return ArrayIndexExpressionToFilterFieldTranslator.Translate(context, expression, fieldExpression, indexExpression);
        }

        private static TranslatedFilterField TranslateBsonValueGetItemWithString(TranslationContext context, Expression expression, Expression fieldExpression, Expression keyExpression)
        {
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
            var key = keyExpression.GetConstantValue<string>(containingExpression: expression);
            return fieldTranslation.SubField(key, BsonValueSerializer.Instance);
        }

        private static TranslatedFilterField TranslateIListGetItemWithInt(TranslationContext context, Expression expression, Expression fieldExpression, Expression indexExpression)
        {
            return ArrayIndexExpressionToFilterFieldTranslator.Translate(context, expression, fieldExpression, indexExpression);
        }

        private static TranslatedFilterField TranslateDictionaryGetItemWithKey(TranslationContext context, Expression expression, Expression fieldExpression, Expression keyExpression)
        {
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
            var key = keyExpression.GetConstantValue<object>(containingExpression: expression);

            if (!(fieldTranslation.Serializer is IBsonDictionarySerializer dictionarySerializer))
            {
                throw new ExpressionNotSupportedException(expression, because: $"dictionary serializer class {fieldTranslation.Serializer.GetType()} does not implement {nameof(IBsonDictionarySerializer)}");
            }
            if (dictionarySerializer.DictionaryRepresentation != DictionaryRepresentation.Document)
            {
                throw new ExpressionNotSupportedException(expression, because: "dictionary is not represented as a document");
            }

            var keySerializer = dictionarySerializer.KeySerializer;
            var valueSerializer = dictionarySerializer.ValueSerializer;

            var serializedKey = SerializationHelper.SerializeValue(keySerializer, key);
            if (serializedKey is not BsonString)
            {
                throw new ExpressionNotSupportedException(expression, because: "key did not serialize as a string");
            }

            return fieldTranslation.SubField(serializedKey.AsString, valueSerializer);
        }
    }
}
