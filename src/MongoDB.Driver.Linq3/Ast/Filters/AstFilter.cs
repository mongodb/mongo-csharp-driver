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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Ast.Filters
{
    public abstract class AstFilter : AstNode
    {
        #region static
        // public static methods
        public static AstFilter All(AstFilterField field, IEnumerable<BsonValue> values)
        {
            return new AstFieldOperationFilter(field, new AstAllFilterOperation(values));
        }

        public static AstFilter And(params AstFilter[] filters)
        {
            Ensure.IsNotNull(filters, nameof(filters));
            Ensure.That(filters.Length > 0, "And must have at least one filter.", nameof(filters));
            Ensure.That(filters.All(f => f != null), "filters cannot contain nulls.", nameof(filters));

            if (filters.Any(f => f.NodeType == AstNodeType.AndFilter))
            {
                var flattenedFilters = new List<AstFilter>();
                foreach (var filter in filters)
                {
                    if (filter is AstAndFilter andFilter)
                    {
                        flattenedFilters.AddRange(andFilter.Filters);
                    }
                    else
                    {
                        flattenedFilters.Add(filter);
                    }
                }
                return new AstAndFilter(flattenedFilters);
            }

            return new AstAndFilter(filters);
        }

        public static AstFieldOperationFilter BitsAllClear(AstFilterField field, BsonValue bitMask)
        {
            return new AstFieldOperationFilter(field, new AstBitsAllClearFilterOperation(bitMask));
        }

        public static AstFieldOperationFilter BitsAllSet(AstFilterField field, BsonValue bitMask)
        {
            return new AstFieldOperationFilter(field, new AstBitsAllSetFilterOperation(bitMask));
        }

        public static AstFieldOperationFilter BitsAnyClear(AstFilterField field, BsonValue bitMask)
        {
            return new AstFieldOperationFilter(field, new AstBitsAnyClearFilterOperation(bitMask));
        }

        public static AstFieldOperationFilter BitsAnySet(AstFilterField field, BsonValue bitMask)
        {
            return new AstFieldOperationFilter(field, new AstBitsAnySetFilterOperation(bitMask));
        }

        public static AstFilter Combine(AstFilter optionalFilter1, AstFilter optionalFilter2)
        {
            return
                optionalFilter1 == null ? optionalFilter2 :
                optionalFilter2 == null ? null :
                AstFilter.And(optionalFilter1, optionalFilter2);
        }

        public static AstFieldOperationFilter Compare(AstFilterField field, AstComparisonFilterOperator comparisonOperator, BsonValue value)
        {
            return new AstFieldOperationFilter(field, new AstComparisonFilterOperation(comparisonOperator, value));
        }

        public static AstFieldOperationFilter ElemMatch(AstFilterField field, AstFilter filter)
        {
            return new AstFieldOperationFilter(field, new AstElemMatchFilterOperation(filter));
        }

        public static AstFieldOperationFilter Eq(AstFilterField field, BsonValue value)
        {
            return new AstFieldOperationFilter(field, new AstComparisonFilterOperation(AstComparisonFilterOperator.Eq, value));
        }

        public static AstFieldOperationFilter In(AstFilterField field, IEnumerable<BsonValue> values)
        {
            return new AstFieldOperationFilter(field, new AstInFilterOperation(values));
        }

        public static AstFilter MatchesEverything()
        {
            return new AstMatchesEverythingFilter();
        }

        public static AstFilter MatchesNothing()
        {
            return new AstMatchesNothingFilter();
        }

        public static AstFieldOperationFilter Mod(AstFilterField field, BsonValue divisor, BsonValue remainder)
        {
            return new AstFieldOperationFilter(field, new AstModFilterOperation(divisor, remainder));
        }

        public static AstFieldOperationFilter Ne(AstFilterField field, BsonValue value)
        {
            return new AstFieldOperationFilter(field, new AstComparisonFilterOperation(AstComparisonFilterOperator.Ne, value));
        }

        public static AstFilter Not(AstFilter filter)
        {
            if (filter is AstFieldOperationFilter fieldOperationFilter)
            {
                if (fieldOperationFilter.Operation is AstNotFilterOperation notFilterOperation)
                {
                    return new AstFieldOperationFilter(fieldOperationFilter.Field, notFilterOperation.Operation);
                }

                if (fieldOperationFilter.Operation is AstComparisonFilterOperation comparisonFilterOperation)
                {
                    var comparisonOperator = comparisonFilterOperation.Operator;
                    if (comparisonOperator == AstComparisonFilterOperator.Eq || comparisonOperator == AstComparisonFilterOperator.Ne)
                    {
                        var oppositeComparisonOperator = comparisonOperator == AstComparisonFilterOperator.Eq ? AstComparisonFilterOperator.Ne : AstComparisonFilterOperator.Eq;
                        return new AstFieldOperationFilter(fieldOperationFilter.Field, new AstComparisonFilterOperation(oppositeComparisonOperator, comparisonFilterOperation.Value));
                    }
                }

                if (fieldOperationFilter.Operation is AstInFilterOperation inFilterOperation)
                {
                    return new AstFieldOperationFilter(fieldOperationFilter.Field, new AstNinFilterOperation(inFilterOperation.Values));
                }

                if (fieldOperationFilter.Operation is AstNinFilterOperation ninFilterOperation)
                {
                    return new AstFieldOperationFilter(fieldOperationFilter.Field, new AstInFilterOperation(ninFilterOperation.Values));
                }

                return new AstFieldOperationFilter(fieldOperationFilter.Field, new AstNotFilterOperation(fieldOperationFilter.Operation));
            }

            if (filter is AstNorFilter norFilter && norFilter.Filters.Length == 1)
            {
                return norFilter.Filters[0];
            }

            if (filter.NodeType == AstNodeType.MatchesEverythingFilter)
            {
                return AstFilter.MatchesNothing();
            }

            if (filter.NodeType == AstNodeType.MatchesNothingFilter)
            {
                return AstFilter.MatchesEverything();
            }

            return new AstNorFilter(filter);
        }

        public static AstFieldOperationFilter Regex(AstFilterField field, string pattern, string options)
        {
            return new AstFieldOperationFilter(field, new AstRegexFilterOperation(pattern, options));
        }

        public static AstFieldOperationFilter Size(AstFilterField field, BsonValue size)
        {
            return new AstFieldOperationFilter(field, new AstSizeFilterOperation(size));
        }
        #endregion

        // public properties
        public virtual bool UsesExpr => false;
    }
}
