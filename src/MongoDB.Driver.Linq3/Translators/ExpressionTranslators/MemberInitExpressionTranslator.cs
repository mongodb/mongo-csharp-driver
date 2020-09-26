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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class MemberInitExpressionTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MemberInitExpression expression)
        {
            var classMapType = typeof(BsonClassMap<>).MakeGenericType(expression.Type);
            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);
            var computedFields = new List<AstComputedField>();

            foreach (var binding in expression.Bindings)
            {
                var memberAssignment = (MemberAssignment)binding;
                var member = memberAssignment.Member;
                var valueExpression = memberAssignment.Expression;
                var valueTranslation = ExpressionTranslator.Translate(context, valueExpression);
                var memberSerializer = valueTranslation.Serializer ?? BsonSerializer.LookupSerializer(valueExpression.Type);
                classMap.MapMember(member).SetSerializer(memberSerializer);
                computedFields.Add(new AstComputedField(member.Name, valueTranslation.Ast));
            }
            classMap.Freeze();

            var ast = new AstComputedDocumentExpression(computedFields);
            var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(expression.Type);
            var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);

            return new ExpressionTranslation(expression, ast, serializer);
        }
    }
}
