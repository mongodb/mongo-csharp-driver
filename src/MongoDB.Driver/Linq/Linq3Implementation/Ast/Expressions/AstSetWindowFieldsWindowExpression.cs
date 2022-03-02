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
    internal sealed class AstSetWindowFieldsWindowExpression : AstExpression
    {
        private readonly IReadOnlyList<AstExpression> _args;
        private readonly AstSetWindowFieldsOperator _operator;
        private readonly AstSetWindowFieldsWindow _window;

        public AstSetWindowFieldsWindowExpression(AstSetWindowFieldsOperator @operator, IEnumerable<AstExpression> args, AstSetWindowFieldsWindow window)
        {
            _operator = @operator;
            _args = Ensure.IsNotNull(args, nameof(args)).AsReadOnlyList();
            _window = window; // optional
        }

        public IReadOnlyList<AstExpression> Args => _args;
        public override AstNodeType NodeType => AstNodeType.SetWindowFieldsWindowExpression;
        public AstSetWindowFieldsOperator Operator => _operator;
        public AstSetWindowFieldsWindow Window => _window;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitSetWindowFieldsExpression(this);
        }

        public override BsonValue Render()
        {
            var renderedArgs = _operator switch
            {
                AstSetWindowFieldsOperator.Derivative => RenderDerivativeOrIntegralArgs(),
                AstSetWindowFieldsOperator.ExpMovingAvgWithAlphaWeighting => RenderExpMovingAvgArgsWithAlphaWeightingArgs(),
                AstSetWindowFieldsOperator.ExpMovingAvgWithPositionalWeighting => RenderExpMovingAvgArgsWithPositionalWeightingArgs(),
                AstSetWindowFieldsOperator.Integral => RenderDerivativeOrIntegralArgs(),
                AstSetWindowFieldsOperator.Shift => RenderShiftArgs(),
                _ when _args.Count == 1 => _args[0].Render(),
                _ => new BsonArray(_args.Select(a => a.Render()))
            };

            return new BsonDocument
            {
                { _operator.Render(), renderedArgs },
                { "window", () => _window.Render(), _window != null }
            };
        }

        public AstSetWindowFieldsWindowExpression Update(IEnumerable<AstExpression> args)
        {
            if (args == _args)
            {
                return this;
            }

            return new AstSetWindowFieldsWindowExpression(_operator, args, _window);
        }

        private BsonDocument RenderDerivativeOrIntegralArgs()
        {
            var input = _args[0].Render();
            var unit = _args.Count > 1 ? _args[1].Render() : null;

            return new BsonDocument
            {
                { "input", input },
                { "unit", unit, unit != null }
            };
        }

        private BsonDocument RenderExpMovingAvgArgsWithAlphaWeightingArgs()
        {
            var input = _args[0].Render();
            var alpha = _args[1].Render();

            return new BsonDocument
            {
                { "input", input },
                { "alpha", alpha }
            };
        }

        private BsonDocument RenderExpMovingAvgArgsWithPositionalWeightingArgs()
        {
            var input = _args[0].Render();
            var n = _args[1].Render();

            return new BsonDocument
            {
                { "input", input },
                { "N", n }
            };
        }

        private BsonDocument RenderShiftArgs()
        {
            var output = _args[0].Render();
            var by = _args[1].Render();
            var defaultValue = _args.Count == 3 ? _args[2].Render() : null;

            return new BsonDocument
            {
                { "output", output },
                { "by", by },
                { "default", defaultValue, defaultValue != null }
            };
        }
    }
}
