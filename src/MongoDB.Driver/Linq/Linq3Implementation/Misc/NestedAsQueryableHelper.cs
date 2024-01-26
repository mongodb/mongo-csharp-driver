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
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class NestedAsQueryableHelper
    {
        public static void EnsureQueryableMethodHasNestedAsQueryableSource(MethodCallExpression expression, AggregationExpression sourceTranslation)
        {
            if (expression.Method.DeclaringType == typeof(Queryable) &&
                sourceTranslation.Serializer is not INestedAsQueryableSerializer)
            {
                throw new ExpressionNotSupportedException(expression, because: "source serializer is not a NestedAsQueryableSerializer");
            }
        }

        public static void EnsureQueryableMethodHasNestedAsOrderedQueryableSource(MethodCallExpression expression, AggregationExpression sourceTranslation)
        {
            if (expression.Method.DeclaringType == typeof(Queryable) &&
                sourceTranslation.Serializer is not INestedAsOrderedQueryableSerializer)
            {
                throw new ExpressionNotSupportedException(expression, because: "source serializer is not a NestedAsOrderedQueryableSerializer");
            }
        }
    }
}
