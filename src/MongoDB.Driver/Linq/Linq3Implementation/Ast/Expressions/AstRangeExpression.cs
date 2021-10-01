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
    internal sealed class AstRangeExpression : AstExpression
    {
        private readonly AstExpression _end;
        private readonly AstExpression _start;
        private readonly AstExpression _step;

        public AstRangeExpression(
            AstExpression start,
            AstExpression end,
            AstExpression step = null)
        {
            _start = Ensure.IsNotNull(start, nameof(start));
            _end = Ensure.IsNotNull(end, nameof(end));
            _step = step;
        }

        public AstExpression End => _end;
        public override AstNodeType NodeType => AstNodeType.RangeExpression;
        public AstExpression Start => _start;
        public AstExpression Step => _step;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitRangeExpression(this);
        }

        public override BsonValue Render()
        {
            var args = new BsonArray { _start.Render(), _end.Render() };
            if (_step != null)
            {
                args.Add(_step.Render());
            }
 
            return new BsonDocument("$range", args);
        }

        public AstRangeExpression Update(
            AstExpression start,
            AstExpression end,
            AstExpression step)
        {
            if (start == _start && end == _end && step == _step)
            {
                return this;
            }

            return new AstRangeExpression(start, end, step);
        }
    }
}
