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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class ProjectionHelper
    {
        // public static methods
        public static (IReadOnlyList<AstProjectStageSpecification>, IBsonSerializer) CreateAggregationProjection(AggregationExpression expression)
        {
            return expression.Ast.NodeType switch
            {
                AstNodeType.ComputedDocumentExpression => CreateComputedDocumentProjection(expression),
                _ => CreateWrappedValueProjection(expression)
            };
        }

        public static (IReadOnlyList<AstProjectStageSpecification>, IBsonSerializer) CreateFindProjection(AggregationExpression expression)
        {
            return expression.Ast.NodeType switch
            {
                AstNodeType.GetFieldExpression => CreateFindGetFieldProjection(expression),
                _ => CreateAggregationProjection(expression)
            };
        }

        public static (AstProjectStage, IBsonSerializer) CreateProjectStage(AggregationExpression expression)
        {
            var (specifications, projectionSerializer) = CreateAggregationProjection(expression);
            var projectStage = AstStage.Project(specifications);
            return (projectStage, projectionSerializer);
        }

        // private static methods
        private static (IReadOnlyList<AstProjectStageSpecification>, IBsonSerializer) CreateComputedDocumentProjection(AggregationExpression expression)
        {
            var computedDocument = (AstComputedDocumentExpression)expression.Ast;

            var specifications = new List<AstProjectStageSpecification>();
            var isIdProjected = false;
            foreach (var computedField in computedDocument.Fields)
            {
                var path = computedField.Path;
                var value = QuoteIfNecessary(computedField.Value);
                specifications.Add(AstProject.Set(path, value));
                isIdProjected |= path == "_id";
            }
            if (!isIdProjected)
            {
                specifications.Add(AstProject.ExcludeId());
            }

            return (specifications, expression.Serializer);
        }

        private static (IReadOnlyList<AstProjectStageSpecification>, IBsonSerializer) CreateFindGetFieldProjection(AggregationExpression expression)
        {
            var getFieldExpressionAst = (AstGetFieldExpression)expression.Ast;
            if (getFieldExpressionAst.HasSafeFieldName(out var fieldName))
            {
                var specifications = fieldName == "_id" ?
                    new List<AstProjectStageSpecification> { AstProject.Include(fieldName) } :
                    new List<AstProjectStageSpecification> { AstProject.Include(fieldName), AstProject.Exclude("_id") };
                var wrappedValueSerializer = WrappedValueSerializer.Create(fieldName, expression.Serializer);
                return (specifications, wrappedValueSerializer);
            }

            return CreateWrappedValueProjection(expression);
        }

        private static (IReadOnlyList<AstProjectStageSpecification>, IBsonSerializer) CreateWrappedValueProjection(AggregationExpression expression)
        {
            var wrappedValueSerializer = WrappedValueSerializer.Create("_v", expression.Serializer);
            var specifications = new List<AstProjectStageSpecification>
            {
                AstProject.Set(wrappedValueSerializer.FieldName, QuoteIfNecessary(expression.Ast)),
                AstProject.ExcludeId()
            };

            return (specifications, wrappedValueSerializer);
        }

        private static AstExpression QuoteIfNecessary(AstExpression expression)
        {
            if (expression is AstConstantExpression constantExpression)
            {
                if (ValueNeedsToBeQuoted(constantExpression.Value))
                {
                    return AstExpression.Literal(constantExpression);
                }
            }

            return expression;

            bool ValueNeedsToBeQuoted(BsonValue value)
            {
                switch (value.BsonType)
                {
                    case BsonType.Boolean:
                    case BsonType.Decimal128:
                    case BsonType.Double:
                    case BsonType.Int32:
                    case BsonType.Int64:
                        return true;

                    default:
                        return false;
                }
            }
        }
    }
}
