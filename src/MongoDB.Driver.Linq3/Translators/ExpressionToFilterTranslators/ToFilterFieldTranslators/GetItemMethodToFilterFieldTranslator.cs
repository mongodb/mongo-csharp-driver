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

using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.ExtensionMethods;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    public static class GetItemMethodToFilterFieldTranslator
    {
        public static AstFilterField Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.IsStatic &&
                method.IsSpecialName &&
                method.Name == "get_Item" &&
                arguments.Count == 1)
            {
                var fieldExpression = expression.Object;
                var indexExpression = arguments[0];

                if (indexExpression.Type == typeof(int))
                {
                    var index = indexExpression.GetConstantValue<int>(containingExpression: expression);
                    return TranslateWithIntIndex(context, expression, method, fieldExpression, index);
                }

                if (indexExpression.Type == typeof(string))
                {
                    var index = indexExpression.GetConstantValue<string>(containingExpression: expression);
                    return TranslateWithStringIndex(context, expression, method, fieldExpression, index);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilterField TranslateWithIntIndex(TranslationContext context, MethodCallExpression expression, MethodInfo method, Expression fieldExpression, int index)
        {
            var field = ExpressionToFilterFieldTranslator.TranslateEnumerable(context, fieldExpression);

            if (field.Serializer is IBsonArraySerializer arraySerializer &&
                arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
            {
                var itemSerializer = itemSerializationInfo.Serializer;
                if (method.ReturnType.IsAssignableFrom(itemSerializer.ValueType))
                {
                    return field.SubField(index.ToString(), itemSerializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilterField TranslateWithStringIndex(TranslationContext context, MethodCallExpression expression, MethodInfo method, Expression fieldExpression, string index)
        {
            var field = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);

            if (field.Serializer is IBsonDictionarySerializer dictionarySerializer &&
                dictionarySerializer.DictionaryRepresentation == DictionaryRepresentation.Document)
            {
                var valueSerializer = dictionarySerializer.ValueSerializer;
                if (method.ReturnType.IsAssignableFrom(valueSerializer.ValueType))
                {
                    return field.SubField(index, valueSerializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
