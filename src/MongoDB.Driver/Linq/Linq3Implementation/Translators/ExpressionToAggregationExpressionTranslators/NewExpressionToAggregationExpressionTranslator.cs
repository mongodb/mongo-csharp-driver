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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, NewExpression expression)
        {
            if (expression.Type == typeof(DateTime))
            {
                return NewDateTimeExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            if (expression.Type.IsConstructedGenericType && expression.Type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                return NewHashSetExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            if (expression.Type.IsConstructedGenericType && expression.Type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return NewListExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }

            var classMapType = typeof(BsonClassMap<>).MakeGenericType(expression.Type);
            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);
            var computedFields = new List<AstComputedField>();

            for (var i = 0; i < expression.Members.Count; i++)
            {
                var member = expression.Members[i];
                var fieldExpression = expression.Arguments[i];
                var fieldTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, fieldExpression);
                var memberSerializer = fieldTranslation.Serializer ?? BsonSerializer.LookupSerializer(fieldExpression.Type);
                classMap.MapProperty(member.Name).SetSerializer(memberSerializer);
                computedFields.Add(AstExpression.ComputedField(member.Name, fieldTranslation.Ast));
            }

            var constructorInfo = expression.Type.GetConstructors().Single();
            var constructorArgumentNames = expression.Members.Select(m => m.Name).ToArray();
            classMap.MapConstructor(constructorInfo, constructorArgumentNames);
            classMap.Freeze();

            var ast = AstExpression.ComputedDocument(computedFields);
            var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(expression.Type);
            var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);

            return new AggregationExpression(expression, ast, serializer);
        }
    }
}
