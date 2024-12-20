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
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class GetTypeComparisonExpressionToFilterTranslator
    {
        // caller is responsible for ensuring constant is on the right
        public static bool CanTranslate(
            Expression leftExpression,
            AstComparisonFilterOperator comparisonOperator,
            Expression rightExpression)
        {
            return
                leftExpression is MethodCallExpression methodCallExpression &&
                methodCallExpression.Method.Is(ObjectMethod.GetType) &&
                (comparisonOperator == AstComparisonFilterOperator.Eq || comparisonOperator == AstComparisonFilterOperator.Ne) &&
                rightExpression is ConstantExpression;
        }

        public static AstFilter Translate(
            TranslationContext context,
            BinaryExpression expression,
            MethodCallExpression getTypeExpression,
            AstComparisonFilterOperator comparisonOperator,
            Expression typeConstantExpression)
        {
            if (CanTranslate(getTypeExpression, comparisonOperator, typeConstantExpression))
            {
                var fieldExpression = getTypeExpression.Object;
                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
                var nominalType = fieldTranslation.Serializer.ValueType;
                var actualType = typeConstantExpression.GetConstantValue<Type>(expression);

                var discriminatorConvention = fieldTranslation.Serializer.GetDiscriminatorConvention();
                var discriminatorField = fieldTranslation.AstField.SubField(discriminatorConvention.ElementName);

                var filter = discriminatorConvention switch
                {
                    IHierarchicalDiscriminatorConvention hierarchicalDiscriminatorConvention => DiscriminatorAstFilter.TypeEquals(discriminatorField, hierarchicalDiscriminatorConvention, nominalType, actualType),
                    _ => DiscriminatorAstFilter.TypeEquals(discriminatorField, discriminatorConvention, nominalType, actualType),
                };

                return comparisonOperator switch
                {
                    AstComparisonFilterOperator.Eq => filter,
                    AstComparisonFilterOperator.Ne => AstFilter.Not(filter),
                    _ => throw new ExpressionNotSupportedException(expression, because: $"comparison operator {comparisonOperator} is not supported")
                };
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
