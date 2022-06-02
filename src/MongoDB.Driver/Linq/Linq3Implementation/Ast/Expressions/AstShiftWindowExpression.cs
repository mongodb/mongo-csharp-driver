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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstShiftWindowExpression : AstWindowExpression
    {
        private readonly AstExpression _arg;
        private readonly int _by;
        private readonly AstExpression _defaultValue;

        public AstShiftWindowExpression(AstExpression arg, int by, AstExpression defaultValue)
        {
            _arg = Ensure.IsNotNull(arg, nameof(arg));
            _by = by;
            _defaultValue = defaultValue; // can be null
        }

        public AstExpression Arg => _arg;
        public int By => _by;
        public AstExpression DefaultValue => _defaultValue;
        public override AstNodeType NodeType => AstNodeType.ShiftWindowExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitShiftWindowExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$shift", new BsonDocument
                    {
                        { "output", _arg.Render() },
                        { "by", _by },
                        { "default", _defaultValue?.Render(), _defaultValue != null }
                    }
                }
            };
        }

        public AstShiftWindowExpression Update(AstExpression arg, int by, AstExpression defaultValue)
        {
            if (arg == _arg && by == _by && defaultValue == _defaultValue)
            {
                return this;
            }

            return new AstShiftWindowExpression(arg, by, defaultValue);
        }
    }
}
