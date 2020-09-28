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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionTranslators.PropertyTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class MemberExpressionTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MemberExpression expression)
        {
            var containerExpression = expression.Expression;
            var member = expression.Member;

            var containerTranslation = ExpressionTranslator.Translate(context, containerExpression);
            if (!DocumentSerializerHelper.HasFieldInfo(containerTranslation.Serializer, member.Name))
            {
                switch (member.Name)
                {
                    case "Length": return LengthPropertyTranslator.Translate(context, expression);
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
                var ast = new AstFieldExpression(TranslatedFieldHelper.Combine(fieldExpression.Field, fieldInfo.ElementName));
                return new ExpressionTranslation(expression, ast, fieldInfo.Serializer);
            }
            else
            {
                var ast = new AstLetExpression(
                    new[] { new AstComputedField("this", containerTranslation.Ast) },
                    new AstFieldExpression($"$$this.{fieldInfo.ElementName}"));
                return new ExpressionTranslation(expression, ast, fieldInfo.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool TryTranslateCollectionCountProperty(MemberExpression expression, ExpressionTranslation container, MemberInfo memberInfo, out ExpressionTranslation result)
        {
            result = null;

            var memberName = memberInfo.Name;
            if ((memberName == "Count" || memberName == "LongCount") && memberInfo is PropertyInfo propertyInfo)
            {
                var containerType = container.Expression.Type;
                if (containerType.Implements(typeof(ICollection)) || containerType.Implements(typeof(ICollection<>)))
                {
                    var ast =
                        new AstConvertExpression(
                            new AstUnaryExpression(AstUnaryOperator.Size, container.Ast),
                            propertyInfo.PropertyType);
                    var serializer = BsonSerializer.LookupSerializer(propertyInfo.PropertyType);

                    result = new ExpressionTranslation(expression, ast, serializer);
                    return true;
                }
            }

            return false;
        }

        private static bool TryTranslateDateTimeProperty(MemberExpression expression, ExpressionTranslation container, MemberInfo memberInfo, out ExpressionTranslation result)
        {
            result = null;

            if (container.Expression.Type == typeof(DateTime) && memberInfo is PropertyInfo propertyInfo)
            {
                AstDatePart datePart;
                switch (propertyInfo.Name)
                {
                    case "Day": datePart = AstDatePart.DayOfMonth; break;
                    case "DayOfYear": datePart = AstDatePart.DayOfYear; break;
                    case "Hour": datePart = AstDatePart.Hour; break;
                    case "Millisecond": datePart = AstDatePart.Millisecond; break;
                    case "Minute": datePart = AstDatePart.Minute; break;
                    case "Month": datePart = AstDatePart.Month; break;
                    case "Second": datePart = AstDatePart.Second; break;
                    case "Week": datePart = AstDatePart.Week; break;
                    case "Year": datePart = AstDatePart.Year; break;
                    default: return false;
                }
                var ast = new AstDatePartExpression(datePart, container.Ast);
                var serializer = new Int32Serializer();

                result = new ExpressionTranslation(expression, ast, serializer);
                return true;
            }

            return false;
        }
    }
}
