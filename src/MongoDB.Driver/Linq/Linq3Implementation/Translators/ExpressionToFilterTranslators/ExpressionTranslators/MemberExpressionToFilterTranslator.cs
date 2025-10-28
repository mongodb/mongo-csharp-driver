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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class MemberExpressionToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MemberExpression expression)
        {
            var memberInfo = expression.Member;

            if (memberInfo is FieldInfo fieldInfo)
            {
                if (fieldInfo.FieldType == typeof(bool))
                {
                    var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, expression);
                    var serializedTrue = SerializationHelper.SerializeValue(context.SerializationDomain, fieldTranslation.Serializer, true);
                    return AstFilter.Eq(fieldTranslation.Ast, serializedTrue);
                }
            }

            if (memberInfo is PropertyInfo propertyInfo)
            {
                if (propertyInfo.Is(NullableProperty.HasValue))
                {
                    var fieldExpression = expression.Expression;
                    var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
                    return AstFilter.Ne(fieldTranslation.Ast, BsonNull.Value);
                }

                if (propertyInfo.PropertyType == typeof(bool))
                {
                    var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, expression);
                    var serializedTrue = SerializationHelper.SerializeValue(context.SerializationDomain, fieldTranslation.Serializer, true);
                    return AstFilter.Eq(fieldTranslation.Ast, serializedTrue);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
