﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public sealed class AstComputedArrayExpression : AstExpression
    {
        private readonly IReadOnlyList<AstExpression> _items;

        public AstComputedArrayExpression(IEnumerable<AstExpression> items)
        {
            _items = Ensure.IsNotNull(items, nameof(items)).ToList().AsReadOnly();
        }

        public AstComputedArrayExpression(params AstExpression[] items)
            : this((IEnumerable<AstExpression>)items)
        {
        }

        public IReadOnlyList<AstExpression> Items => _items;
        public override AstNodeType NodeType => AstNodeType.ComputedArrayExpression;

        public override BsonValue Render()
        {
            var renderedItems = new List<BsonValue>();

            foreach (var item in _items)
            {
                var renderedItem = item.Render();
                renderedItems.Add(renderedItem);
            }

            return new BsonArray(renderedItems);
        }
    }
}
