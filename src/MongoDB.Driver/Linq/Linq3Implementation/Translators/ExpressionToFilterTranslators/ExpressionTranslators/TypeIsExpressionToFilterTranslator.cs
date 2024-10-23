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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class TypeIsExpressionToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, TypeBinaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.TypeIs)
            {
                var fieldExpression = expression.Expression;
                var field = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
                var nominalType = fieldExpression.Type;
                var actualType = expression.TypeOperand;

                if (nominalType == actualType)
                {
                    return AstFilter.MatchesEverything();
                }
                else
                {
                    var discriminatorConvention = field.Serializer.GetDiscriminatorConvention();
                    var discriminatorField = field.SubField(discriminatorConvention.ElementName, BsonValueSerializer.Instance);

                    return discriminatorConvention switch
                    {
                        IHierarchicalDiscriminatorConvention hierarchicalDiscriminatorConvention => DiscriminatorAstFilter.TypeIs(discriminatorField, hierarchicalDiscriminatorConvention, nominalType, actualType),
                        IScalarDiscriminatorConvention scalarDiscriminatorConvention => DiscriminatorAstFilter.TypeIs(discriminatorField, scalarDiscriminatorConvention, nominalType, actualType),
                        _ => throw new ExpressionNotSupportedException(expression, because: "is operator is not supported with the configured discriminator convention")
                    };
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
