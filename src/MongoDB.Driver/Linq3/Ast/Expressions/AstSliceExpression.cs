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
using System;

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    internal sealed class AstSliceExpression : AstExpression
    {
        private readonly AstExpression _array;
        private readonly AstExpression _n;
        private readonly AstExpression _position;

        public AstSliceExpression(
            AstExpression array,
            AstExpression n)
        {
            _array = Ensure.IsNotNull(array, nameof(array));
            _n = Ensure.IsNotNull(n, nameof(n));
        }

        public AstSliceExpression(
            AstExpression array,
            AstExpression position,
            AstExpression n)
        {
            _array = Ensure.IsNotNull(array, nameof(array));
            _position = Ensure.IsNotNull(position, nameof(position));
            _n = Ensure.IsNotNull(n, nameof(n));
        }

        public AstExpression Array => _array;
        public AstExpression N => _n;
        public override AstNodeType NodeType => AstNodeType.SliceExpression;
        public AstExpression Position => _position;

        public override BsonValue Render()
        {
            var args =
                _position == null ?
                    new BsonArray { _array.Render(), _n.Render() } :
                    new BsonArray { _array.Render(), _position.Render(), _n.Render() };

            return new BsonDocument("$slice", args);
        }
    }
}
