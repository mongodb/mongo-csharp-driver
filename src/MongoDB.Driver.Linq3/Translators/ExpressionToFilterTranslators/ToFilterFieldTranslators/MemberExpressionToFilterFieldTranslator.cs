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
    public static class MemberExpressionToFilterFieldTranslator
    {
        public static AstFilterField Translate(TranslationContext context, MemberExpression memberExpression)
        {
            var containingFieldAst = ExpressionToFilterFieldTranslator.Translate(context, memberExpression.Expression);
            var containingFieldSerializer = containingFieldAst.Serializer;
            var containingFieldSerializerType = containingFieldSerializer.GetType();

            if (containingFieldSerializer is IBsonDocumentSerializer documentSerializer &&
                documentSerializer.TryGetMemberSerializationInfo(memberExpression.Member.Name, out BsonSerializationInfo memberSerializationInfo))
            {
                var subFieldName = memberSerializationInfo.ElementName;
                var subFieldSerializer = memberSerializationInfo.Serializer;
                return containingFieldAst.SubField(subFieldName, subFieldSerializer);
            }

            if (memberExpression.Expression.Type.IsConstructedGenericType &&
                memberExpression.Expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                memberExpression.Member.Name == "Value" &&
                containingFieldSerializerType.IsConstructedGenericType &&
                containingFieldSerializerType.GetGenericTypeDefinition() == typeof(NullableSerializer<>))
            {
                var valueSerializer = ((IChildSerializerConfigurable)containingFieldSerializer).ChildSerializer;
                return AstFilter.Field(containingFieldAst.Path, valueSerializer);
            }

            throw new ExpressionNotSupportedException(memberExpression);
        }

    }
}
