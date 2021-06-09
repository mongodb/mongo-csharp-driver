﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Filters;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators
{
    public static class MemberExpressionToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MemberExpression expression)
        {
            var containerExpression = expression.Expression;
            var memberInfo = expression.Member;

            if (expression.Type == typeof(bool))
            {
                if (memberInfo is PropertyInfo propertyInfo)
                {
                    var containerSerializer = BsonSerializer.LookupSerializer(containerExpression.Type); // TODO: use known serializer
                    if (containerSerializer is IBsonDocumentSerializer containerDocumentSerializer)
                    {
                        if (containerDocumentSerializer.TryGetMemberSerializationInfo(propertyInfo.Name, out var propertySerializationInfo))
                        {
                            var elementName = propertySerializationInfo.ElementName;
                            return new AstComparisonFilter(AstComparisonFilterOperator.Eq, new AstFieldExpression(elementName), true);
                        }
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
