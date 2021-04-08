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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.PropertyTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators
{
    public static class MemberExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MemberExpression expression)
        {
            var containerExpression = expression.Expression;
            var member = expression.Member;

            var containerTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, containerExpression);
            if (!DocumentSerializerHelper.HasFieldInfo(containerTranslation.Serializer, member.Name))
            {
                switch (member.Name)
                {
                    case "Length": return LengthPropertyToAggregationExpressionTranslator.Translate(context, expression);
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

            var fieldInfo = DocumentSerializerHelper.GetFieldInfo(containerTranslation.Serializer, member.Name);
            if (containerTranslation.Ast is AstFieldExpression fieldExpression)
            {
                var ast = AstExpression.SubField(fieldExpression, fieldInfo.ElementName);
                return new AggregationExpression(expression, ast, fieldInfo.Serializer);
            }
            else
            {
                var ast = AstExpression.Let(
                    AstExpression.ComputedField("this", containerTranslation.Ast),
                    AstExpression.Field($"$this.{fieldInfo.ElementName}"));
                return new AggregationExpression(expression, ast, fieldInfo.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool TryTranslateCollectionCountProperty(MemberExpression expression, AggregationExpression container, MemberInfo memberInfo, out AggregationExpression result)
        {
            result = null;

            var memberName = memberInfo.Name;
            if ((memberName == "Count" || memberName == "LongCount") && memberInfo is PropertyInfo propertyInfo)
            {
                var containerType = container.Expression.Type;
                if (containerType.Implements(typeof(ICollection)) || containerType.Implements(typeof(ICollection<>)))
                {
                    var ast = AstExpression.Size(container.Ast);
                    var serializer = BsonSerializer.LookupSerializer(propertyInfo.PropertyType);

                    result = new AggregationExpression(expression, ast, serializer);
                    return true;
                }
            }

            return false;
        }

        private static bool TryTranslateDateTimeProperty(MemberExpression expression, AggregationExpression container, MemberInfo memberInfo, out AggregationExpression result)
        {
            result = null;

            if (container.Expression.Type == typeof(DateTime) && memberInfo is PropertyInfo propertyInfo)
            {
                AstExpression ast;
                IBsonSerializer serializer;

                if (propertyInfo.Name == "DayOfWeek")
                {
                    ast = AstExpression.Subtract(AstExpression.DatePart(AstDatePart.DayOfWeek, container.Ast), 1);
                    serializer = new EnumSerializer<DayOfWeek>(BsonType.Int32);
                }
                else
                {
                    AstDatePart datePart;
                    switch (propertyInfo.Name)
                    {
                        case "Day": datePart = AstDatePart.DayOfMonth; break;
                        case "DayOfYear": datePart = AstDatePart.DayOfYear; break;
                        case "DayOfWeek": datePart = AstDatePart.DayOfWeek; break;
                        case "Hour": datePart = AstDatePart.Hour; break;
                        case "Millisecond": datePart = AstDatePart.Millisecond; break;
                        case "Minute": datePart = AstDatePart.Minute; break;
                        case "Month": datePart = AstDatePart.Month; break;
                        case "Second": datePart = AstDatePart.Second; break;
                        case "Week": datePart = AstDatePart.Week; break;
                        case "Year": datePart = AstDatePart.Year; break;
                        default: return false;
                    }
                    ast = AstExpression.DatePart(datePart, container.Ast);
                    serializer = new Int32Serializer();
                }

                result = new AggregationExpression(expression, ast, serializer);
                return true;
            }

            return false;
        }
    }
}
