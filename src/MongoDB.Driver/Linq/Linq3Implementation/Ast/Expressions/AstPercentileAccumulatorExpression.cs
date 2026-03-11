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
    internal sealed class AstPercentileAccumulatorExpression : AstAccumulatorExpression
    {
        private readonly AstExpression _input;
        private readonly AstExpression _percentiles;

        public AstPercentileAccumulatorExpression(AstExpression input, AstExpression percentiles)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _percentiles = Ensure.IsNotNull(percentiles, nameof(percentiles));
        }

        public AstExpression Input => _input;

        public AstExpression Percentiles => _percentiles;

        public override AstNodeType NodeType => AstNodeType.PercentileAccumulatorExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitPercentileAccumulatorExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                {
                    "$percentile", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "p", _percentiles.Render() },
                        { "method", "approximate" } // server requires this parameter but currently only allows this value
                    }
                }
            };
        }

        public AstPercentileAccumulatorExpression Update(AstExpression input, AstExpression percentiles)
        {
            if (input == _input && percentiles == _percentiles)
            {
                return this;
            }
            return new AstPercentileAccumulatorExpression(input, percentiles);
        }
    }
}