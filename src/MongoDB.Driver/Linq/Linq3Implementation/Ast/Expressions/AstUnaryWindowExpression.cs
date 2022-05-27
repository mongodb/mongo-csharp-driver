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
    internal sealed class AstUnaryWindowExpression : AstWindowExpression
    {
        private readonly AstExpression _arg;
        private readonly AstUnaryWindowOperator _operator;
        private readonly AstWindow _window;

        public AstUnaryWindowExpression(AstUnaryWindowOperator @operator, AstExpression arg, AstWindow window)
        {
            _operator = @operator;
            _arg = Ensure.IsNotNull(arg, nameof(arg));
            _window = window;
        }

        public AstExpression Arg => _arg;
        public override AstNodeType NodeType => AstNodeType.UnaryWindowExpression;
        public AstUnaryWindowOperator Operator => _operator;
        public AstWindow Window => _window;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitUnaryWindowExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { _operator.Render(), _arg.Render() },
                { "window", _window?.Render(), _window != null }
            };
        }

        public AstUnaryWindowExpression Update(AstUnaryWindowOperator @operator, AstExpression arg, AstWindow window)
        {
            if (@operator == _operator && arg == _arg && window == _window)
            {
                return this;
            }

            return new AstUnaryWindowExpression(@operator, arg, window);
        }
    }
}
