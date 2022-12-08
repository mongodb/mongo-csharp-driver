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

using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal static class GetItemMethodToFilterFieldTranslator
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
                    return ArrayIndexExpressionToFilterFieldTranslator.Translate(context, expression, fieldExpression, indexExpression);
                }

                if (indexExpression.Type == typeof(string))
                {
                    return TranslateWithStringIndex(context, expression, method, fieldExpression, indexExpression);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilterField TranslateWithStringIndex(TranslationContext context, MethodCallExpression expression, MethodInfo method, Expression fieldExpression, Expression indexExpression)
        {
            var field = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
            var key = indexExpression.GetConstantValue<string>(containingExpression: expression);

            if (typeof(BsonValue).IsAssignableFrom(field.Serializer.ValueType))
            {
                var valueSerializer = BsonValueSerializer.Instance;
                return field.SubField(key, valueSerializer);
            }

            if (field.Serializer is IBsonDictionarySerializer dictionarySerializer &&
                dictionarySerializer.DictionaryRepresentation == DictionaryRepresentation.Document)
            {
                var valueSerializer = dictionarySerializer.ValueSerializer;
                if (method.ReturnType.IsAssignableFrom(valueSerializer.ValueType))
                {
                    return field.SubField(key, valueSerializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
