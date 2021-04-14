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

namespace MongoDB.Driver.Linq3.Misc
{
    public static class ProjectionHelper
    {
        // public static methods
        public static (AstProjectStage, IBsonSerializer) CreateProjectStage(AggregationExpression expression)
        {
            if (expression.Ast.NodeType == AstNodeType.ComputedDocumentExpression)
            {
                return CreateComputedDocumentProjectStage(expression);
            }
            else
            {
                return CreateWrappedValueProjectStage(expression);
            }
        }

        // private static methods
        private static (AstProjectStage, IBsonSerializer) CreateComputedDocumentProjectStage(AggregationExpression expression)
        {
            var computedDocument = (AstComputedDocumentExpression)expression.Ast;

            var specifications = new List<AstProjectStageSpecification>();

            var isIdProjected = false;
            foreach (var computedField in computedDocument.Fields)
            {
                var path = computedField.Path;
                var value = computedField.Value;

                if (value is AstConstantExpression astConstantExpression)
                {
                    if (ValueNeedsToBeQuoted(astConstantExpression.Value))
                    {
                        value = AstExpression.Literal(value);
                    }
                }
                specifications.Add(AstProject.Set(path, value));
                isIdProjected |= path == "_id";
            }

            if (!isIdProjected)
            {
                specifications.Add(AstProject.ExcludeId());
            }

            var projectStage = AstStage.Project(specifications);

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

        private static (AstProjectStage, IBsonSerializer) CreateWrappedValueProjectStage(AggregationExpression expression)
        {
            var wrappedValueSerializer = WrappedValueSerializer.Create(expression.Serializer);
            var projectStage =
                AstStage.Project(
                    AstProject.Set("_v", expression.Ast),
                    AstProject.ExcludeId());

            return (projectStage, wrappedValueSerializer);
        }
    }
}
