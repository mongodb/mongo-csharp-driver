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
                var dictionaryExpression = expression.Object;
                var valueExpression = arguments[0];

                var dictionaryField = ExpressionToFilterFieldTranslator.Translate(context, dictionaryExpression);
                var dictionarySerializer = GetDictionarySerializer(expression, dictionaryField);
                var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;
                var valueSerializer = dictionarySerializer.ValueSerializer;

                if (valueExpression is ConstantExpression constantValueExpression)
                {
                    var valueField = AstFilter.Field("v", valueSerializer);
                    var value = constantValueExpression.Value;
                    var serializedValue = SerializationHelper.SerializeValue(valueSerializer, value);

                    switch (dictionaryRepresentation)
                    {
                        case DictionaryRepresentation.ArrayOfDocuments:
                            return AstFilter.ElemMatch(dictionaryField, AstFilter.Eq(valueField, serializedValue));

                        default:
                            throw new ExpressionNotSupportedException(expression, because: $"ContainsValue is not supported when DictionaryRepresentation is: {dictionaryRepresentation}");
                    }   
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static IBsonDictionarySerializer GetDictionarySerializer(Expression expression, AstFilterField field)
        {
            if (field.Serializer is IBsonDictionarySerializer dictionarySerializer)
            {
                return dictionarySerializer;
            }

            throw new ExpressionNotSupportedException(expression, because: $"class {field.Serializer.GetType().FullName} does not implement the IBsonDictionarySerializer interface");
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
