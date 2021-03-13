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
    public sealed class AstAndFilter : AstFilter
    {
        #region static
        public static AstAndFilter CreateFlattened(params AstFilter[] filters)
        {
            Ensure.IsNotNull(filters, nameof(filters));
            Ensure.That(filters.Length > 0, "Filters length cannot be zero.", nameof(filters));
            Ensure.That(!filters.Contains(null), "Filters cannot contain null.", nameof(filters));

            return new AstAndFilter(Flatten(filters));

            AstFilter[] Flatten(AstFilter[] filters)
            {
                if (filters.Length == 2 && filters[0].NodeType != AstNodeType.AndFilter && filters[1].NodeType != AstNodeType.AndFilter)
                {
                    return filters;
                }

                if (filters.Any(a => a is AstAndFilter))
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

                    return flattenedFilters.ToArray();
                }

                return filters;
            }
        }
        #endregion

        private readonly AstFilter[] _filters;

        public AstAndFilter(params AstFilter[] filters)
        {
            Ensure.IsNotNull(filters, nameof(filters));
            Ensure.That(filters.Length > 0, "Filters length cannot be zero.", nameof(filters));
            Ensure.That(!filters.Contains(null), "Filters cannot contain null.", nameof(filters));
            _filters = filters;
        }

        public AstFilter[] Filters => _filters;
        public override AstNodeType NodeType => AstNodeType.AndFilter;

        public override BsonValue Render()
        {
            return new BsonDocument("$and", new BsonArray(_filters.Select(a => a.Render())));
        }
    }
}
