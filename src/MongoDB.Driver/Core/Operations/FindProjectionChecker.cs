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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    internal static class FindProjectionChecker
    {
        internal static void ThrowIfAggregationExpressionIsUsedWhenNotSupported(BsonDocument projection, int wireVersion)
        {
            if (projection == null || Feature.FindProjectionExpressions.IsSupported(wireVersion))
            {
                return;
            }

            foreach (var specification in projection)
            {
                ThrowIfAggregationExpressionIsUsed(specification);
            }

            static void ThrowIfAggregationExpressionIsUsed(BsonElement specification)
            {
                if (IsAggregationExpression(specification.Value))
                {
                    var specificationAsDocument = new BsonDocument(specification);
                    throw new NotSupportedException($"The projection specification {specificationAsDocument} uses an aggregation expression and is not supported with find on servers prior to version 4.4.");
                }
            }

            static bool IsAggregationExpression(BsonValue value)
            {
                return value.BsonType switch
                {
                    BsonType.Boolean => false,
                    _ when value.IsNumeric => false,
                    _ when value is BsonDocument documentValue =>
                        documentValue.ElementCount == 1 && documentValue.GetElement(0).Name switch
                        {
                            "$elemMatch" => false,
                            "$meta" => false,
                            "$slice" => false,
                            _ => true,
                        },
                    _ => true
                };
            }
        }
    }
}
