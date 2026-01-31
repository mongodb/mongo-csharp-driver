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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class DictionaryIndexerComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression, Expression rightExpression)
        {
            return
                leftExpression is MethodCallExpression methodCallExpression &&
                rightExpression is ConstantExpression &&
                DictionaryMethod.IsGetItemWithKeyMethod(methodCallExpression.Method);
        }

        public static AstFilter Translate(TranslationContext context, Expression containingExpression, AstComparisonFilterOperator comparisonOperator, MethodCallExpression indexerExpression, ConstantExpression valueExpression)
        {
            var dictionaryExpression = indexerExpression.Object;
            var keyExpression = indexerExpression.Arguments[0];

            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, dictionaryExpression);

            if (fieldTranslation.Serializer is not IBsonDictionarySerializer dictionarySerializer)
            {
                throw new ExpressionNotSupportedException(containingExpression, because: $"class {fieldTranslation.Serializer.GetType().FullName} does not implement the IBsonDictionarySerializer interface");
            }

            var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;
            var serializedKey = GetSerializedKey(containingExpression, keyExpression, dictionarySerializer.KeySerializer);
            var serializedValue = SerializationHelper.SerializeValue(dictionarySerializer.ValueSerializer, valueExpression, containingExpression);

            switch (dictionaryRepresentation)
            {
                case DictionaryRepresentation.Document:
                    if (serializedKey is not BsonString)
                    {
                        throw new ExpressionNotSupportedException(containingExpression, because: "Document representation requires keys to serialize as strings");
                    }
                    var subField = fieldTranslation.SubField(serializedKey.AsString, dictionarySerializer.ValueSerializer);
                    return AstFilter.Compare(subField.Ast, comparisonOperator, serializedValue);

                case DictionaryRepresentation.ArrayOfArrays:
                case DictionaryRepresentation.ArrayOfDocuments:
                    var keyFieldName = dictionaryRepresentation == DictionaryRepresentation.ArrayOfArrays ? "0" : "k";
                    var valueFieldName = dictionaryRepresentation == DictionaryRepresentation.ArrayOfArrays ? "1" : "v";

                    var keyField = AstFilter.Field(keyFieldName);
                    var valueField = AstFilter.Field(valueFieldName);
                    var keyMatchFilter = AstFilter.Eq(keyField, serializedKey);
                    var valueMatchFilter = AstFilter.Compare(valueField, comparisonOperator, serializedValue);
                    var combinedFilter = AstFilter.And(keyMatchFilter, valueMatchFilter);

                    return AstFilter.ElemMatch(fieldTranslation.Ast, combinedFilter);

                default:
                    throw new ExpressionNotSupportedException(containingExpression, because: $"Unexpected dictionary representation: {dictionaryRepresentation}");
            }
        }

        private static BsonValue GetSerializedKey(Expression expression, Expression keyExpression, IBsonSerializer keySerializer)
        {
            var key = keyExpression.GetConstantValue<object>(containingExpression: expression);
            return SerializationHelper.SerializeValue(keySerializer, key);
        }
    }
}
