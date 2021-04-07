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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Ast.Filters
{
    public sealed class AstNorFilter : AstFilter
    {
        private readonly AstFilter[] _filters;

        public AstNorFilter(params AstFilter[] filters)
        {
            Ensure.IsNotNull(filters, nameof(filters));
            Ensure.That(filters.Length > 0, "filter cannot be empty.", nameof(filters));
            Ensure.That(!filters.Contains(null), "filters cannot contain null.", nameof(filters));
            _filters = filters;
        }

        public AstFilter[] Filters => _filters;
        public override AstNodeType NodeType => AstNodeType.NorFilter;
        public override bool UsesExpr => _filters.Any(f => f.UsesExpr);

        public override BsonValue Render()
        {
            return new BsonDocument("$nor", new BsonArray(_filters.Select(a => a.Render())));
        }
    }
}
