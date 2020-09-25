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

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class MemberExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MemberExpression expression)
        {
            var container = expression.Expression;
            var member = expression.Member;

            var translatedContainer = ExpressionTranslator.Translate(context, container);

            if (!DocumentSerializerHelper.HasFieldInfo(translatedContainer.Serializer, member.Name))
            {
                if (TryTranslateCollectionCountProperty(expression, translatedContainer, member, out var translatedCount))
                {
                    return translatedCount;
                }

                if (TryTranslateDateTimeProperty(expression, translatedContainer, member, out var translatedDateTimeProperty))
                {
                    return translatedDateTimeProperty;
                }
            }

            var fieldInfo = DocumentSerializerHelper.GetFieldInfo(translatedContainer.Serializer, member.Name);

            //if (translatedContainer.Translation.IsString && translatedContainer.Translation.AsString.StartsWith("$"))
            if (translatedContainer.Translation is AstFieldExpression fieldExpression)
            {
                //var containerTranslation = translatedContainer.Translation.AsString;
                //var translation = TranslatedFieldHelper.Combine(containerTranslation, fieldInfo.ElementName);
                var translation = new AstFieldExpression(TranslatedFieldHelper.Combine(fieldExpression.Field, fieldInfo.ElementName));
                return new TranslatedExpression(expression, translation, fieldInfo.Serializer);
            }
            else
            {
                //var translation = new BsonDocument("$let", new BsonDocument
                //{
                //    { "vars", new BsonDocument("_document", translatedContainer.Translation) },
                //    { "in", $"$$_document.{fieldInfo.ElementName}" }
                //});
                var translation = new AstLetExpression(
                    new[] { new AstComputedField("d__", translatedContainer.Translation) },
                    new AstFieldExpression($"$$d__.{fieldInfo.ElementName}"));
                return new TranslatedExpression(expression, translation, fieldInfo.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool TryTranslateCollectionCountProperty(MemberExpression expression, TranslatedExpression container, MemberInfo memberInfo, out TranslatedExpression result)
        {
            result = null;

            var memberName = memberInfo.Name;
            if ((memberName == "Count" || memberName == "LongCount") && memberInfo is PropertyInfo propertyInfo)
            {
                var containerType = container.Expression.Type;
                if (containerType.Implements(typeof(ICollection)) || containerType.Implements(typeof(ICollection<>)))
                {
                    var translation =
                        new AstConvertExpression(
                            new AstUnaryExpression(AstUnaryOperator.Size, container.Translation),
                            propertyInfo.PropertyType);
                    var serializer = BsonSerializer.LookupSerializer(propertyInfo.PropertyType);
                    result = new TranslatedExpression(expression, translation, serializer);
                    return true;
                }
            }

            return false;
        }

        private static bool TryTranslateDateTimeProperty(MemberExpression expression, TranslatedExpression container, MemberInfo memberInfo, out TranslatedExpression result)
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

                var translation = new AstDatePartExpression(datePart, container.Translation);
                var serializer = new Int32Serializer();
                result = new TranslatedExpression(expression, translation, serializer);
                return true;
            }

            return false;
        }
    }
}
