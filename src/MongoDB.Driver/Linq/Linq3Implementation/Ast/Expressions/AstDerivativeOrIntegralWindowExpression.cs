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
    internal sealed class AstDerivativeOrIntegralWindowExpression : AstWindowExpression
    {
        private readonly AstExpression _arg;
        private readonly AstDerivativeOrIntegralWindowOperator _operator;
        private readonly WindowTimeUnit? _unit;
        private readonly AstWindow _window;

        public AstDerivativeOrIntegralWindowExpression(AstDerivativeOrIntegralWindowOperator @operator, AstExpression arg, WindowTimeUnit? unit, AstWindow window)
        {
            _operator = @operator;
            _arg = Ensure.IsNotNull(arg, nameof(arg));
            _unit = unit;
            _window = window;
        }

        public AstExpression Arg => _arg;
        public override AstNodeType NodeType => AstNodeType.DerivativeOrIntegralWindowExpression;
        public AstDerivativeOrIntegralWindowOperator Operator => _operator;
        public WindowTimeUnit? Unit => _unit;
        public AstWindow Window => _window;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitDerivativeOrIntegralWindowExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { _operator.Render(), new BsonDocument
                    {
                        { "input", _arg.Render() },
                        { "unit", () => _unit.Value.Render(), _unit.HasValue }
                    }
                },
                { "window", _window?.Render(), _window != null }
            };
        }

        public AstDerivativeOrIntegralWindowExpression Update(AstDerivativeOrIntegralWindowOperator @operator, AstExpression arg, WindowTimeUnit? unit, AstWindow window)
        {
            if (@operator == _operator && arg == _arg && unit == _unit && window == _window)
            {
                return this;
            }

            return new AstDerivativeOrIntegralWindowExpression(@operator, arg, unit, window);
        }
    }
}
