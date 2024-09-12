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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class TypeIsExpressionToAggregationExpressionTranslator
    {
        // public static methods
        public static AggregationExpression Translate(TranslationContext context, TypeBinaryExpression expression)
        {
            var objectExpression = expression.Expression;
            var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);

            var nominalType = objectExpression.Type;
            var actualType = expression.TypeOperand;

            AstExpression ast;
            if (nominalType == actualType)
            {
                ast = AstExpression.Constant(true);
            }
            else
            {
                var discriminatorConvention = objectTranslation.Serializer.GetDiscriminatorConvention();
                var discriminatorField = AstExpression.GetField(objectTranslation.Ast, discriminatorConvention.ElementName);

                ast = discriminatorConvention switch
                {
                    IHierarchicalDiscriminatorConvention hierarchicalDiscriminatorConvention => DiscriminatorAstExpression.TypeIs(discriminatorField, hierarchicalDiscriminatorConvention, nominalType, actualType),
                    IScalarDiscriminatorConvention scalarDiscriminatorConvention => DiscriminatorAstExpression.TypeIs(discriminatorField, scalarDiscriminatorConvention, nominalType, actualType),
                    _ => throw new ExpressionNotSupportedException(expression, because: "is operator is not supported with the configured discriminator convention")
                };
            }

            return new AggregationExpression(expression, ast, BooleanSerializer.Instance);
        }
    }
}
