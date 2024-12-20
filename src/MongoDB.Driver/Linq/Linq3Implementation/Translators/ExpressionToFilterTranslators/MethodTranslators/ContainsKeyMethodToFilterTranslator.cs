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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class ContainsKeyMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IsContainsKeyMethod(method))
            {
                var fieldExpression = expression.Object;
                var keyExpression = arguments[0];

                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
                var dictionarySerializer = GetDictionarySerializer(expression, fieldTranslation);
                var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;

                switch (dictionaryRepresentation)
                {
                    case DictionaryRepresentation.Document:
                        var key = GetKeyStringConstant(expression, keyExpression, dictionarySerializer.KeySerializer);
                        var keyField = fieldTranslation.AstField.SubField(key);
                        return AstFilter.Exists(keyField);

                    default:
                        throw new ExpressionNotSupportedException(expression, because: $"ContainsKey is not supported when DictionaryRepresentation is: {dictionaryRepresentation}");
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static IBsonDictionarySerializer GetDictionarySerializer(Expression expression, TranslatedFilterField field)
        {
            if (field.Serializer is IBsonDictionarySerializer dictionarySerializer)
            {
                return dictionarySerializer;
            }

            throw new ExpressionNotSupportedException(expression, because: $"class {field.Serializer.GetType().FullName} does not implement the IBsonDictionarySerializer interface");
        }

        private static string GetKeyStringConstant(Expression expression, Expression keyExpression, IBsonSerializer keySerializer)
        {
            if (keyExpression is ConstantExpression keyConstantExpression)
            {
                var keyValue = keyConstantExpression.Value;
                var serializedKeyValue = SerializationHelper.SerializeValue(keySerializer, keyValue);
                if (serializedKeyValue.BsonType == BsonType.String)
                {
                    return serializedKeyValue.AsString;
                }
            }

            throw new ExpressionNotSupportedException(expression, because: "key must be a constant represented as a string");
        }

        private static bool IsContainsKeyMethod(MethodInfo method)
        {
            return
                !method.IsStatic &&
                method.IsPublic &&
                method.ReturnType == typeof(bool) &&
                method.Name == "ContainsKey" &&
                method.GetParameters() is var parameters &&
                parameters.Length == 1;
        }
    }
}
