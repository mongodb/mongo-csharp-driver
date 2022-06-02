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
    internal sealed class AstBinaryWindowExpression : AstWindowExpression
    {
        private readonly AstExpression _arg1;
        private readonly AstExpression _arg2;
        private readonly AstBinaryWindowOperator _operator;
        private readonly AstWindow _window;

        public AstBinaryWindowExpression(AstBinaryWindowOperator @operator, AstExpression arg1, AstExpression arg2, AstWindow window)
        {
            _operator = @operator;
            _arg1 = Ensure.IsNotNull(arg1, nameof(arg1));
            _arg2 = Ensure.IsNotNull(arg2, nameof(arg2));
            _window = window;
        }

        public AstExpression Arg1 => _arg1;
        public AstExpression Arg2 => _arg2;
        public override AstNodeType NodeType => AstNodeType.BinaryWindowExpression;
        public AstBinaryWindowOperator Operator => _operator;
        public AstWindow Window => _window;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitBinaryWindowExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { _operator.Render(), new BsonArray { _arg1.Render(), _arg2.Render() } },
                { "window", _window?.Render(), _window != null }
            };
        }

        public AstBinaryWindowExpression Update(AstBinaryWindowOperator @operator, AstExpression arg1, AstExpression arg2, AstWindow window)
        {
            if (@operator == _operator && arg1 == _arg1 && arg2 == _arg2 && window == _window)
            {
                return this;
            }

            return new AstBinaryWindowExpression(@operator, arg1, arg2, window);
        }
    }
}
