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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstNullaryWindowExpression : AstWindowExpression
    {
        private readonly AstNullaryWindowOperator _operator;
        private readonly AstWindow _window;

        public AstNullaryWindowExpression(AstNullaryWindowOperator @operator, AstWindow window)
        {
            _operator = @operator;
            _window = window;
        }

        public override AstNodeType NodeType => AstNodeType.UnaryWindowExpression;
        public AstNullaryWindowOperator Operator => _operator;
        public AstWindow Window => _window;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitNullaryWindowExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { _operator.Render(), new BsonDocument() },
                { "window", _window?.Render(), _window != null }
            };
        }

        public AstNullaryWindowExpression Update(AstNullaryWindowOperator @operator, AstWindow window)
        {
            if (@operator == _operator && window == _window)
            {
                return this;
            }

            return new AstNullaryWindowExpression(@operator, window);
        }
    }
}
