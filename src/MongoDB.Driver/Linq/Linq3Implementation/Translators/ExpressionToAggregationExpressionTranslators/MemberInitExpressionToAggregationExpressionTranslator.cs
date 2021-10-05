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

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class MemberInitExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MemberInitExpression expression)
        {
            var classSerializer = BsonSerializer.LookupSerializer(expression.Type);
            if (classSerializer is IBsonDocumentSerializer documentSerializer)
            {
                var computedFields = new List<AstComputedField>();

                var newExpression = expression.NewExpression;
                var constructorParameters = newExpression.Constructor.GetParameters();
                var constructorArguments = newExpression.Arguments;
                for (var i = 0; i < constructorParameters.Length; i++)
                {
                    var constructorParameter = constructorParameters[i];
                    var argumentExpression = constructorArguments[i];
                    var fieldName = GetFieldName(constructorParameter);
                    var argumentTanslation = ExpressionToAggregationExpressionTranslator.Translate(context, argumentExpression);
                    computedFields.Add(AstExpression.ComputedField(fieldName, argumentTanslation.Ast));
                }

                foreach (var binding in expression.Bindings)
                {
                    var memberAssignment = (MemberAssignment)binding;
                    var member = memberAssignment.Member;
                    if (!(documentSerializer.TryGetMemberSerializationInfo(member.Name, out var memberSerializationInfo)))
                    {
                        throw new ExpressionNotSupportedException(expression);
                    }
                    var elementName = memberSerializationInfo.ElementName;
                    var valueExpression = memberAssignment.Expression;
                    var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                    computedFields.Add(AstExpression.ComputedField(elementName, valueTranslation.Ast));
                }

                var ast = AstExpression.ComputedDocument(computedFields);
                var serializer = context.KnownSerializersRegistry.GetSerializer(expression);
                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static string GetFieldName(ParameterInfo parameter)
        {
            // TODO: implement properly
            var parameterName = parameter.Name;
            var fieldName = parameterName.Substring(0, 1).ToUpper() + parameterName.Substring(1);
            return fieldName;
        }
    }
}
