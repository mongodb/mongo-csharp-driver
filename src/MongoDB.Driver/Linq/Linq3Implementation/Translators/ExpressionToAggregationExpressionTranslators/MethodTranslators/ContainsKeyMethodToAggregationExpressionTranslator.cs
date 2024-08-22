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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class ContainsKeyMethodToAggregationExpressionTranslator
    {
        // public methods
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IsContainsKeyMethod(method))
            {
                var dictionaryExpression = expression.Object;
                var keyExpression = arguments[0];

                var dictionaryTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, dictionaryExpression);
                var dictionarySerializer = GetDictionarySerializer(expression, dictionaryTranslation);
                var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;

                AstExpression ast;
                switch (dictionaryRepresentation)
                {
                    case DictionaryRepresentation.Document:
                        var keyFieldName = GetKeyFieldName(context, expression, keyExpression, dictionarySerializer.KeySerializer);
                        ast = AstExpression.IsNotMissing(AstExpression.GetField(dictionaryTranslation.Ast, keyFieldName));
                        break;

                    default:
                        throw new ExpressionNotSupportedException(expression, because: $"ContainsKey is not supported when DictionaryRepresentation is: {dictionaryRepresentation}");
                }

                return new AggregationExpression(expression, ast, BooleanSerializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstExpression GetKeyFieldName(TranslationContext context, Expression expression, Expression keyExpression, IBsonSerializer keySerializer)
        {
            if (keyExpression is ConstantExpression keyConstantExpression)
            {
                var keyValue = keyConstantExpression.Value;
                var serializedKeyValue = SerializationHelper.SerializeValue(keySerializer, keyValue);
                ThrowIfKeyIsNotRepresentedAsAString(expression, serializedKeyValue.BsonType);
                return AstExpression.Constant(serializedKeyValue);
            }

            ThrowIfKeyIsNotRepresentedAsAString(expression, keySerializer);
            var keyTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, keyExpression);
            return keyTranslation.Ast;
        }

        private static IBsonDictionarySerializer GetDictionarySerializer(Expression expression, AggregationExpression dictionaryTranslation)
        {
            if (dictionaryTranslation.Serializer is IBsonDictionarySerializer dictionarySerializer)
            {
                return dictionarySerializer;
            }

            throw new ExpressionNotSupportedException(expression, because: $"class {dictionaryTranslation.Serializer.GetType().FullName} does not implement the IBsonDictionarySerializer interface");
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

        private static void ThrowIfKeyIsNotRepresentedAsAString(Expression expression, IBsonSerializer keySerializer)
        {
            if (keySerializer is not IHasRepresentationSerializer hasRepresentationSerializer)
            {
                throw new ExpressionNotSupportedException(expression, because: "unable to determine if key is represented as a string");
            }
            var keyRepresentation = hasRepresentationSerializer.Representation;

            ThrowIfKeyIsNotRepresentedAsAString(expression, keyRepresentation);
        }

        private static void ThrowIfKeyIsNotRepresentedAsAString(Expression expression, BsonType keyRepresentation)
        {
            if (keyRepresentation != BsonType.String)
            {
                throw new ExpressionNotSupportedException(expression, because: "key is not represented as a string");
            }
        }
    }
}
