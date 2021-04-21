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
    internal sealed class AstAndFilter : AstFilter
    {
        private readonly IReadOnlyList<AstFilter> _filters;

        public AstAndFilter(IEnumerable<AstFilter> filters)
        {
            _filters = Ensure.IsNotNull(filters, nameof(filters)).ToList().AsReadOnly();
            Ensure.That(_filters.Count > 0, "filters cannot be empty.", nameof(filters));
            Ensure.That(!_filters.Contains(null), "filters cannot contain null.", nameof(filters));
        }

        public IReadOnlyList<AstFilter> Filters => _filters;
        public override AstNodeType NodeType => AstNodeType.AndFilter;
        public override bool UsesExpr => _filters.Any(f => f.UsesExpr);

        public override BsonValue Render()
        {
            if (TryRenderAsImplicitAnd(_filters, out var renderedAsImplicitAnd))
            {
                return renderedAsImplicitAnd;
            }
            else
            {
                return new BsonDocument("$and", new BsonArray(_filters.Select(f => f.Render())));
            }
        }

        private bool TryRenderAsImplicitAnd(IReadOnlyList<AstFilter> filters, out BsonDocument renderedAsImplicitAnd)
        {
            renderedAsImplicitAnd = new BsonDocument();

            foreach (var filter in filters)
            {
                if (!(filter is AstFieldOperationFilter fieldOperationFilter))
                {
                    return false;
                }

                if (!OperationCanBeUsedInImplicitAnd(fieldOperationFilter.Operation))
                {
                    return false;
                }

                var renderedFilter = filter.Render() as BsonDocument;
                if (renderedFilter == null || renderedFilter.ElementCount != 1)
                {
                    return false;
                }

                var fieldPath = renderedFilter.GetElement(0).Name;
                if (fieldPath.StartsWith("$"))
                {
                    // this case occurs when { $elem : { $op : args } } is rendered as { $op : args } inside an $elemMatch
                    if (renderedAsImplicitAnd.Contains(fieldPath))
                    {
                        return false;
                    }
                    renderedAsImplicitAnd.Merge(renderedFilter);
                }
                else
                {
                    var fieldValue = renderedAsImplicitAnd.GetValue(fieldPath, null);
                    if (fieldValue == null)
                    {
                        renderedAsImplicitAnd.Merge(renderedFilter);
                    }
                    else
                    {
                        var fieldDocument = fieldValue as BsonDocument;
                        if (fieldDocument == null)
                        {
                            return false;
                        }

                        var renderedOperation = renderedFilter[0] as BsonDocument;
                        if (renderedOperation == null || renderedOperation.ElementCount != 1)
                        {
                            return false;
                        }

                        var operatorName = renderedOperation.GetElement(0).Name;
                        if (!operatorName.StartsWith("$") || fieldDocument.Contains(operatorName))
                        {
                            return false;
                        }

                        fieldDocument.Merge(renderedOperation);
                    }
                }
            }

            return true;
        }

        private bool OperationCanBeUsedInImplicitAnd(AstFilterOperation operation)
        {
            switch (operation.NodeType)
            {
                case AstNodeType.GeoIntersectsFilterOperation:
                case AstNodeType.GeoNearStage:
                case AstNodeType.GeoWithinBoxFilterOperation:
                case AstNodeType.GeoWithinCenterFilterOperation:
                case AstNodeType.GeoWithinCenterSphereFilterOperation:
                case AstNodeType.GeoWithinFilterOperation:
                case AstNodeType.NearFilterOperation:
                case AstNodeType.NearSphereFilterOperation:
                    return false;

                default:
                    return true;
            }
        }
    }
}
