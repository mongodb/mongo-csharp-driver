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
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class GetItemMethodToAggregationExpressionTranslator
    {
        // public static methods
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var sourceExpression = expression.Object;
            var method = expression.Method;
            var arguments = expression.Arguments;
            return Translate(context, expression, method, sourceExpression, arguments);
        }

        public static TranslatedExpression Translate(TranslationContext context, Expression expression, MethodInfo method, Expression sourceExpression, ReadOnlyCollection<Expression> arguments)
        {
            if (BsonValueMethod.IsGetItemWithIntMethod(method))
            {
                return TranslateBsonValueGetItemWithInt(context, expression, sourceExpression, arguments[0]);
            }

            if (BsonValueMethod.IsGetItemWithStringMethod(method))
            {
                return TranslateBsonValueGetItemWithString(context, expression, sourceExpression, arguments[0]);
            }

            if (IListMethod.IsGetItemWithIntMethod(method))
            {
                return TranslateIListGetItemWithInt(context, expression, sourceExpression, arguments[0]);
            }

            if (DictionaryMethod.IsGetItemWithKeyMethod(method))
            {
                return TranslateIDictionaryGetItemWithKey(context, expression, sourceExpression, arguments[0]);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static IBsonSerializer GetDictionaryValueSerializer(IBsonSerializer serializer)
        {
            if (serializer is IBsonDictionarySerializer dictionarySerializer)
            {
                return dictionarySerializer.ValueSerializer;
            }

            throw new InvalidOperationException($"Unable to determine value serializer for dictionary serializer: {serializer.GetType().FullName}.");
        }

        private static AstExpression GetLimitIfSupported(TranslationContext context)
        {
            var compatibilityLevel = context.TranslationOptions.CompatibilityLevel;
            if (Feature.FilterLimit.IsSupported(compatibilityLevel.ToWireVersion()))
            {
                return AstExpression.Constant(1);
            }
            return null;
        }

        private static TranslatedExpression TranslateBsonValueGetItemWithInt(TranslationContext context, Expression expression, Expression sourceExpression, Expression indexExpression)
        {
            var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
            var indexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, indexExpression);

            AstExpression ast;
            if (sourceExpression.Type == typeof(BsonDocument) || sourceExpression.Type.IsSubclassOf(typeof(BsonDocument)))
            {
                var objectArray = AstExpression.Unary(AstUnaryOperator.ObjectToArray, sourceTranslation.Ast);
                var objectArrayItem = AstExpression.ArrayElemAt(objectArray, indexTranslation.Ast);
                ast = AstExpression.GetField(objectArrayItem, "v");
            }
            else
            {
                ast = AstExpression.ArrayElemAt(sourceTranslation.Ast, indexTranslation.Ast);
            }
            var valueSerializer = BsonValueSerializer.Instance;
            return new TranslatedExpression(expression, ast, valueSerializer);
        }

        private static TranslatedExpression TranslateBsonValueGetItemWithString(TranslationContext context, Expression expression, Expression sourceExpression, Expression keyExpression)
        {
            var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
            var keyTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, keyExpression);
            var ast = AstExpression.GetField(sourceTranslation.Ast, keyTranslation.Ast);
            var valueSerializer = BsonValueSerializer.Instance;
            return new TranslatedExpression(expression, ast, valueSerializer);
        }

        private static TranslatedExpression TranslateIListGetItemWithInt(TranslationContext context, Expression expression, Expression sourceExpression, Expression indexExpression)
        {
            var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
            var indexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, indexExpression);
            var ast = AstExpression.ArrayElemAt(sourceTranslation.Ast, indexTranslation.Ast);
            var serializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
            return new TranslatedExpression(expression, ast, serializer);
        }

        private static TranslatedExpression TranslateIDictionaryGetItemWithKey(TranslationContext context, Expression expression, Expression dictionaryExpression, Expression keyExpression)
        {
            var dictionaryTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, dictionaryExpression);
            if (!(dictionaryTranslation.Serializer is IBsonDictionarySerializer dictionarySerializer))
            {
                throw new ExpressionNotSupportedException(expression, because: $"dictionary serializer class {dictionaryTranslation.Serializer.GetType()} does not implement {nameof(IBsonDictionarySerializer)}");
            }

            var keySerializer = dictionarySerializer.KeySerializer;
            var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;
            AstExpression keyFieldNameAst;

            if (keyExpression is ConstantExpression constantKeyExpression)
            {
                var key = constantKeyExpression.Value;
                var serializedKey = SerializationHelper.SerializeValue(keySerializer, key);

                if (dictionaryRepresentation == DictionaryRepresentation.Document && serializedKey is not BsonString)
                {
                    throw new ExpressionNotSupportedException(expression, because: "Document representation requires keys to serialize as strings");
                }

                keyFieldNameAst = AstExpression.Constant(serializedKey);
            }
            else
            {
                if (dictionaryRepresentation == DictionaryRepresentation.Document)
                {
                    if (keySerializer is not IHasRepresentationSerializer hasRepresentationSerializer)
                    {
                        throw new ExpressionNotSupportedException(expression, because: $"key serializer class {keySerializer.GetType()} does not implement {nameof(IHasRepresentationSerializer)}");
                    }
                    if (hasRepresentationSerializer.Representation != BsonType.String)
                    {
                        throw new ExpressionNotSupportedException(expression, because: $"key serializer class {keySerializer.GetType()} does not serialize as a string");
                    }
                }

                var keyTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, keyExpression);
                if (!keyTranslation.Serializer.Equals(keySerializer))
                {
                    throw new ExpressionNotSupportedException(expression, because: "key expression serializer is not equal to the key serializer");
                }

                keyFieldNameAst = keyTranslation.Ast;
            }

            AstExpression ast;
            switch (dictionaryRepresentation)
            {
                case DictionaryRepresentation.Document:
                    ast = AstExpression.GetField(dictionaryTranslation.Ast, keyFieldNameAst);
                    break;

                case DictionaryRepresentation.ArrayOfArrays:
                    {
                        var filter = AstExpression.Filter(
                            dictionaryTranslation.Ast,
                            AstExpression.Eq(AstExpression.ArrayElemAt(AstExpression.Var("kvp"), 0), keyFieldNameAst),
                            "kvp",
                            limit: GetLimitIfSupported(context));
                        ast = AstExpression.ArrayElemAt(AstExpression.ArrayElemAt(filter, 0), 1);
                        break;
                    }

                case DictionaryRepresentation.ArrayOfDocuments:
                    {
                        var filter = AstExpression.Filter(
                            dictionaryTranslation.Ast,
                            AstExpression.Eq(AstExpression.GetField(AstExpression.Var("kvp"), "k"), keyFieldNameAst),
                            "kvp",
                            limit: GetLimitIfSupported(context));
                        ast = AstExpression.GetField(AstExpression.ArrayElemAt(filter, 0), "v");
                        break;
                    }
                default:
                    throw new ExpressionNotSupportedException(expression, because: $"Indexer access is not supported when DictionaryRepresentation is: {dictionaryRepresentation}");
            }

            return new TranslatedExpression(expression, ast, dictionarySerializer.ValueSerializer);
        }
    }
}
