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
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq3.Misc
{
    public static class ProjectionHelper
    {
        // public static methods
        public static void AddProjectStage(Pipeline pipeline, AggregationExpression expression)
        {
            var (projectStage, newOutputSerializer) = CreateProjectStage(pipeline, expression);
            pipeline.AddStages(newOutputSerializer, projectStage);
        }

        public static (AstProjectStage, IBsonSerializer) CreateProjectStage(Pipeline pipeline, AggregationExpression expression)
        {
            if (expression.Ast.NodeType == AstNodeType.ComputedDocumentExpression)
            {
                return CreateComputedDocumentProjectStage(pipeline, expression);
            }
            else
            {
                return CreateWrappedValueProjectStage(pipeline, expression);
            }
        }

        // private static methods
        private static (AstProjectStage, IBsonSerializer) CreateComputedDocumentProjectStage(Pipeline pipeline, AggregationExpression expression)
        {
            var computedDocument = (AstComputedDocumentExpression)expression.Ast;

            var specifications = new List<AstProjectStageSpecification>();

            var isIdProjected = false;
            foreach (var computedField in computedDocument.Fields)
            {
                var projectedField = computedField;
                if (computedField.Expression is AstConstantExpression constantExpression)
                {
                    if (ValueNeedsToBeQuoted(constantExpression.Value))
                    {
                        projectedField = new AstComputedField(computedField.Name, new AstUnaryExpression(AstUnaryOperator.Literal, constantExpression));
                    }
                }
                specifications.Add(new AstProjectStageComputedFieldSpecification(projectedField));
                isIdProjected |= computedField.Name == "_id";
            }

            if (!isIdProjected)
            {
                specifications.Add(new AstProjectStageExcludeIdSpecification());
            }

            var projectStage = new AstProjectStage(specifications);

            return (projectStage, expression.Serializer);

            bool ValueNeedsToBeQuoted(BsonValue constantValue)
            {
                switch (constantValue.BsonType)
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

        private static (AstProjectStage, IBsonSerializer) CreateWrappedValueProjectStage(Pipeline pipeline, AggregationExpression expression)
        {
            var wrappedValueSerializer = WrappedValueSerializer.Create(expression.Serializer);
            var projectStage =
                new AstProjectStage(
                    new AstProjectStageComputedFieldSpecification(new Ast.AstComputedField("_v", expression.Ast)),
                    new AstProjectStageExcludeIdSpecification());

            return (projectStage, wrappedValueSerializer);
        }
    }
}
