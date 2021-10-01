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
    internal sealed class AstSwitchExpressionBranch : AstNode
    {
        private readonly AstExpression _case;
        private readonly AstExpression _then;

        public AstSwitchExpressionBranch(AstExpression @case, AstExpression then)
        {
            _case = Ensure.IsNotNull(@case, nameof(@case));
            _then = Ensure.IsNotNull(then, nameof(then));
        }

        public AstExpression Case => _case;
        public override AstNodeType NodeType => AstNodeType.SwitchExpressionBranch;
        public AstExpression Then => _then;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitSwitchExpressionBranch(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument { { "case", _case.Render() }, { "then", _then.Render() } };
        }

        public AstSwitchExpressionBranch Update(AstExpression @case, AstExpression then)
        {
            if (@case == _case && then == _then)
            {
                return this;
            }

            return new AstSwitchExpressionBranch(@case, then);
        }
    }

    internal sealed class AstSwitchExpression : AstExpression
    {
        private IReadOnlyList<AstSwitchExpressionBranch> _branches;
        private AstExpression _default;

        public AstSwitchExpression(
            IEnumerable<AstSwitchExpressionBranch> branches,
            AstExpression @default = null)
        {
            _branches = Ensure.IsNotNull(branches, nameof(branches)).AsReadOnlyList();
            _default = @default;
        }

        public IReadOnlyList<AstSwitchExpressionBranch> Branches => _branches;
        public AstExpression Default => _default;
        public override AstNodeType NodeType => AstNodeType.SwitchExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitSwitchExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$switch", new BsonDocument
                    {
                        { "branches", new BsonArray(_branches.Select(b => b.Render())) },
                        { "default", () => _default.Render(), _default != null }
                    }
                }
            };
        }

        public AstSwitchExpression Update(
            IEnumerable<AstSwitchExpressionBranch> branches,
            AstExpression @default)
        {
            if (branches == _branches && @default == _default)
            {
                return this;
            }

            return new AstSwitchExpression(branches, @default);
        }
    }
}
