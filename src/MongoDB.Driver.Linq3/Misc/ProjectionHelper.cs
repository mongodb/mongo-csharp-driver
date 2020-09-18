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
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;

namespace MongoDB.Driver.Linq3.Misc
{
    public static class ProjectionHelper
    {
        //public static BsonValue ConvertExpressionToProjection(BsonValue translatedExpression)
        public static IEnumerable<AstProjectStageSpecification> ConvertExpressionToProjection(AstExpression expression)
        {
            //if (translatedExpression is BsonDocument projection)
            //{
            //    foreach (var element in projection.Elements.ToList())
            //    {
            //        var needsLiteral = false;
            //        switch (element.Value.BsonType)
            //        {
            //            case BsonType.Boolean:
            //            case BsonType.Decimal128:
            //            case BsonType.Double:
            //            case BsonType.Int32:
            //            case BsonType.Int64:
            //                needsLiteral = true;
            //                break;
            //        }

            //        if (needsLiteral)
            //        {
            //            projection[element.Name] = new BsonDocument("$literal", element.Value);
            //        }
            //    }

            //    if (!projection.Contains("_id"))
            //    {
            //        projection.InsertAt(0, new BsonElement("_id", 0));
            //    }

            //    return projection;
            //}

            //return translatedExpression;

            var computedDocumentExpression = (AstComputedDocumentExpression)expression; // TODO: is this always true?            

            var projection = new List<AstProjectStageSpecification>();

            var isIdProjected = false;
            foreach (var computedField in computedDocumentExpression.Fields)
            {
                var projectedField = computedField;
                if (computedField.Expression is AstConstantExpression constantExpression)
                {
                    var constantValue = constantExpression.Value;
                    if (NeedsToBeQuoted(constantValue))
                    {
                        projectedField = new AstComputedField(computedField.Name, new AstUnaryExpression(AstUnaryOperator.Literal, constantExpression));
                    }
                }
                projection.Add(new AstProjectStageComputedFieldSpecification(projectedField));
                isIdProjected |= computedField.Name == "_id";
            }

            if (!isIdProjected)
            {
                projection.Insert(0, new AstProjectStageExcludeIdSpecification());
            }

            return projection;

            bool NeedsToBeQuoted(BsonValue constantValue)
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
    }
}
