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
    public sealed class AstElemMatchFilter : AstFilter
    {
        private readonly AstFilterField _field;
        private readonly AstFilter _filter; // note: using "$elem" to represent the implied element values

        public AstElemMatchFilter(AstFilterField field, AstFilter filter)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _filter = Ensure.IsNotNull(filter, nameof(filter));
        }

        public AstFilterField Field => _field;
        public AstFilter Filter => _filter;
        public override AstNodeType NodeType => AstNodeType.ElemMatchFilter;

        public override BsonValue Render()
        {
            return new BsonDocument(_field.Path, new BsonDocument("$elemMatch", RewriteElemMatchFilter(_filter.Render())));
        }

        // TODO: this implementation is incomplete
        private BsonValue RewriteElemMatchFilter(BsonValue filter)
        {
            if (filter is BsonDocument filterDocument && filterDocument.ElementCount == 1)
            {
                var elementName = filterDocument.GetElement(0).Name;
                if (elementName == "$elem")
                {
                    var condition = filterDocument[0];
                    if (condition is BsonDocument conditionDocument &&
                        conditionDocument.ElementCount > 0 &&
                        conditionDocument.GetElement(0).Name.StartsWith("$"))
                    {
                        return condition; // TODO: recurse
                    }
                    return new BsonDocument("$eq", condition);
                }

                if (elementName.StartsWith("$elem."))
                {
                    var subFieldName = elementName.Substring(6);
                    filterDocument.SetElement(0, new BsonElement(subFieldName, filterDocument[0]));
                    return filterDocument;
                }

                if (elementName == "$and" || elementName == "$or")
                {
                    var rewrittenClauses = filterDocument[0].AsBsonArray.Select(clause => RewriteElemMatchFilter(clause));
                    return new BsonDocument(elementName, new BsonArray(rewrittenClauses));
                }
            }

            return filter;
        }
    }
}
