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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstComputedArrayExpression : AstExpression
    {
        private readonly IReadOnlyList<AstExpression> _items;

        public AstComputedArrayExpression(IEnumerable<AstExpression> items)
        {
            _items = Ensure.IsNotNull(items, nameof(items)).AsReadOnlyList();
        }

        public IReadOnlyList<AstExpression> Items => _items;
        public override AstNodeType NodeType => AstNodeType.ComputedArrayExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitComputedArrayExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonArray(_items.Select(item => item.Render()));
        }

        public AstComputedArrayExpression Update(IEnumerable<AstExpression> items)
        {
            if (items == _items)
            {
                return this;
            }

            return new AstComputedArrayExpression(items);
        }
    }
}
