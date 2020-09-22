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
        public static TranslatedExpression Translate(TranslationContext context, MemberInitExpression expression)
        {
            var computedFields = new List<AstComputedField>();
            var classMapType = typeof(BsonClassMap<>).MakeGenericType(expression.Type);
            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);

            foreach (var binding in expression.Bindings)
            {
                var memberAssignment = (MemberAssignment)binding;
                var member = memberAssignment.Member;
                var valueExpression = memberAssignment.Expression;
                var translatedValue = ExpressionTranslator.Translate(context, valueExpression);
                computedFields.Add(new AstComputedField(member.Name, translatedValue.Translation));
                var translatedFieldSerializer = translatedValue.Serializer ?? BsonSerializer.LookupSerializer(valueExpression.Type);
                classMap.MapMember(member).SetSerializer(translatedFieldSerializer);
            }
            classMap.Freeze();

            var translation = new AstComputedDocumentExpression(computedFields);
            var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(expression.Type);
            var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);

            return new TranslatedExpression(expression, translation, serializer);
        }
    }
}
