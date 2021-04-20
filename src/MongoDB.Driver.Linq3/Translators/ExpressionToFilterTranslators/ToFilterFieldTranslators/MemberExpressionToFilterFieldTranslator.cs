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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Filters;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal static class MemberExpressionToFilterFieldTranslator
    {
        public static AstFilterField Translate(TranslationContext context, MemberExpression memberExpression)
        {
            var field = ExpressionToFilterFieldTranslator.Translate(context, memberExpression.Expression);
            var fieldSerializer = field.Serializer;
            var fieldSerializerType = fieldSerializer.GetType();

            if (fieldSerializer is IBsonDocumentSerializer documentSerializer &&
                documentSerializer.TryGetMemberSerializationInfo(memberExpression.Member.Name, out BsonSerializationInfo memberSerializationInfo))
            {
                var subFieldName = memberSerializationInfo.ElementName;
                var subFieldSerializer = memberSerializationInfo.Serializer;
                return field.SubField(subFieldName, subFieldSerializer);
            }

            if (memberExpression.Expression.Type.IsConstructedGenericType &&
                memberExpression.Expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                memberExpression.Member.Name == "Value" &&
                fieldSerializerType.IsConstructedGenericType &&
                fieldSerializerType.GetGenericTypeDefinition() == typeof(NullableSerializer<>))
            {
                var valueSerializer = ((IChildSerializerConfigurable)fieldSerializer).ChildSerializer;
                return AstFilter.Field(field.Path, valueSerializer);
            }

            throw new ExpressionNotSupportedException(memberExpression);
        }

    }
}
