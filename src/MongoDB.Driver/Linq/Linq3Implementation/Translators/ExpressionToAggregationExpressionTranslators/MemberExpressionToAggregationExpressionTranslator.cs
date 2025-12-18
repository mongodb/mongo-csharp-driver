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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.PropertyTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class MemberExpressionToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MemberExpression expression)
        {
            var containerExpression = expression.Expression;
            var member = expression.Member;

            if (member is PropertyInfo property && property.DeclaringType.IsNullable())
            {
                switch (property.Name)
                {
                    case "HasValue": return HasValuePropertyToAggregationExpressionTranslator.Translate(context, expression);
                    case "Value": return ValuePropertyToAggregationExpressionTranslator.Translate(context, expression);
                    default: break;
                }
            }

            if (TryTranslateDictionaryProperty(context, expression, containerExpression, member, out var translatedDictionaryProperty))
            {
                return translatedDictionaryProperty;
            }

            if (typeof(BsonValue).IsAssignableFrom(containerExpression.Type))
            {
                throw new ExpressionNotSupportedException(expression); // TODO: support BsonValue properties
            }

            var containerTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, containerExpression);
            if (containerTranslation.Serializer is IWrappedValueSerializer wrappedValueSerializer)
            {
                var unwrappedValueAst = AstExpression.GetField(containerTranslation.Ast, wrappedValueSerializer.FieldName);
                containerTranslation = new TranslatedExpression(containerExpression, unwrappedValueAst, wrappedValueSerializer.ValueSerializer);
            }

            if (containerExpression.Type.IsTupleOrValueTuple())
            {
                return TranslateTupleItemProperty(expression, containerTranslation);
            }

            if (!DocumentSerializerHelper.AreMembersRepresentedAsFields(containerTranslation.Serializer, out _))
            {
                if (member is PropertyInfo propertyInfo && propertyInfo.Name == "Length")
                {
                    return LengthPropertyToAggregationExpressionTranslator.Translate(context, expression);
                }

                if (TryTranslateKeyValuePairProperty(expression, containerTranslation, member, out var translatedKeyValuePairProperty))
                {
                    return translatedKeyValuePairProperty;
                }

                if (TryTranslateCollectionCountProperty(expression, containerTranslation, member, out var translatedCount))
                {
                    return translatedCount;
                }

                if (TryTranslateDateTimeProperty(expression, containerTranslation, member, out var translatedDateTimeProperty))
                {
                    return translatedDateTimeProperty;
                }
            }

            var serializationInfo = DocumentSerializerHelper.GetMemberSerializationInfo(containerTranslation.Serializer, member.Name);
            AstExpression ast;
            if (serializationInfo.ElementPath == null)
            {
                ast = AstExpression.GetField(containerTranslation.Ast, serializationInfo.ElementName);
            }
            else
            {
                ast = containerTranslation.Ast;
                foreach (var subFieldName in serializationInfo.ElementPath)
                {
                    ast = AstExpression.GetField(ast, subFieldName);
                }
            }
            return new TranslatedExpression(expression, ast, serializationInfo.Serializer);
        }

        private static TranslatedExpression TranslateTupleItemProperty(MemberExpression expression, TranslatedExpression containerTranslation)
        {
            if (containerTranslation.Serializer is IBsonTupleSerializer tupleSerializer)
            {
                var itemName = expression.Member.Name;
                if (TupleSerializer.TryParseItemName(itemName, out var itemNumber))
                {
                    var ast = AstExpression.ArrayElemAt(containerTranslation.Ast, index: itemNumber - 1);
                    var itemSerializer = tupleSerializer.GetItemSerializer(itemNumber);
                    return new TranslatedExpression(expression, ast, itemSerializer);
                }

                throw new ExpressionNotSupportedException(expression, because: $"Item name is not valid: {itemName}");
            }

            throw new ExpressionNotSupportedException(expression, because: $"serializer {containerTranslation.Serializer.GetType().FullName} does not implement IBsonTupleSerializer");
        }

        private static bool TryTranslateCollectionCountProperty(MemberExpression expression, TranslatedExpression container, MemberInfo memberInfo, out TranslatedExpression result)
        {
            if (EnumerableProperty.IsCountProperty(expression))
            {
                AstExpression ast;

                if (container.Serializer is IBsonDictionarySerializer dictionarySerializer &&
                    dictionarySerializer.DictionaryRepresentation == DictionaryRepresentation.Document)
                {
                    ast = AstExpression.Size(AstExpression.ObjectToArray(container.Ast));
                }
                else
                {
                    SerializationHelper.EnsureRepresentationIsArray(expression, container.Serializer);
                    ast = AstExpression.Size(container.Ast);
                }

                var serializer = Int32Serializer.Instance;
                result = new TranslatedExpression(expression, ast, serializer);
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryTranslateDateTimeProperty(MemberExpression expression, TranslatedExpression container, MemberInfo memberInfo, out TranslatedExpression result)
        {
            result = null;

            if (container.Expression.Type == typeof(DateTime) && memberInfo is PropertyInfo propertyInfo)
            {
                AstExpression ast;
                IBsonSerializer serializer;

                switch (propertyInfo.Name)
                {
                    case "Date":
                        ast = AstExpression.DateTrunc(container.Ast, "day");
                        serializer = container.Serializer;
                        break;

                    case "DayOfWeek":
                        ast = AstExpression.Subtract(AstExpression.DatePart(AstDatePart.DayOfWeek, container.Ast), 1);
                        serializer = new EnumSerializer<DayOfWeek>(BsonType.Int32);
                        break;

                    case "TimeOfDay":
                        var endDate = container.Ast;
                        var startDate = AstExpression.DateTrunc(container.Ast, "day");
                        ast = AstExpression.DateDiff(startDate, endDate, "millisecond");
                        serializer = new TimeSpanSerializer(BsonType.Int64, TimeSpanUnits.Milliseconds);
                        break;

                    default:
                        var datePart = propertyInfo.Name switch
                        {
                            "Day" => AstDatePart.DayOfMonth,
                            "DayOfWeek" => AstDatePart.DayOfWeek,
                            "DayOfYear" => AstDatePart.DayOfYear,
                            "Hour" => AstDatePart.Hour,
                            "Millisecond" => AstDatePart.Millisecond,
                            "Minute" => AstDatePart.Minute,
                            "Month" => AstDatePart.Month,
                            "Second" => AstDatePart.Second,
                            "Year" => AstDatePart.Year,
                            _ => throw new ExpressionNotSupportedException(expression)
                        };
                        ast = AstExpression.DatePart(datePart, container.Ast);
                        serializer = new Int32Serializer();
                        break;
                }

                result = new TranslatedExpression(expression, ast, serializer);
                return true;
            }

            return false;
        }

        private static bool TryTranslateDictionaryProperty(TranslationContext context, MemberExpression expression, Expression containerExpression, MemberInfo memberInfo, out TranslatedExpression translatedDictionaryProperty)
        {
            if (memberInfo is PropertyInfo propertyInfo)
            {
                var declaringType = propertyInfo.DeclaringType;
                var declaringTypeDefinition = declaringType.IsConstructedGenericType ? declaringType.GetGenericTypeDefinition() : null;
                if (declaringTypeDefinition == typeof(Dictionary<,>) || declaringTypeDefinition == typeof(IDictionary<,>))
                {
                    var containerTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, containerExpression);
                    var containerAst = containerTranslation.Ast;

                    if (containerTranslation.Serializer is IBsonDictionarySerializer dictionarySerializer)
                    {
                        var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;
                        var keySerializer = dictionarySerializer.KeySerializer;
                        var valueSerializer = dictionarySerializer.ValueSerializer;
                        var kvpVar = AstExpression.Var("kvp");

                        switch (propertyInfo.Name)
                        {
                            case "Count":
                                var countAst = dictionaryRepresentation switch
                                {
                                    DictionaryRepresentation.ArrayOfDocuments or DictionaryRepresentation.ArrayOfArrays => AstExpression.Size(containerAst),
                                    _ => throw new ExpressionNotSupportedException(expression, $"Unexpected dictionary representation: {dictionaryRepresentation}")
                                };
                                var countSerializer = Int32Serializer.Instance;
                                translatedDictionaryProperty = new TranslatedExpression(expression, countAst, countSerializer);
                                return true;

                            case "Keys":
                                var keysAst = dictionaryRepresentation switch
                                {
                                    DictionaryRepresentation.ArrayOfDocuments => AstExpression.Map(containerAst, kvpVar, AstExpression.GetField(kvpVar, "k")),
                                    DictionaryRepresentation.ArrayOfArrays  => AstExpression.Map(containerAst, kvpVar, AstExpression.ArrayElemAt(kvpVar, 0)),
                                    _ => throw new ExpressionNotSupportedException(expression, $"Unexpected dictionary representation: {dictionaryRepresentation}")
                                };
                                var keysSerializer = declaringTypeDefinition == typeof(Dictionary<,>)
                                    ? DictionaryKeyCollectionSerializer.Create(keySerializer, valueSerializer)
                                    : ICollectionSerializer.Create(keySerializer);
                                translatedDictionaryProperty = new TranslatedExpression(expression, keysAst, keysSerializer);
                                return true;

                            case "Values":
                                if (declaringTypeDefinition == typeof(Dictionary<,>))
                                {
                                    var kvpPairsAst = dictionaryRepresentation switch
                                    {
                                        DictionaryRepresentation.ArrayOfDocuments => containerAst,
                                        DictionaryRepresentation.ArrayOfArrays => AstExpression.Map(containerAst, kvpVar, AstExpression.ComputedDocument([("k", AstExpression.ArrayElemAt(kvpVar, 0)), ("v", AstExpression.ArrayElemAt(kvpVar, 1))])),
                                        _ => throw new ExpressionNotSupportedException(expression, $"Unexpected dictionary representation: {dictionaryRepresentation}")
                                    };
                                    var valuesSerializer = DictionaryValueCollectionSerializer.Create(keySerializer, valueSerializer);
                                    translatedDictionaryProperty = new TranslatedExpression(expression, kvpPairsAst, valuesSerializer);
                                    return true;
                                }
                                else if (declaringTypeDefinition == typeof(IDictionary<,>))
                                {
                                    var valuesAst = dictionaryRepresentation switch
                                    {
                                        DictionaryRepresentation.ArrayOfArrays => AstExpression.Map(containerAst, kvpVar, AstExpression.ArrayElemAt(kvpVar, 1)),
                                        DictionaryRepresentation.ArrayOfDocuments => AstExpression.Map(containerAst, kvpVar, AstExpression.GetField(kvpVar, "v")),
                                        _ => throw new ExpressionNotSupportedException(expression, $"Unexpected dictionary representation: {dictionaryRepresentation}")
                                    };
                                    var valuesSerializer = ICollectionSerializer.Create(valueSerializer);
                                    translatedDictionaryProperty = new TranslatedExpression(expression, valuesAst, valuesSerializer);
                                    return true;
                                }
                                break;
                        }

                    }
                }
            }

            translatedDictionaryProperty = null;
            return false;
        }

        private static bool TryTranslateKeyValuePairProperty(MemberExpression expression, TranslatedExpression container, MemberInfo memberInfo, out TranslatedExpression result)
        {
            if (container.Expression.Type.IsGenericType &&
                container.Expression.Type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>) &&
                container.Serializer is IKeyValuePairSerializerV2 { Representation: BsonType.Array } kvpSerializer)
            {
                AstExpression ast;
                IBsonSerializer serializer;

                switch (memberInfo.Name)
                {
                    case "Key":
                        ast = AstExpression.ArrayElemAt(container.Ast, 0);
                        serializer = kvpSerializer.KeySerializer;
                        break;
                    case "Value":
                        ast = AstExpression.ArrayElemAt(container.Ast, 1);
                        serializer = kvpSerializer.ValueSerializer;
                        break;
                    default:
                        throw new ExpressionNotSupportedException(expression);
                }
                result = new TranslatedExpression(expression, ast, serializer);
                return true;
            }

            result = null;
            return false;
        }
    }
}
