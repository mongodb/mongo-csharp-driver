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
using System.Collections;
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
        public static AggregationExpression Translate(TranslationContext context, MemberExpression expression)
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

            var containerTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, containerExpression);
            if (containerTranslation.Serializer is IWrappedValueSerializer wrappedValueSerializer)
            {
                var unwrappedValueAst = AstExpression.GetField(containerTranslation.Ast, wrappedValueSerializer.FieldName);
                containerTranslation = new AggregationExpression(containerExpression, unwrappedValueAst, wrappedValueSerializer.ValueSerializer);
            }

            if (containerExpression.Type.IsTupleOrValueTuple())
            {
                return TranslateTupleItemProperty(expression, containerTranslation);
            }

            if (!DocumentSerializerHelper.AreMembersRepresentedAsFields(containerTranslation.Serializer, out _))
            {
                if (member is PropertyInfo propertyInfo  && propertyInfo.Name == "Length")
                {
                    return LengthPropertyToAggregationExpressionTranslator.Translate(context, expression);
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
            return new AggregationExpression(expression, ast, serializationInfo.Serializer);
        }

        private static AggregationExpression TranslateTupleItemProperty(MemberExpression expression, AggregationExpression containerTranslation)
        {
            if (containerTranslation.Serializer is IBsonTupleSerializer tupleSerializer)
            {
                var itemName = expression.Member.Name;
                if (TupleSerializer.TryParseItemName(itemName, out var itemNumber))
                {
                    var ast = AstExpression.ArrayElemAt(containerTranslation.Ast, index: itemNumber - 1);
                    var itemSerializer = tupleSerializer.GetItemSerializer(itemNumber);
                    return new AggregationExpression(expression, ast, itemSerializer);
                }

                throw new ExpressionNotSupportedException(expression, because: $"Item name is not valid: {itemName}");
            }

            throw new ExpressionNotSupportedException(expression, because: $"serializer {containerTranslation.Serializer.GetType().FullName} does not implement IBsonTupleSerializer");
        }

        private static bool TryTranslateCollectionCountProperty(MemberExpression expression, AggregationExpression container, MemberInfo memberInfo, out AggregationExpression result)
        {
            if (EnumerableProperty.IsCountProperty(expression))
            {
                SerializationHelper.EnsureRepresentationIsArray(expression, container.Serializer);

                var ast = AstExpression.Size(container.Ast);
                var serializer = Int32Serializer.Instance;

                result = new AggregationExpression(expression, ast, serializer);
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryTranslateDateTimeProperty(MemberExpression expression, AggregationExpression container, MemberInfo memberInfo, out AggregationExpression result)
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

                result = new AggregationExpression(expression, ast, serializer);
                return true;
            }

            return false;
        }
    }
}
