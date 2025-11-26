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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class ContainsValueMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IsContainsValueMethod(method))
            {
                var fieldExpression = expression.Object;
                var valueExpression = arguments[0];
                return TranslateContainsValue(context, expression, fieldExpression, valueExpression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static AstFilter TranslateContainsValue(TranslationContext context, Expression expression, Expression fieldExpression, Expression valueExpression)
        {
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
            var dictionarySerializer = GetDictionarySerializer(expression, fieldTranslation);
            var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;
            var valueSerializer = dictionarySerializer.ValueSerializer;

            if (valueExpression is ConstantExpression constantValueExpression)
            {
                var value = constantValueExpression.Value;
                var serializedValue = SerializationHelper.SerializeValue(valueSerializer, value);

                switch (dictionaryRepresentation)
                {
                    case DictionaryRepresentation.ArrayOfDocuments:
                    case DictionaryRepresentation.ArrayOfArrays:
                        {
                            var fieldName = dictionaryRepresentation == DictionaryRepresentation.ArrayOfDocuments ? "v" : "1";
                            var valueField = AstFilter.Field(fieldName);
                            return AstFilter.ElemMatch(fieldTranslation.Ast, AstFilter.Eq(valueField, serializedValue));
                        }

                    default:
                        throw new ExpressionNotSupportedException(expression, because: $"DictionaryRepresentation: {dictionaryRepresentation} is not supported for ContainsValue method.");
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static IBsonDictionarySerializer GetDictionarySerializer(Expression expression, TranslatedFilterField fieldTranslation)
        {
            if (fieldTranslation.Serializer is IBsonDictionarySerializer dictionarySerializer)
            {
                return dictionarySerializer;
            }

            throw new ExpressionNotSupportedException(expression, because: $"class {fieldTranslation.Serializer.GetType().FullName} does not implement the IBsonDictionarySerializer interface");
        }

        private static bool IsContainsValueMethod(MethodInfo method)
        {
            return
                !method.IsStatic &&
                method.IsPublic &&
                method.ReturnType == typeof(bool) &&
                method.Name == "ContainsValue" &&
                method.GetParameters() is var parameters &&
                parameters.Length == 1;
        }
    }
}
