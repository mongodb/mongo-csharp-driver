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

using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal static class SelectMethodToFilterFieldTranslator
    {
        public static TranslatedFilterField Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.Select))
            {
                var fieldExpression = arguments[0];
                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);

                var selectorExpression = arguments[1];
                if (selectorExpression is LambdaExpression lambdaExpression &&
                    lambdaExpression.Body is MemberExpression memberExpression &&
                    memberExpression.Expression == lambdaExpression.Parameters.Single())
                {
                    var itemSerializer = ArraySerializerHelper.GetItemSerializer(fieldTranslation.Serializer);
                    if (DocumentSerializerHelper.AreMembersRepresentedAsFields(itemSerializer, out var documentSerializer))
                    {
                        var memberName = memberExpression.Member.Name;
                        if (documentSerializer.TryGetMemberSerializationInfo(memberName, out var memberSerializationInfo))
                        {
                            var subFieldName = memberSerializationInfo.ElementName;
                            var subFieldSerializer = IEnumerableSerializer.Create(memberSerializationInfo.Serializer);
                            return fieldTranslation.SubField(subFieldName, subFieldSerializer);
                        }
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
