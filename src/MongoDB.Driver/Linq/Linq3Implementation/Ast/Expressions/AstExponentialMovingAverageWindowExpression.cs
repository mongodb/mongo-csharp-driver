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
    internal sealed class AstExponentialMovingAverageWindowExpression : AstWindowExpression
    {
        private readonly AstExpression _arg;
        private readonly ExponentialMovingAverageWeighting _weighting;
        private readonly AstWindow _window;

        public AstExponentialMovingAverageWindowExpression(AstExpression arg, ExponentialMovingAverageWeighting weighting, AstWindow window)
        {
            _arg = Ensure.IsNotNull(arg, nameof(arg));
            _weighting = Ensure.IsNotNull(weighting, nameof(weighting));
            _window = window;
        }

        public AstExpression Arg => _arg;
        public override AstNodeType NodeType => AstNodeType.ExponentialMovingAverageWindowExpression;
        public ExponentialMovingAverageWeighting Weighting => _weighting;
        public AstWindow Window => _window;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitExponentialMovingAverageWindowExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$expMovingAvg", new BsonDocument
                    {
                        { "input", _arg.Render() },
                        { "N", () => ((ExponentialMovingAveragePositionalWeighting)_weighting).N, _weighting is ExponentialMovingAveragePositionalWeighting },
                        { "alpha", () => ((ExponentialMovingAverageAlphaWeighting)_weighting).Alpha, _weighting is ExponentialMovingAverageAlphaWeighting },
                    }
                },
                { "window", _window?.Render(), _window != null }
            };
        }

        public AstExponentialMovingAverageWindowExpression Update(AstExpression arg, ExponentialMovingAverageWeighting weighting, AstWindow window)
        {
            if (arg == _arg && weighting == _weighting && window == _window)
            {
                return this;
            }

            return new AstExponentialMovingAverageWindowExpression(arg, weighting, window);
        }
    }
}
